/*  更新履歴
 *
 * 0.2.0
 *	 初期リリース()
 * 
 * 1.0.0
 *	 預金枠拡張機能追加
 *　
 * 1.0.1
 *	 Typo修正
 * 
 * 1.1.0
 *   入出金額をボタンではなく入力フィールドから入力するように変更
 *
 * 1.1.1
 *   「use_only_ui」がTRUEのの状態で取引を行ったときに、権限エラーでUIが消える問題の修正
 *    0以下の値を入力したときに取引が続行されてしまう問題の修正
 * 
 * 1.1.2
 *    枠拡張の支払いを口座残高から行った場合残高がマイナスになっても支払いができる問題の修正
 *
 * 1.2.0-β
 *    langを日本語と英語で分離
 *    ユーザの言語設定が英語(デフォルト)意外だった場合に各言語のメッセージを表示するように変更
 * 
 */

/*
 * TODO
 *    リファクタ
 *    ハードコード
 *    コイン必要数などのコンフィグ分離。langにハードコードしないように変数か
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rusty Bank", "babu77", "1.2.0")]
    [Description("This is a simple plugin that adds banking functionality.")]
    public class RustyBank : RustPlugin
    {
        #region Definitions(Global)
        private const string PName = "<color=yellow>[RustyBank]:</color>";
        private const string PermDefault = "rustybank.default";
        private const string PermVip1 = "rustybank.vip1";
        private const string PermVip2 = "rustybank.vip2";
        private const string PermVip3 = "rustybank.vip3";
        private const string PermAdmin = "rustybank.admin";
        private const string ExtensionUiName = "RustyBankUIExtension";
        private const string ExtensionItemUiName = "RustyBankUIExtensionItem_";
        private const string MainUiName = "RustyBankUIMain";
        private const string SubUiName = "RustyBankUISub";
        private const string OpMenuUiName = "RustyBankUIOpMenu";
        private const string DaWUiName = "RustyBankDaWUi";
        private const string ExtensionSubUiName = "RustyBankUIExtensionSub";
        private const string FadeUiName = "RustyBankUIFade";
        private const string NameLabelUiName = "RustyBankUINameLabel";
        private const string DepositUiName = "RustyBankUIDeposit";
        private const string WithdrawUiName = "RustyBankUIWithdraw";
        private const string ShopUiName = "RustyBankUIShop";
        private const string AdminUiName = "RustyBankUIAdmin";
        private Configurations Configs = new Configurations();
        private DynamicConfigFile _rbDataFile;
        private Dictionary<ulong, PlayerData> _dataPlayerRb;
        private DynamicConfigFile _rbNpcDataFile;
        private List<ulong> _dataNpc;
        private Dictionary<ulong, List<string>> OpenUiPanel = new Dictionary<ulong, List<string>>();
        private List<ulong> _managedAddList = new List<ulong>();
        private List<ulong> _managedRemoveList = new List<ulong>();


        [PluginReference] 
        private Plugin Economics;
        #endregion

        #region Classes

        /// <summary>
        /// プレイヤーデータファイル用
        /// </summary>
        private class PlayerData
        {
            public string DisplayName { get; set; }
            public double Balance { get; set; }
            public int Extension { get; set; }
        }

        /// <summary>
        /// コンフィグ用
        /// </summary>
        private class Configurations
        {
            //public bool UseEconomics { get; set; }
            public bool IsOnlyUi { get; set; }
            public bool IsExtension { get; set; }
            public double ExtensionFee { get; set; }
            public double ExtensionAmount { get; set; }
            public double ExtensionLimit { get; set; }
            public bool ShowVersion { get; set; }
            public float DistanceFromNpc { get; set; }
            public int TransactExpirationTime { get; set; }
            public AuthorityConfigs Admin { get; set; }
            public AuthorityConfigs Default { get; set; }
            public AuthorityConfigs Vip1 { get; set; }
            public AuthorityConfigs Vip2 { get; set; }
            public AuthorityConfigs Vip3 { get; set; }
            

            public Configurations()
            {
                this.Default = new AuthorityConfigs();
                this.Admin = new AuthorityConfigs();
                this.Vip1 = new AuthorityConfigs();
                this.Vip2 = new AuthorityConfigs();
                this.Vip3 = new AuthorityConfigs();
            }
        }

        /// <summary>
        /// 権限別コンフィグ
        /// </summary>
        private class AuthorityConfigs
        {
            public double MaxDepositBalance { get; set; }
            public bool IsFee { get; set; }
            public double Fee { get; set; }
        }
        #endregion

        #region Configurations
        private void LoadVariables()
        {
            //Global setting
            Configs.IsOnlyUi = Convert.ToBoolean(GetConfig("00_Global", "Use_Only_UI", false));
            
            Configs.IsExtension = Convert.ToBoolean(GetConfig("00_Global", "Extension_Enable", false));
            
            Configs.ExtensionFee = Convert.ToDouble(GetConfig("00_Global", "Extension_Fee", 100000.0));
            
            Configs.ExtensionAmount = Convert.ToDouble(GetConfig("00_Global", "Extension_Amount", 10000.0));
            
            Configs.ExtensionLimit = Convert.ToDouble(GetConfig("00_Global", "Extension_Limit", 500000.0));

            Configs.ShowVersion = Convert.ToBoolean(GetConfig("00_Global", "Show_Version", true));
            
            Configs.DistanceFromNpc = Convert.ToSingle(GetConfig("00_Global", "Distance_From_Npc", 5f));
            
            Configs.TransactExpirationTime = Convert.ToInt32(GetConfig("00_Global", "TransactExpirationTime", 5));

            //Admin
            Configs.Admin.MaxDepositBalance = Convert.ToDouble(GetConfig("10_Admin", "Max_Deposit_Balance", 300000));
            Configs.Admin.IsFee = Convert.ToBoolean(GetConfig("10_Admin", "Use_Fee", true));
            Configs.Admin.Fee = Convert.ToDouble(GetConfig("10_Admin", "Fee", 200));

            //Default
            Configs.Default.MaxDepositBalance = Convert.ToDouble(GetConfig("11_Default", "Max_Deposit_Balance", 150000));
            Configs.Default.IsFee = Convert.ToBoolean(GetConfig("11_Default", "Use_Fee", true));
            Configs.Default.Fee = Convert.ToDouble(GetConfig("11_Default", "Fee", 300));

            //Vip1
            Configs.Vip1.MaxDepositBalance = Convert.ToDouble(GetConfig("12_Vip1", "Max_Deposit_Balance", 200000));
            Configs.Vip1.IsFee = Convert.ToBoolean(GetConfig("12_Vip1", "Use_Fee", true));
            Configs.Vip1.Fee = Convert.ToDouble(GetConfig("12_Vip1", "Fee", 250));

            //Vip2
            Configs.Vip2.MaxDepositBalance = Convert.ToDouble(GetConfig("13_Vip2", "Max_Deposit_Balance", 250000));
            Configs.Vip2.IsFee = Convert.ToBoolean(GetConfig("13_Vip2", "Use_Fee", true));
            Configs.Vip2.Fee = Convert.ToDouble(GetConfig("13_Vip2", "Fee", 230));

            //Vip3
            Configs.Vip3.MaxDepositBalance = Convert.ToDouble(GetConfig("14_Vip3", "Max_Deposit_Balance", 300000));
            Configs.Vip3.IsFee = Convert.ToBoolean(GetConfig("14_Vip3", "Use_Fee", true));
            Configs.Vip3.Fee = Convert.ToDouble(GetConfig("14_Vip3", "Fee", 200));
        }

        private object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
            }
            return value;
        }
        private void RbLoadConfig()
        {
            LoadVariables();
        }
        #endregion

        #region Hooks
        
        /// <summary>
        /// Oxide Initialize
        /// </summary>
        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPerm", "You don't have a permission."},
                {"NoEconomics", "Economics plugin not found."},
                {"InternalError", "A serious error has occurred. Please contact the admin." },
                {"NotRun", "Plugin not working." },
                {"SyntaxError", "<color=red>Syntax error</color>"},
                {"PlayerNotFound", "Player not found."},
                {"PlayerDataNotFound", "Player data not found."},
                {"BalanceExceedsLimit", "Your balance has exceeded the limit. You cannot deposit."},
                {"NotEnoughMoney", "Not enough money in possession."},
                {"BalanceInsufficient", "Insufficient balance"},
                {"NotExistsAccount", "<color=#FFFFFF>You do not have an account.</color>"},
                {"SwitchAddMode", "Switched to \"Add a NPC banker\" mode."},
                {"SwitchRemoveMode", "Switched to \"Remove a NPC banker\" mode."},
                {"AlreadyAddMode", "Already in \"Add a NPC banker\" mode."},
                {"AlreadyRemoveMode", "Already in \"Remove a NPC banker\" mode."},
                {"TimeoutAddMode", "\"Add a NPC banker\" mode timed out."},
                {"TimeoutRemoveMode", "\"Remove a NPC banker\" mode timed out."},
                {"NumericError", "Please enter a number."},
                {"NumericalIntegrityError", "Please enter a number bigger than zero."},
                {"ExpansionSlotLimitExceeded", "You've exceeded the maximum balance expansion limit available for purchase."},
                {"CostForOpenAccount", "<color=#FFFFFF>You need {cost} to open an account.</color>"},
                {"OpenAnAccount", "<color=#FFFFFF>Open an account.</color>"},
                {"Close", "<color=#FFFFFF>Close</color>"},
                {"Manage", "<color=#000000>Manage</color>"},
                {"Expand", "<color=#FFFFFF>Expand</color>"},
                {"DepositT", "Deposit"},
                {"WithdrawT", "Withdraw"},
                {"TransactionFees", "<color=#FFFFFF>Transaction fees: {fees}</color>"},
                {"MoneyInPossession", "Money in possession"},
                {"DepositY", "Deposit"},
                {"EnterAnAmount.", "Please enter an amount."},
                {"DepositD", "Deposit"},
                {"WithdrawD", "Withdraw"},
                {"ExpandAccountDepositLimits", "You can expand your account deposit limits."},
                {"OneUnitOfExpansion", "One unit of expansion (additional 10,000 coins for deposit) can be purchased with 100,000 coins. (500,000 coins maximum)"},
                {"PurchaseWithTheMoneyInPossession", "Purchase with the money in possession."},
                {"WithdrawFromAnAccount", "Withdraw from your account."},
                {"CurrentDepositLimit", "Current Deposit Limit"},
                {"ConfirmDeposit", "Do you want to deposit the money? Amount: {amount}"},
                {"ConfirmWithdraw", "Do you want to withdraw the money? Amount: {amount}"},
            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPerm", "権限がありません。"},
                {"NoEconomics", "Economicsプラグインがロードされていません。"},
                {"InternalError", "深刻なエラーが発生しました。管理者に報告してください。" },
                {"NotRun", "プラグインが動いていません。" },
                {"SyntaxError", "<color=red>構文エラー</color>"},
                {"PlayerNotFound", "プレイヤーが見つかりません。"},
                {"PlayerDataNotFound", "プレイヤーデータが見つかりません。"},
                {"BalanceExceedsLimit", "残高が限度額を超えたため、入金できません。"},
                {"NotEnoughMoney", "所持金が足りません。"},
                {"BalanceInsufficient", "口座残高が不足しています。"},
                {"NotExistsAccount", "<color=#FFFFFF>口座が開設されていません。</color>"},
                {"SwitchAddMode", "銀行員追加モードに切り替わりました。"},
                {"SwitchRemoveMode", "銀行員削除モードに切り替わりました。"},
                {"AlreadyAddMode", "既に銀行員追加モードです。"},
                {"AlreadyRemoveMode", "既に銀行員削除モードです。"},
                {"TimeoutAddMode", "銀行員追加モードがタイムアウトしました。"},
                {"TimeoutRemoveMode", "銀行員削除モードがタイムアウトしました。"},
                {"NumericError", "数値でない文字が入力されました。"},
                {"NumericalIntegrityError", "0以上である必要があります。"},
                {"ExpansionSlotLimitExceeded", "購入可能な残高拡張枠の上限を超えています。"},
                {"CostForOpenAccount", "<color=#FFFFFF>口座の開設には{cost}必要です。</color>"},
                {"OpenAnAccount", "<color=#FFFFFF>アカウントを作成する</color>"},
                {"Close", "<color=#FFFFFF>閉じる</color>"},
                {"Manage", "<color=#000000>管理</color>"},
                {"Expand", "<color=#FFFFFF>拡張</color>"},
                {"DepositT", "入金"},
                {"WithdrawT", "出金"},
                {"TransactionFees", "<color=#FFFFFF>取引手数料: {fees}</color>"},
                {"MoneyInPossession", "手持ち"},
                {"DepositY", "預金"},
                {"EnterAnAmount.", "金額を入力してください"},
                {"DepositD", "入金する"},
                {"WithdrawD", "出金する"},
                {"ExpandAccountDepositLimits", "口座預金枠の拡張ができます。"},
                {"OneUnitOfExpansion", "1口(預金枠追加10,000コイン)を100,000コインで拡張できます。(上限: 500,000コイン)"},
                {"PurchaseWithTheMoneyInPossession", "手持金から購入"},
                {"WithdrawFromAnAccount", "口座から引落"},
                {"CurrentDepositLimit", "現在預金枠"},
                {"ConfirmDeposit", "入金します。よろしいですか?入金額: {amount}"},
                {"ConfirmWithdraw", "出金します。よろしいですか?出金額: {amount}"},
            }, this, "ja");
        }

        /// <summary>
        /// ロードデフォルトコンフィグ
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        /// <summary>
        /// Oxide Loaded
        /// </summary>
        private void Loaded()
        {
            permission.RegisterPermission(PermAdmin, this);
            permission.RegisterPermission(PermDefault, this);
            permission.RegisterPermission(PermVip1, this);
            permission.RegisterPermission(PermVip2, this);
            permission.RegisterPermission(PermVip3, this);
            
            _rbDataFile = GetFile("RustyBank/BankData");
            _dataPlayerRb = _rbDataFile.ReadObject<Dictionary<ulong, PlayerData>>();
            
            _rbNpcDataFile = GetFile("RustyBank/NpcData");
            _dataNpc = _rbNpcDataFile.ReadObject<List<ulong>>();

            RbLoadConfig();
        }
        
        /// <summary>
        /// Oxide OnServerSave
        /// </summary>
        private void OnServerSave()
        {
            SaveRbData();
        }
        
        private void OnServerShutdown() => OnServerSave();

        /// <summary>
        /// Oxide Unload
        /// </summary>
        private void Unload()
        {
            if (!Interface.Oxide.IsShuttingDown)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (!player || !player.IsConnected)
                    {
                        continue;
                    }
                    
                    DestroyEntries(player);
                    FuncDestroyUi(player);
                }
            }
            OnServerSave();
        }
        
        void OnPlayerConnected(BasePlayer player)
        {
            PlayerData playerData;
            if (_dataPlayerRb.TryGetValue(player.userID, out playerData))
            {
                if (string.IsNullOrEmpty(playerData.DisplayName) || !playerData.DisplayName.Equals(player.displayName))
                {
                    playerData.DisplayName = player.displayName;
                }
            }
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (_managedAddList.Contains(player.userID))
            {
                _dataNpc.Add(npc.userID);
                
                if (_managedAddList.Contains(player.userID))
                {
                    _managedAddList.Remove(player.userID);
                }
                else
                {
                    return;
                }
            }
            else if (_managedRemoveList.Contains(player.userID))
            {
                _dataNpc.Remove(npc.userID);
                
                if (_managedRemoveList.Contains(player.userID))
                {
                    _managedRemoveList.Remove(player.userID);
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (_dataNpc.Contains(npc.userID))
                {
                    RustyBankUiCrate(player, npc);
                }
            }
        }

        #endregion

        #region Functions
        private void SaveRbData()
        {
            if (_dataPlayerRb != null)
            {
                _rbDataFile.WriteObject(_dataPlayerRb);
            }

            if (_dataNpc != null)
            {
                _rbNpcDataFile.WriteObject(_dataNpc);
            }
        }

        private DynamicConfigFile GetFile(string name)
        {
            var file = Interface.Oxide.DataFileSystem.GetFile(name);
            file.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            file.Settings.Converters = new JsonConverter[] { new UnityVector3Converter(), new CustomComparerDictionaryCreationConverter<string>(StringComparer.OrdinalIgnoreCase) };
            return file;
        }

        /// <summary>
        /// Economicsの残高
        /// </summary>
        /// <returns></returns>
        private bool GetPossession(BasePlayer player, out double possession)
        {
            possession = 0.0;
            try
            {
                possession = Convert.ToDouble(Economics?.Call("Balance", player.userID));
                //SendMessage(player, possession.ToString());
            }
            catch
            {
                Puts("GetPossession Failure");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 預金残高取得
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <param name="balance">預金残高</param>
        /// <returns>bool 正常系/異常系</returns>
        private bool GetAccountBalance(BasePlayer player, out double balance)
        {
            balance = 0.0;
            try
            {
                PlayerData playerData;

                if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    playerData = new PlayerData()
                    {
                        DisplayName = player.displayName
                    };
                    _dataPlayerRb.Add(player.userID, playerData);
                }

                balance = playerData.Balance;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 手数料の取得
        /// </summary>
        /// <param name="player"></param>
        /// <returns>double fees: 手数料</returns>
        private double GetFees(BasePlayer player)
        {
            var fees = 0.0;
            if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                if (Configs.Admin.IsFee)
                {
                    fees = Configs.Admin.Fee;
                }
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip1))
            {
                if (Configs.Vip1.IsFee)
                {
                    fees = Configs.Vip1.Fee;
                }
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip2))
            {
                if (Configs.Default.IsFee)
                {
                    fees = Configs.Vip2.Fee;
                }
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip3))
            {
                if (Configs.Default.IsFee)
                {
                    fees = Configs.Vip3.Fee;
                }
            }
            else if (permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                if (Configs.Default.IsFee)
                {
                    fees = Configs.Default.Fee;
                }
            }

            return fees;
        }

        /// <summary>
        /// 預金額上限の取得
        /// </summary>
        /// <param name="player"></param>
        /// <returns>double balance: 最大預金額</returns>
        private double GetMaxDepositBalance(BasePlayer player)
        {
            var balance = 0.0;

            PlayerData playerData;
            if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
            {
                return balance;
            }
            
            if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                //balance = Configs.Admin.MaxDepositBalance;
                balance = Configs.Admin.MaxDepositBalance + (playerData.Extension * Configs.ExtensionAmount);
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip1))
            {
                balance = Configs.Vip1.MaxDepositBalance + (playerData.Extension * Configs.ExtensionAmount);
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip2))
            {
                balance = Configs.Vip2.MaxDepositBalance + (playerData.Extension * Configs.ExtensionAmount);
            }
            else if (permission.UserHasPermission(player.UserIDString, PermVip3))
            {
                balance = Configs.Vip3.MaxDepositBalance + (playerData.Extension * Configs.ExtensionAmount);
            }
            else if (permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                balance = Configs.Default.MaxDepositBalance + (playerData.Extension * Configs.ExtensionAmount);
            }

            return balance;
        }

        /// <summary>
        /// 権限チェック
        /// </summary>
        /// <param name="player"></param>
        /// <returns>bool 権限あり(true)/なし(false)</returns>
        private bool UseHasPerm(BasePlayer player) 
            => permission.UserHasPermission(player.UserIDString, PermAdmin) ||
               permission.UserHasPermission(player.UserIDString, PermVip1) ||
               permission.UserHasPermission(player.UserIDString, PermVip2) ||
               permission.UserHasPermission(player.UserIDString, PermVip3) ||
               permission.UserHasPermission(player.UserIDString, PermDefault);

        /// <summary>
        /// アカウント作成済みであるか
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <returns>bool true:作成済み/未作成:false</returns>
        private bool ExistsAccount(BasePlayer player)
        {
            PlayerData playerData;
            return _dataPlayerRb.TryGetValue(player.userID, out playerData);
        }

        /// <summary>
        /// UI削除
        /// </summary>
        /// <param name="player"></param>
        private void FuncDestroyUi(BasePlayer player)
        {
            DestroySubUi(player);
            CuiHelper.DestroyUi(player, MainUiName);
            CuiHelper.DestroyUi(player, NameLabelUiName);
            CuiHelper.DestroyUi(player, DepositUiName);
            CuiHelper.DestroyUi(player, WithdrawUiName);
            CuiHelper.DestroyUi(player, ShopUiName);
            CuiHelper.DestroyUi(player, OpMenuUiName);
            CuiHelper.DestroyUi(player, DaWUiName);
            RemoveUiPanel(player, MainUiName);
            RemoveUiPanel(player, NameLabelUiName);
            RemoveUiPanel(player, DepositUiName);
            RemoveUiPanel(player, WithdrawUiName);
            RemoveUiPanel(player, ShopUiName);
            RemoveUiPanel(player, OpMenuUiName);
            RemoveUiPanel(player, DaWUiName);
            
            FuncDestroyExtensionUi(player);
        }
        
        /// <summary>
        /// 拡張用UI削除(実行様)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemCounter"></param>
        private void FuncDestroyExtensionUi(BasePlayer player, int itemCounter = 3)
        {
            CuiHelper.DestroyUi(player, ExtensionUiName);
            RemoveUiPanel(player, ExtensionUiName);
            
            CuiHelper.DestroyUi(player, ExtensionSubUiName);
            RemoveUiPanel(player, ExtensionSubUiName);

            for (var i = 1; i <= itemCounter; i++)
            {
                CuiHelper.DestroyUi(player, $"{ExtensionItemUiName}{i}");
                RemoveUiPanel(player, $"{ExtensionItemUiName}{i}");
            }
        }

        /// <summary>
        /// サブUI削除
        /// </summary>
        /// <param name="player"></param>
        private void DestroySubUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, SubUiName);
            RemoveUiPanel(player, SubUiName);
        }
        
        /// <summary>
        /// 拡張用UI削除
        /// </summary>
        /// <param name="player"></param>
        private void DestroyExtensionSubUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ExtensionSubUiName);
            RemoveUiPanel(player, ExtensionSubUiName);
        }

        /// <summary>
        /// フェードUI
        /// </summary>
        /// <param name="player"></param>
        private void DestroyFadeUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FadeUiName);
            RemoveUiPanel(player, FadeUiName);
        }

        /// <summary>
        /// UI表示(条件分岐)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="npc"></param>
        private void RustyBankUiCrate(BasePlayer player, BasePlayer npc = null)
        {
            if (UseHasPerm(player))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player, npc);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else
            {
                SendMessage(player, lang.GetMessage("NoPerm", this, player.UserIDString));
            }
        }

        /// <summary>
        /// 拡張追加(手持ちから支払い)
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <param name="amount">追加口数</param>
        private void AddExtensionFromHand(BasePlayer player, int amount)
        {
            var cost = amount * Configs.ExtensionFee;
            //拡張容量確認
            PlayerData playerData;
            if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
            {
                return;
            }
            //現在の預金上限確認
            var maxDepositBalance = GetMaxDepositBalance(player);
            
            //現在の預金上限+ふやしたい枠 が上限を超えないか
            if (Configs.ExtensionLimit < maxDepositBalance + (amount * Configs.ExtensionAmount))
            {
                return;
            }
            //手持ち確認
            double possession;
            GetPossession(player, out possession);
            if (possession < cost)
            {
                return;
            }

            //拡張
            playerData.Extension += amount;

            //支払い
            try
            {
                var apiResult = Economics?.Call("Withdraw", player.userID, cost);
                if (apiResult == null || !(bool)apiResult)
                {
                    throw new Exception();
                }
            }
            catch
            {
                //
            }
            
            //データ格納
            _dataPlayerRb[player.userID] = playerData;

            return;
        }

        /// <summary>
        /// 拡張追加(口座から支払い)
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <param name="amount">追加口数</param>
        private void AddExtensionFromBank(BasePlayer player, int amount)
        {
            //拡張容量確認
            var cost = amount * Configs.ExtensionFee;
            PlayerData playerData;
            if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
            {
                return;
            }
            
            //拡張枠計算
            var extensionValue = amount * Configs.ExtensionAmount;

            //現在の預金上限確認
            var maxDepositBalance = GetMaxDepositBalance(player);
            
            //現在の預金上限+ふやしたい枠 が上限を超えないか
            if (Configs.ExtensionLimit < maxDepositBalance + extensionValue)
            {
                CreateFadeUI(player, lang.GetMessage("ExpansionSlotLimitExceeded", this, player.UserIDString));
                return;
            }
            
            //口座残高が購入額を超えてないか確認
            if (playerData.Balance < cost)
            {
                CreateFadeUI(player, lang.GetMessage("BalanceInsufficient", this, player.UserIDString));
                return;
            }
            
            //拡張
            playerData.Extension += amount;
            
            //支払い
            playerData.Balance -= cost;
            
            //データ格納
            _dataPlayerRb[player.userID] = playerData;

            return;
        }
        
        /// <summary>
        /// アカウント新規作成
        /// </summary>
        /// <param name="player"></param>
        private void CreateBlankAccountMenu(BasePlayer player)
        {
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float)0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.8), "0.01 0.01", "0.99 0.99", true);

            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
            //TODO: 消す
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), $"{this.Title} Ver.{this.Version}", 30, "0 0", "1 1");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //説明
            const double initialDeposit = 500;
            
            var fees = GetFees(player);

            var cost = initialDeposit + fees;

            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#FFA500", (float)1.0), lang.GetMessage("NotExistsAccount", this, player.UserIDString), 30, "0 0.5", "1 0.6");
            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#FFA500", (float)1.0), lang.GetMessage("CostForOpenAccount", this, player.UserIDString).Replace("{cost}", cost.ToString("#,0")), 30, "0 0.4", "1 0.5");
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#32CD32", (float)1.0), lang.GetMessage("OpenAnAccount", this, player.UserIDString), 18, "0.01 0.3", "0.99 0.4", "rustybank.createaccount");

            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), lang.GetMessage("Close", this, player.UserIDString), 18, "0.8 0.92", "0.99 0.97", "rustybank.destroyui");

            CuiHelper.AddUi(player, mainUiElement);
            AddUiPanel(player, MainUiName);
        }

        /// <summary>
        /// RustyBankメインメニュー
        /// </summary>
        /// <param name="player"></param>
        private void CreateRustyBankMenu(BasePlayer player, BasePlayer npc = null)
        {
            
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);

            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.3 0.99");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //管理パネル
            if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FFFF00", (float)1.0), lang.GetMessage("Manage", this, player.UserIDString), 18, "0.4 0.92", "0.59 0.99", "rustybank.adminui");
            }
            
            //拡張
            if (Configs.IsExtension)
            {
                UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#0000FF", (float)1.0), lang.GetMessage("Expand", this, player.UserIDString), 18, "0.6 0.92", "0.79 0.99", "rustybank.createbankextensionmenu");
            }
            
            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), lang.GetMessage("Close", this, player.UserIDString), 18, "0.8 0.92", "0.99 0.99", "rustybank.destroyui");
            
            var opMenuUiElement = UI.CreateElementContainer(OpMenuUiName, UI.Color("#f5f5f5", (float)0.4), "0.01 0.10", "0.99 0.75");

            //入金
            UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), lang.GetMessage("DepositT", this, player.UserIDString), 50, "0.05 0.25", "0.45 0.75", "rustybank.createdwui deposit");

            //出金
            UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), lang.GetMessage("WithdrawT", this, player.UserIDString), 50, "0.55 0.25", "0.95 0.75", "rustybank.createdwui withdraw");

            //説明Label
            var fees = GetFees(player);
            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.8), lang.GetMessage("TransactionFees", this, player.UserIDString).Replace("fees", fees.ToString("#,0")), 18, "0.1 0.03", "0.9 0.085");

            CuiHelper.AddUi(player, mainUiElement);
            CuiHelper.AddUi(player, nameLabelUiElement);
            CuiHelper.AddUi(player, opMenuUiElement);
            AddUiPanel(player, MainUiName);
            AddUiPanel(player, NameLabelUiName);
            AddUiPanel(player, OpMenuUiName);
            
            //SubUI
            UpdateSubUi(player);
        }

        /// <summary>
        /// サブUI更新用
        /// </summary>
        /// <param name="player"></param>
        private void UpdateSubUi(BasePlayer player)
        {
            if (IsUiPanelOpened(player, SubUiName))
            {
                DestroySubUi(player);
            }
            
            //TODO: ここから

            //SubUI
            var subUiElement = UI.CreateElementContainer(SubUiName, UI.Color("#000000", (float)0.0), "0.01 0.8", "0.99 0.9");
            UI.CreatePanel(ref subUiElement, SubUiName, UI.Color("#000000", (float)0.0), "0.1 0.01", "0.99 0.9", true);
            UI.CreateLabel(ref subUiElement, SubUiName, "", $"<=>", 30, "0.45 0.01", "0.55 0.9");

            //Label
            double _possession;
            if (GetPossession(player, out _possession))
            {
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"手持ち:{_possession:#,0}", 30, "0.01 0.01", "0.50 0.9");
            }
            else
            {
                UI.CreateLabel(ref subUiElement, SubUiName, "", "手持ち:...", 30, "0.01 0.01", "0.50 0.9");
            }

            double _balance;
            var maxDepositBalance = GetMaxDepositBalance(player);
            if (GetAccountBalance(player, out _balance))
            {
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance:#,0}/{maxDepositBalance:#,0}", 30, "0.51 0.01", "0.99 0.9");
            }
            else
            {
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance:#,0}/{maxDepositBalance:#,0}", 30, "0.51 0.01", "0.99 0.9");
            }

            CuiHelper.AddUi(player, subUiElement);
            AddUiPanel(player, SubUiName);
        }

        /// <summary>
        /// 入出金用UI
        /// </summary>
        /// <param name="player"></param>
        /// <param name="mode"></param>
        private void CreateDaWPanel(BasePlayer player, string mode)
        {
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);

            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //入力UI
            var opMenuUiElement = UI.CreateElementContainer(OpMenuUiName, UI.Color("#f5f5f5", (float)0.4), "0.01 0.10", "0.99 0.75");
            UI.CreateLabel(ref opMenuUiElement, OpMenuUiName, UI.Color("#FFFFFF", (float)1.0), $"↓金額を入力してください↓", 30, "0 0.75", "1 1");
            UI.CreatePanel(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), "0. 0.35", "1 0.65", true);
            UI.CreateInputField(ref opMenuUiElement, OpMenuUiName, UI.Color("#ffff00", (float)1.00), UI.Color("#ffff00", (float)1.00), 50, 8, "0 0", "1 1", $"rustybank.dw {mode}",TextAnchor.MiddleCenter, "0");
            
            //ボタン
            var DaWUiElement = UI.CreateElementContainer(DaWUiName, UI.Color("#000000", (float) 0.75), "0.25 0.1", "0.75 0.2");
            switch (mode)
            {
                case "deposit":
                    UI.CreateButton(ref DaWUiElement, DaWUiName, UI.Color("#47cc81",1f), "入金する", 25, "0 0", "1 1", "");
                    break;
                case "withdraw":
                    UI.CreateButton(ref DaWUiElement, DaWUiName, UI.Color("#47cc81",1f), "出金する", 25, "0 0", "1 1", "");
                    break;
            }
            
            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.99", "rustybank.destroyui");
            
            //説明Label
            var fees = GetFees(player);
            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.8), $"<color=#FFFFFF>取引手数料(Transaction Fees): {fees:#,0}</color>", 18, "0.1 0.03", "0.9 0.085");
            
            CuiHelper.AddUi(player, mainUiElement);
            CuiHelper.AddUi(player, nameLabelUiElement);
            CuiHelper.AddUi(player, opMenuUiElement);
            CuiHelper.AddUi(player, DaWUiElement);
            AddUiPanel(player, MainUiName);
            AddUiPanel(player, NameLabelUiName);
            AddUiPanel(player, OpMenuUiName);
            AddUiPanel(player, DaWUiName);
            
            //SubUI
            UpdateSubUi(player);
        }

        /// <summary>
        /// 預金額拡張用UI
        /// </summary>
        /// <param name="player"></param>
        private void CreateExtensionPanel(BasePlayer player)
        {
            var extensionUiElement = UI.CreateElementContainer(ExtensionUiName, UI.Color("#000000", (float)0.75), "0.01 0.25", "0.99 0.75");
            
            UI.CreatePanel(ref extensionUiElement, ExtensionUiName, UI.Color("#000000", (float)0.8), "0.0 0.0", "1.0 1.0", true);
            
            UI.CreateLabel(ref extensionUiElement, ExtensionUiName, "", "口座預金枠の拡張ができます。", 20, "0.0 0.8", "1.0 0.99");
            UI.CreateLabel(ref extensionUiElement, ExtensionUiName, "", $"1口(預金枠追加{Configs.ExtensionAmount:#,0}豚コイン)を{Configs.ExtensionFee:#,0}豚コインで拡張できます。(上限: {Configs.ExtensionLimit:#,0}豚コイン)", 20, "0.0 0.7", "1.0 0.89");
            
            //UI.CreateButton(ref extensionUiElement, ExtensionUiName, UI.Color("#0000FF", (float)1.0), "<color=#FFFFFF>購入</color>", 18, "0.56 0.4", "0.75 0.65", "");

            UI.CreateButton(ref extensionUiElement, ExtensionUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.99", $"rustybank.destroybankextensionmenu 3");
            
            CuiHelper.AddUi(player, extensionUiElement);
            AddUiPanel(player, ExtensionUiName);
            
            CreateExtensionItems(player, $"{ExtensionItemUiName}1", "0.01 0.25", "0.32 0.55", 1);
            CreateExtensionItems(player, $"{ExtensionItemUiName}2", "0.34 0.25", "0.65 0.55", 5);
            CreateExtensionItems(player, $"{ExtensionItemUiName}3", "0.66 0.25", "0.99 0.55", 10);

            CreateExtensionSubUI(player);
        }

        /// <summary>
        /// 預金額拡張用アイテム表示
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <param name="panelName"></param>
        /// <param name="aMin"></param>
        /// <param name="aMax"></param>
        /// <param name="amount"></param>
        private void CreateExtensionItems(BasePlayer player, string panelName, string aMin, string aMax, int amount)
        {
            var extensionItemsUiElement = UI.CreateElementContainer(panelName, UI.Color("#000000", (float)1.0), aMin, aMax);

            var price = amount * Configs.ExtensionFee;
            
            UI.CreateLabel(ref extensionItemsUiElement, panelName, "", $"{amount}口", 25, "0.0 0.75", "1.0 0.9");
            UI.CreateLabel(ref extensionItemsUiElement, panelName, "", $"{price:#,0}豚コイン", 25, "0.0 0.60", "1.0 0.75");
            UI.CreateButton(ref extensionItemsUiElement, panelName, UI.Color("#0000FF", (float)0.9), "<color=#FFFFFF>手持金から購入</color>", 18, "0.01 0.1", "0.4 0.3", $"rustybank.addbankextension t {amount}");
            if ((amount*price) < GetMaxDepositBalance(player))
            {
                UI.CreateButton(ref extensionItemsUiElement, panelName, UI.Color("#0000FF", (float)0.9), "<color=#FFFFFF>口座から引落</color>", 18, "0.6 0.1", "0.99 0.3", $"rustybank.addbankextension b {amount}");
            }
            
            CuiHelper.AddUi(player, extensionItemsUiElement);
            AddUiPanel(player, panelName);
        }

        private void CreateExtensionSubUI(BasePlayer player)
        {
            var maxDepositBalance = GetMaxDepositBalance(player);
            if (IsUiPanelOpened(player, ExtensionSubUiName))
            {
                DestroyExtensionSubUi(player);
            }
            //SubUI
            var subUiElement = UI.CreateElementContainer(ExtensionSubUiName, UI.Color("#000000", (float)0.0), "0.01 0.55", "0.99 0.65");

            //Label
            UI.CreateLabel(ref subUiElement, ExtensionSubUiName, "", $"現在預金枠: {maxDepositBalance:#,0}", 20, "0.25 0.01", "0.75 0.9");
            
            CuiHelper.AddUi(player, subUiElement);
            AddUiPanel(player, ExtensionSubUiName);
        }

        private void DestroyEntries(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "UIPanel");
            //CuiHelper.DestroyUi(player, "Entry");
            if (OpenUiPanel.ContainsKey(player.userID))
            {
                foreach (var entry in OpenUiPanel[player.userID])
                {
                    CuiHelper.DestroyUi(player, entry);
                }
                    
                OpenUiPanel.Remove(player.userID);
            }
        }

        private void CreateFadeUI(BasePlayer player, string text)
        {
            DestroyFadeUi(player);

            var fadeUiElement = UI.CreateElementContainerFade(FadeUiName, UI.Color("#000000", (float)0.75), "0.0 0.8", "1 0.9", 1.0f, 1.0f);
            UI.CreatePanelFade(ref fadeUiElement, FadeUiName, UI.Color("#FF0000", (float)0.8), "0.01 0.01", "0.99 0.99", 1.0f, 1.0f,true);
            UI.CreateLabelFade(ref fadeUiElement, FadeUiName, UI.Color("#FFF100", (float)1.0), text, 15, "0.01 0.01", "0.99 0.89", 1.0f, 1.0f);

            CuiHelper.AddUi(player, fadeUiElement);
            AddUiPanel(player, FadeUiName);

            timer.Once(3f, () =>
            {
                DestroyFadeUi(player);

            });
        }

        private void AddUiPanel(BasePlayer player, string panelName)
        {
            if (!OpenUiPanel.ContainsKey(player.userID))
                OpenUiPanel.Add(player.userID, new List<string>());
            OpenUiPanel[player.userID].Add(panelName);
        }

        private void RemoveUiPanel(BasePlayer player, string panelName)
        {
            List<string> uiList;
            if (OpenUiPanel.TryGetValue(player.userID, out uiList))
            {
                if (uiList.Contains(panelName))
                {
                    OpenUiPanel[player.userID].Remove(panelName);
                }
            }
        }

        private bool IsUiPanelOpened(BasePlayer player, string panelName)
        {
            if (!OpenUiPanel.ContainsKey(player.userID))
            {
                return false;
            }

            List<string> uiList;
            return OpenUiPanel.TryGetValue(player.userID, out uiList) && uiList.Contains(panelName);
        }

        /// <summary>
        /// 確認UI作成
        /// </summary>
        /// <param name="player"></param>
        /// <param name="mode"></param>
        /// <param name="amount"></param>
        private void CreateTransactionConfirmationUI(BasePlayer player, string mode, int amount)
        {
            FuncDestroyUi(player);
            
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);

            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");
            
            
            var opMenuUiElement = UI.CreateElementContainer(OpMenuUiName, UI.Color("#f5f5f5", (float)0.4), "0.01 0.10", "0.99 0.75");
            switch (mode){
                case "deposit":
                    UI.CreateLabel(ref opMenuUiElement, OpMenuUiName, UI.Color("#FFFFFF", (float)1.0), $"入金します。よろしいですか?入金額: {amount:#,0}", 30, "0 0.75", "1 1");
                    //OK
                    UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), "YES", 50, "0.55 0.25", "0.95 0.75", $"rustybank.deposit {amount}");
                    //NO
                    UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), "NO", 50, "0.05 0.25", "0.45 0.75", $"rustybank.bankui");
                    break;
                case "withdraw":
                    UI.CreateLabel(ref opMenuUiElement, OpMenuUiName, UI.Color("#FFFFFF", (float)1.0), $"出金します。よろしいですか?出金額: {amount:#,0}", 30, "0 0.75", "1 1");
                    //OK
                    UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), "YES", 50, "0.55 0.25", "0.95 0.75", $"rustybank.withdraw {amount}");
                    //NO
                    UI.CreateButton(ref opMenuUiElement, OpMenuUiName, UI.Color("#000000", (float)0.75), "NO", 50, "0.05 0.25", "0.45 0.75", $"rustybank.bankui");
                    break;
            }
            
            CuiHelper.AddUi(player, mainUiElement);
            CuiHelper.AddUi(player, nameLabelUiElement);
            CuiHelper.AddUi(player, opMenuUiElement);
            AddUiPanel(player, MainUiName);
            AddUiPanel(player, NameLabelUiName);
            AddUiPanel(player, OpMenuUiName);
        }
        
        private void CommandBankUiFromDW(BasePlayer player)
        {
            FuncDestroyUi(player);
            RustyBankUiCrate(player);

            return;
        }

        private void CreateAdminUi(BasePlayer player)
        {
            FuncDestroyUi(player);
            
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);
            
            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.3 0.99");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>戻る</color>", 18, "0.8 0.92", "0.99 0.99", "rustybank.bankui");
            
            //AdminUI
            var adminUiElement = UI.CreateElementContainer(AdminUiName, UI.Color("#f5f5f5", (float)0.4), "0 0", "1 0.9");
            UI.CreatePanel(ref adminUiElement, AdminUiName, UI.Color("#f5f5f5", (float)0.4), "0 0", "1 1", true);
        }
        #endregion

        #region Commands
        /// <summary>
        /// NPC設定用コマンド
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [ChatCommand("banknpc")]
        private void CommandBankNpc(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendMessage(player, lang.GetMessage("NoPerm", this, player.UserIDString));
                return;
            }
            
            if (args == null || args.Length == 0 || 1 < args.Length)
            {
                SendMessage(player, lang.GetMessage("SyntaxError", this, player.UserIDString));
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                {
                    if (_managedAddList.Contains(player.userID))
                    {
                        SendMessage(player, lang.GetMessage("AlreadyAddMode", this, player.UserIDString));
                    }
                    else
                    {
                        _managedAddList.Add(player.userID);
                        SendMessage(player, lang.GetMessage("SwitchAddMode", this, player.UserIDString));
                        timer.Once(30f, () =>
                        {
                            if (_managedAddList.Contains(player.userID))
                            {
                                _managedAddList.Remove(player.userID);
                                SendMessage(player, lang.GetMessage("TimeoutAddMode", this, player.UserIDString));
                            }
                        });
                    }

                    break;
                }
                case "remove":
                {
                    if (_managedRemoveList.Contains(player.userID))
                    {
                        SendMessage(player, lang.GetMessage("AlreadyRemoveMode", this, player.UserIDString));
                    }
                    else
                    {
                        _managedRemoveList.Add(player.userID);
                        SendMessage(player, lang.GetMessage("SwitchRemoveMode", this, player.UserIDString));
                        timer.Once(30f, () =>
                        {
                            if (_managedRemoveList.Contains(player.userID))
                            {
                                _managedRemoveList.Remove(player.userID);
                                SendMessage(player, lang.GetMessage("TimeoutRemoveMode", this, player.UserIDString));
                            }
                        });
                    }

                    break;
                }
            }
            
        }

        /// <summary>
        /// メインUI起動コマンド
        /// </summary>
        /// <param name="player"></param>
        [ChatCommand("bankui")]
        private void CommandBankUi(BasePlayer player)
        {
            // if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            // {
            //     //管理用UI
            //     CreateBankAdminMenu(player);
            //     return;
            // }
            if (Configs.IsOnlyUi)
            {
                SendMessage(player, lang.GetMessage("NoPerm", this, player.UserIDString));
                return;
            }
            
            FuncDestroyUi(player);
            RustyBankUiCrate(player);

            return;
        }

        [ConsoleCommand("rustybank.bankui")]
        private void CommandBankUi(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            FuncDestroyUi(player);
            CommandBankUiFromDW(player);
        }

        /// <summary>
        /// UI破棄コマンド
        /// </summary>
        /// <param name="args"></param>
        [ConsoleCommand("rustybank.destroyui")]
        private void DestroycUI(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            FuncDestroyUi(player);
        }

        /// <summary>
        /// 入金処理コマンド
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        [ConsoleCommand("rustybank.deposit")]
        private void RustyBankDepositCommand(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            try
            {
                var depositAmount = Convert.ToDouble(args.Args[0]);

                var fees = GetFees(player);

                var cost = depositAmount + fees;

                if (!UseHasPerm(player))
                {
                    CreateFadeUI(player, lang.GetMessage("NoPerm", this, player.UserIDString));
                    return;
                }

                if (depositAmount <= 0)
                {
                    CreateFadeUI(player, lang.GetMessage("NumericalIntegrityError", this, player.UserIDString));
                    return;
                }

                //Economics残高チェック
                var possession = Convert.ToDouble(Economics?.Call("Balance", player.userID));
                if (possession < cost)
                {
                    CreateFadeUI(player, lang.GetMessage("NotEnoughMoney", this, player.UserIDString));
                    return;
                }

                //RustyBank残高チェック
                PlayerData playerData;
                if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    playerData = new PlayerData()
                    {
                        DisplayName = player.displayName
                    };
                }
                else
                {
                    var isCalledEconomicsApi = false;
                    var isRustyBankBalance = false;

                    var maxDepositBalance = GetMaxDepositBalance(player);

                    if (maxDepositBalance < playerData.Balance + depositAmount)
                    {
                        CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this, player.UserIDString));
                        return;
                    }

                    try
                    {
                        var apiResult = Economics?.Call("Withdraw", player.userID, cost);
                        if (apiResult == null || !(bool)apiResult)
                        {
                            throw new Exception();
                        }
                        isCalledEconomicsApi = true;
                        playerData.Balance += depositAmount;
                        isRustyBankBalance = true;
                        if (!isCalledEconomicsApi || !isRustyBankBalance)
                        {
                            throw new Exception();
                        }
                    }
                    catch(Exception)
                    {
                        Puts("Deposit Exception");
                        if (isCalledEconomicsApi)
                        {
                            Economics?.Call("Deposit", player.userID, depositAmount);
                        }

                        if (isRustyBankBalance)
                        {
                            playerData.Balance -= depositAmount;
                        }
                    }
                }
            }
            catch
            {
                //
            }
            //UpdateSubUi(player);
            CommandBankUiFromDW(player);
        }
        
        /// <summary>
        /// 出金処理コマンド
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        [ConsoleCommand("rustybank.withdraw")]
        private void RustyBankWithdrawCommand(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            try
            {
                var withdrawAmount = Convert.ToDouble(args.Args[0]);

                var fees = GetFees(player);

                var cost = withdrawAmount + fees;
                
                if (!UseHasPerm(player))
                {
                    CreateFadeUI(player, lang.GetMessage("NoPerm", this, player.UserIDString));
                    return;
                }
                
                if (withdrawAmount <= 0)
                {
                    CreateFadeUI(player, lang.GetMessage("NumericalIntegrityError", this, player.UserIDString));
                    return;
                }

                //RustyBank残高チェック
                PlayerData playerData;
                if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    CreateFadeUI(player, lang.GetMessage("NotExistsAccount", this, player.UserIDString));
                }
                else
                {
                    if (playerData.Balance < cost)
                    {
                        CreateFadeUI(player, lang.GetMessage("BalanceInsufficient", this, player.UserIDString));
                        return;
                    }

                    bool isCalledEconomicsApi = false;
                    bool isRustyBankBalance = false;
                    try
                    {
                        var apiResult = Economics?.Call("Deposit", player.userID, withdrawAmount);
                        if (apiResult == null || !(bool)apiResult)
                        {
                            throw new Exception();
                        }
                        isCalledEconomicsApi = true;

                        playerData.Balance -= cost;
                        isRustyBankBalance = true;

                        if (!isCalledEconomicsApi || !isRustyBankBalance)
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception)
                    {
                        if (isCalledEconomicsApi)
                        {
                            Economics?.Call("Withdraw", player.userID, withdrawAmount);
                        }

                        if (isRustyBankBalance)
                        {
                            playerData.Balance += withdrawAmount;
                        }

                    }
                }
            }
            catch
            {
                //
            }
            //UpdateSubUi(player);
            CommandBankUiFromDW(player);
        }

        [ConsoleCommand("rustybank.createaccount")]
        private void RustyBankCreateAccountCommand(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;

            double initialDeposit = 500;
            //Economics残高チェック
            double inHand;
            GetPossession(player, out inHand);

            var fees = GetFees(player);

            var cost = initialDeposit + fees;

            if (cost <= inHand)
            {
                PlayerData playerData;
                if (_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    return;
                }
                else
                {
                    var apiResult = Economics?.Call("Withdraw", player.userID, cost);
                    if (apiResult == null || !(bool)apiResult)
                    {
                        CreateFadeUI(player, lang.GetMessage("InternalError", this, player.UserIDString));
                        return;
                    }

                    playerData = new PlayerData()
                    {
                        DisplayName = player.displayName,
                        Balance = initialDeposit
                    };
                    _dataPlayerRb.Add(player.userID, playerData);
                }
            }
            else
            {
                CreateFadeUI(player, lang.GetMessage("NotEnoughMoney", this, player.UserIDString));
                return;
            }

            FuncDestroyUi(player);
        }
        
        [ConsoleCommand("rustybank.createbankextensionmenu")]
        private void RustyBankCreateBankExtension(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            FuncDestroyUi(player);
            CreateExtensionPanel(player);
        }
        
        [ConsoleCommand("rustybank.destroybankextensionmenu")]
        private void RustyBankDestroyBankExtension(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            
            var itemCounter = Convert.ToInt32(args.Args[0]);

            FuncDestroyExtensionUi(player, itemCounter);
            
            CreateRustyBankMenu(player);
        }
        
        [ConsoleCommand("rustybank.addbankextension")]
        private void RustyBankAddBankExtension(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;

            if (args.Args.Length <= 0) return;

            var mode = args.Args[0];

            var amount = Convert.ToInt32(args.Args[1]);

            switch (mode)
            {
                case "t":
                    AddExtensionFromHand(player, amount);
                    break;
                case "b":
                    AddExtensionFromBank(player, amount);
                    break;
                default:
                    break;
            }
            CreateExtensionSubUI(player);
        }

        [ConsoleCommand("rustybank.createdwui")]
        private void RustyBankDaWUi(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            
            FuncDestroyUi(player);

            if (args.Args.Length <= 0) return;
            
            var mode = args.Args[0];

            CreateDaWPanel(player, mode);
        }

        [ConsoleCommand("rustybank.dw")]
        private void DepositOrWithdraw(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (arg.Args.Length != 2)
            {
                return;
            }

            var mode = arg.Args[0];
            var amount = 0;

            if (!int.TryParse(arg.Args[1], out amount))
            {
                CreateFadeUI(player, lang.GetMessage("NumericError", this, player.UserIDString));
                return;
            }
            
            if (amount <= 0)
            {
                CreateFadeUI(player, lang.GetMessage("NumericalIntegrityError", this, player.UserIDString));
                return;
            }

            CreateTransactionConfirmationUI(player, mode, amount);
        }

        [ConsoleCommand("rustybank.adminui")]
        private void CommandAdminUi(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            
            FuncDestroyUi(player);
            CreateAdminUi(player);
        }
        #endregion

        #region UI
        class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = cursor
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return newElement;
            }
            public static CuiElementContainer CreateElementContainerFade(string panelName, string color, string aMin, string aMax, float fadeInTime, float fadeOutTime, bool cursor = false)
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color, FadeIn = fadeInTime},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = cursor,
                            FadeOut = fadeOutTime,
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return newElement;
            }
            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            public static void CreatePanelFade(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, float fadeInTime, float fadeOutTime, bool cursor = false)
            {
                container.Add(new CuiPanel
                    {
                        Image = { Color = color, FadeIn = fadeInTime },
                        RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                        CursorEnabled = cursor,
                        FadeOut = fadeOutTime
                },
                    panel);
            }
            public static void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }
            public static void CreateLabelFade(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, float fadeInTime, float fadeOutTime, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                    {
                        Text = { Color = color, FontSize = size, Align = align, Text = text, FadeIn = fadeInTime },
                        RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                        FadeOut = fadeOutTime,
                    },
                    panel);
            }
            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command },
                    Text = { Text = text, FontSize = size, Align = align },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }

                }
                , panel);
            }

            public static void CreateImage(ref CuiElementContainer container, string panel, string color, string imageUrl, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = color,
                            Url = imageUrl,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax
                        }
                    }
                });
            }

            public static void CreateInputField(ref CuiElementContainer container, string panel, string textColor, string outlineColor,
                int fontSize, int limit, string aMin, string aMax, string command, TextAnchor align =  TextAnchor.LowerRight, string text="")
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Text = text,
                            CharsLimit = limit,
                            FontSize = fontSize,
                            Color = textColor,
                            Align =align,
                            IsPassword = false,
                            Command = command
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax
                        },
                        // new CuiOutlineComponent
                        // {
                        //     Color = outlineColor
                        // }
                    }
                });
            }
            
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region Helper
        private static class MathUtil
        {
            /// <summary>
            /// 球の内側か
            /// (x - a)^2 + (y - b)^2 + (z - c)^2 &gt;= r^2
            /// </summary>
            /// <param name="p">球の中心座標</param>
            /// <param name="r">半径</param>
            /// <param name="c">対象となる点</param>
            /// <returns></returns>
            public static bool InSphere(Vector3 p, float r, Vector3 c)
            {
                var sum = 0f;
                for (var i = 0; i < 3; i++)
                    sum += Mathf.Pow(p[i] - c[i], 2);
                return sum <= Mathf.Pow(r, 2f);
            }
 
            /// <summary>
            /// 円の内側か
            /// (x - a)^2 + (y - b)^2 &gt;= r^2
            /// </summary>
            /// <param name="p">円の中心座標</param>
            /// <param name="r">半径</param>
            /// <param name="c">対象となる点</param>
            /// <returns></returns>
            public static bool InCircle(Vector2 p, float r, Vector2 c)
            {
                var sum = 0f;
                for (var i = 0; i < 2; i++)
                    sum += Mathf.Pow(p[i] - c[i], 2);
                return sum <= Mathf.Pow(r, 2f);
            }
        }
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }
        private class CustomComparerDictionaryCreationConverter<T> : CustomCreationConverter<IDictionary>
        {
            private readonly IEqualityComparer<T> comparer;
            public CustomComparerDictionaryCreationConverter(IEqualityComparer<T> comparer)
            {
                if (comparer == null) throw new ArgumentNullException(nameof(comparer));
                this.comparer = comparer;
            }
            public override bool CanConvert(Type objectType)
            {
                return HasCompatibleInterface(objectType) && HasCompatibleConstructor(objectType);
            }
            private static bool HasCompatibleInterface(Type objectType)
            {
                return objectType.GetInterfaces().Where(i => HasGenericTypeDefinition(i, typeof(IDictionary<,>))).Any(i => typeof(T).IsAssignableFrom(i.GetGenericArguments().First()));
            }
            private static bool HasGenericTypeDefinition(Type objectType, Type typeDefinition)
            {
                return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeDefinition;
            }
            private static bool HasCompatibleConstructor(Type objectType)
            {
                return objectType.GetConstructor(new[] { typeof(IEqualityComparer<T>) }) != null;
            }
            public override IDictionary Create(Type objectType)
            {
                return Activator.CreateInstance(objectType, comparer) as IDictionary;
            }
        }
        void SendMessage(BasePlayer player, string message, params object[] args) => PrintToChat(player, PName + message, args);
        private void Log(string text) => LogToFile("RustyBank", $"[{DateTime.Now}] {text}", this);
        #endregion
    }
}
