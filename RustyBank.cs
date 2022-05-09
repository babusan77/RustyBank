using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Models.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Database;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rusty Bank", "babu77", "1.0.0")]
    [Description("A simple plugin to introduce the bank.")]
    public class RustyBank : RustPlugin
    {
        #region Definitions(Global)
        private const string PName = "<color=yellow>[RustyBank]:</color>";
        private const string PermDefault = "rustybank.default";
        private const string PermVip1 = "rustybank.vip1";
        private const string PermVip2 = "rustybank.vip2";
        private const string PermVip3 = "rustybank.vip3";
        private const string PermAdmin = "rustybank.admin";
       // private const string AdminUiName = "RustyBankAdminUI";
        private const string ExtensionUiName = "RustyBankUIExtension";
        private const string ExtensionItemUiName = "RustyBankUIExtensionItem_";
        private const string MainUiName = "RustyBankUIMain";
        private const string SubUiName = "RustyBankUISub";
        private const string ExtensionSubUiName = "RustyBankUIExtensionSub";
        private const string FadeUiName = "RustyBankUIFade";
        private const string NameLabelUiName = "RustyBankUINameLabel";
        private const string DepositUiName = "RustyBankUIDeposit";
        private const string WithdrawUiName = "RustyBankUIWithdraw";
        private const string ShopUiName = "RustyBankUIShop";
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

        private class PlayerData
        {
            public string DisplayName { get; set; }
            public double Balance { get; set; }
            public int Extension { get; set; }
        }

        private class Configurations
        {
            //public bool UseEconomics { get; set; }
            //public DbConnection MySqlConnection { get; set; }
            public bool IsOnlyUi { get; set; }
            public bool IsExtension { get; set; }
            public double ExtensionFee { get; set; }
            public double ExtensionAmount { get; set; }
            public double ExtensionLimit { get; set; }
            public bool ShowVersion { get; set; }
            public UserConfigs Admin { get; set; }
            public UserConfigs Default { get; set; }
            public UserConfigs Vip1 { get; set; }
            public UserConfigs Vip2 { get; set; }
            public UserConfigs Vip3 { get; set; }
            

            public Configurations()
            {
                //this.MySqlConnection = new DbConnection();
                this.Default = new UserConfigs();
                this.Admin = new UserConfigs();
                this.Vip1 = new UserConfigs();
                this.Vip2 = new UserConfigs();
                this.Vip3 = new UserConfigs();
            }
        }

        private class UserConfigs
        {
            public double MaxDepositBalance { get; set; }
            public bool IsFee { get; set; }
            public double Fee { get; set; }
        }

        private class DbConnection
        {
            public string Address { get; set; }
            public int Port { get; set; }
            public string DbName { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }
        #endregion

        #region Configurations
        //private bool Changed;
        //
        private void LoadVariables()
        {
            //Global setting
            //Configs.UseEconomics = Convert.ToBoolean(GetConfig("00_Global", "Use_Economics", false));
            Configs.IsOnlyUi = Convert.ToBoolean(GetConfig("00_Global", "Use_Only_UI", false));
            
            Configs.IsExtension = Convert.ToBoolean(GetConfig("00_Global", "Extension_Enable", false));
            
            Configs.ExtensionFee = Convert.ToDouble(GetConfig("00_Global", "Extension_Fee", 100000.0));
            
            Configs.ExtensionAmount = Convert.ToDouble(GetConfig("00_Global", "Extension_Amount", 10000.0));
            
            Configs.ExtensionLimit = Convert.ToDouble(GetConfig("00_Global", "Extension_Limit", 500000.0));

            Configs.ShowVersion = Convert.ToBoolean(GetConfig("00_Global", "Show_Version", true));

            //MysqlConnection
            // Configs.MySqlConnection.Address = Convert.ToString(GetConfig("01_MySql", "Address", "localhost"));
            // Configs.MySqlConnection.Port = Convert.ToInt32(GetConfig("01_MySql", "Port", 3306));
            // Configs.MySqlConnection.DbName = Convert.ToString(GetConfig("01_MySql", "DataBase", "DB_RustyBank"));
            // Configs.MySqlConnection.UserName = Convert.ToString(GetConfig("01_MySql", "UserName", "RustyBank"));
            // Configs.MySqlConnection.Password = Convert.ToString(GetConfig("01_MySql", "Password", "password"));

            //Admin
            Configs.Admin.MaxDepositBalance = Convert.ToDouble(GetConfig("10_Admin", "Max_Deposit_Balance", 300000));
            Configs.Admin.IsFee = Convert.ToBoolean(GetConfig("10_Admin", "Use_Fee", true));
            //Configs.Admin.FeeType = Convert.ToInt32(GetConfig("10_Admin", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Admin.Fee = Convert.ToDouble(GetConfig("10_Admin", "Fee", 200));
            //Configs.Admin.FeeInterval = Convert.ToSingle(GetConfig("10_Admin", "Fee_Interval", 3600));

            //Default
            Configs.Default.MaxDepositBalance = Convert.ToDouble(GetConfig("11_Default", "Max_Deposit_Balance", 150000));
            Configs.Default.IsFee = Convert.ToBoolean(GetConfig("11_Default", "Use_Fee", true));
            //Configs.Default.FeeType = Convert.ToInt32(GetConfig("11_Default", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Default.Fee = Convert.ToDouble(GetConfig("11_Default", "Fee", 300));
            //Configs.Default.FeeInterval = Convert.ToSingle(GetConfig("11_Default", "Fee_Interval", 3600));

            //Vip1
            Configs.Vip1.MaxDepositBalance = Convert.ToDouble(GetConfig("12_Vip1", "Max_Deposit_Balance", 200000));
            Configs.Vip1.IsFee = Convert.ToBoolean(GetConfig("12_Vip1", "Use_Fee", true));
            //Configs.Vip1.FeeType = Convert.ToInt32(GetConfig("12_Vip1", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Vip1.Fee = Convert.ToDouble(GetConfig("12_Vip1", "Fee", 250));
            //Configs.Vip1.FeeInterval = Convert.ToSingle(GetConfig("12_Vip1", "Fee_Interval", 3600));
            
            //Vip2
            Configs.Vip2.MaxDepositBalance = Convert.ToDouble(GetConfig("13_Vip2", "Max_Deposit_Balance", 250000));
            Configs.Vip2.IsFee = Convert.ToBoolean(GetConfig("13_Vip2", "Use_Fee", true));
            //Configs.Vip2.FeeType = Convert.ToInt32(GetConfig("13_Vip2", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Vip2.Fee = Convert.ToDouble(GetConfig("13_Vip2", "Fee", 230));
            //Configs.Vip2.FeeInterval = Convert.ToSingle(GetConfig("13_Vip2", "Fee_Interval", 3600));
            
            //Vip3
            Configs.Vip3.MaxDepositBalance = Convert.ToDouble(GetConfig("14_Vip3", "Max_Deposit_Balance", 300000));
            Configs.Vip3.IsFee = Convert.ToBoolean(GetConfig("14_Vip3", "Use_Fee", true));
            //Configs.Vip3.FeeType = Convert.ToInt32(GetConfig("14_Vip3", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Vip3.Fee = Convert.ToDouble(GetConfig("14_Vip3", "Fee", 200));
            //Configs.Vip3.FeeInterval = Convert.ToSingle(GetConfig("14_Vip3", "Fee_Interval", 3600));
        }

        private object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                //Changed = true;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
                //Changed = true;
            }
            return value;
        }
        private void RbLoadConfig()
        {
            LoadVariables();
        }
        #endregion

        #region Hooks
        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPerm", "You don't have a permission."},
                {"NoEconomics", "Can't find Economics Plugin."},
                {"InternalError", "A serious error has occurred in the plugin. Please report this to the administrator." },
                {"NotRun", "Plugin is not running." },
                {"SyntaxError", "<color=red>SyntaxError</color>"},
                {"PlayerNotFound", "Specified player not found."},
                {"PlayerDataNotFound", "The data for the specified player could not be found."},
                {"TargetPlayerDidNotHavePerm", "{target} did not have permission."},
                {"BalanceExceedsLimit", "Unable to deposit because the bank balance exceeds the limit."},
                {"DoNotHavePossession", "You don't have enough money in your possession."},
                {"BalanceInsufficient.", "Your balance is insufficient."},
                {"NotExistsAccount", "Your don't have account."},
                {"SwitchAddMode", "You have switched to additional mode."},
                {"SwitchRemoveMode", "You have switched to remove mode."},
                {"AlreadyAddMode", "You have already switched to the additional mode."},
                {"AlreadyRemoveMode", "You have already switched to the remove mode."},
                {"TimeoutAddMode", "Additional mode timed out."},
                {"TimeoutRemoveMode", "Remove mode timed out."},
                {"MultiplePlayers", "Multiple players found."},
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

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
        private void OnServerSave()
        {
            SaveRbData();
        }
        private void OnServerShutdown() => OnServerSave();

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

                    List<string> uiList;
                    if (OpenUiPanel.TryGetValue(player.userID, out uiList))
                    {
                        foreach (var uiName in uiList)
                        {
                            switch (uiName)
                            {
                                case MainUiName:
                                    CuiHelper.DestroyUi(player, MainUiName);
                                    break;
                                case SubUiName:
                                    CuiHelper.DestroyUi(player, SubUiName);
                                    break;
                            }
                        }
                    }
                    DestroyEntries(player);
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
                    RustyBankUiCrate(player);
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

        //private void OnNewSave()
        //{
        //   // DATA_PLAYER_LOST_CR.Clear();
        //}

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

            PlayerData playerData = new PlayerData();
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
            //CuiHelper.DestroyUi(player, ExtensionUiName);
            //CuiHelper.DestroyUi(player, AdminUiName);
            //RemoveUiPanel(player, AdminUiName);
            RemoveUiPanel(player, MainUiName);
            RemoveUiPanel(player, NameLabelUiName);
            RemoveUiPanel(player, DepositUiName);
            RemoveUiPanel(player, WithdrawUiName);
            RemoveUiPanel(player, ShopUiName);
            //RemoveUiPanel(player, ExtensionUiName);
            //CuiHelper.DestroyUi(player, "db");
            //DestroyEntries(player);
            FuncDestroyExtensionUi(player);
        }
        
        /// <summary>
        /// 拡張用UI削除
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
        
        //private void DestroySubUI(ConsoleSystem.Arg args)
        //{
        //    var player = args.Connection.player as BasePlayer;
        //    CuiHelper.DestroyUi(player, _SubUiName);
        //    RemoveUiPanel(player, _SubUiName);
        //}

        private void DestroySubUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, SubUiName);
            RemoveUiPanel(player, SubUiName);
        }
        
        private void DestroyExtentionSubUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ExtensionSubUiName);
            RemoveUiPanel(player, ExtensionSubUiName);
        }

        private void DestroyFadeUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FadeUiName);
            RemoveUiPanel(player, FadeUiName);
        }

        /// <summary>
        /// UI表示(条件分岐)
        /// </summary>
        /// <param name="player"></param>
        private void RustyBankUiCrate(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else if(permission.UserHasPermission(player.UserIDString, PermVip1))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else if(permission.UserHasPermission(player.UserIDString, PermVip2))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else if(permission.UserHasPermission(player.UserIDString, PermVip3))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else if(permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                if (ExistsAccount(player))
                {
                    CreateRustyBankMenu(player);
                }
                else
                {
                    CreateBlankAccountMenu(player);
                }
            }
            else
            {
                SendMessage(player, lang.GetMessage("NoPerm", this));
            }
        }

        /// <summary>
        /// 拡張追加(手持ちから支払い)
        /// </summary>
        /// <param name="player"></param>
        private void AddExtensionFromHand(BasePlayer player, int amount)
        {
            var cost = amount * Configs.ExtensionFee;
            //拡張容量確認
            PlayerData playerData = new PlayerData();
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
        /// <param name="player"></param>
        private void AddExtensionFromBank(BasePlayer player, int amount)
        {
            //拡張容量確認
            var cost = amount * Configs.ExtensionFee;
            PlayerData playerData = new PlayerData();
            if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
            {
                return;
            }
            
            //口座残高確認
            
            //現在の預金上限確認
            var maxDepositBalance = GetMaxDepositBalance(player);
            
            //現在の預金上限+ふやしたい枠 が上限を超えないか
            if (Configs.ExtensionLimit < maxDepositBalance + (amount * Configs.ExtensionAmount))
            {
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
        #endregion

        #region Commands
        [ChatCommand("banknpc")]
        private void CommandBankNpc(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendMessage(player, lang.GetMessage("NoPerm", this));
                return;
            }
            
            if (args == null || args.Length == 0 || 1 < args.Length)
            {
                SendMessage(player, lang.GetMessage("SyntaxError", this));
                return;
            }

            if (args[0].ToLower().Equals("add"))
            {
                if (_managedAddList.Contains(player.userID))
                {
                    SendMessage(player, lang.GetMessage("AlreadyAddMode", this));
                }
                else
                {
                    _managedAddList.Add(player.userID);
                    SendMessage(player, lang.GetMessage("SwitchAddMode", this));
                    timer.Once(30f, () =>
                    {
                        if (_managedAddList.Contains(player.userID))
                        {
                            _managedAddList.Remove(player.userID);
                            SendMessage(player, lang.GetMessage("TimeoutAddMode", this));
                        }
                    });
                }
            }
            else if(args[0].ToLower().Equals("remove"))
            {
                if (_managedRemoveList.Contains(player.userID))
                {
                    SendMessage(player, lang.GetMessage("AlreadyRemoveMode", this));
                }
                else
                {
                    _managedRemoveList.Add(player.userID);
                    SendMessage(player, lang.GetMessage("SwitchRemoveMode", this));
                    timer.Once(30f, () =>
                    {
                        if (_managedRemoveList.Contains(player.userID))
                        {
                            _managedRemoveList.Remove(player.userID);
                            SendMessage(player, lang.GetMessage("TimeoutRemoveMode", this));
                        }
                    });
                }
            }
            
        }

        [ChatCommand("bank")]
        private void CommandBank(BasePlayer player, string command, string[] args)
        {
            // if (permission.UserHasPermission(player.UserIDString, PermAdmin))
            // {
            //     //管理用UI
            //     CreateBankAdminMenu(player);
            //     return;
            // }
            if (Configs.IsOnlyUi)
            {
                SendMessage(player, lang.GetMessage("NoPerm", this));
                return;
            }
            
            RustyBankUiCrate(player);

            return;
        }

        [ConsoleCommand("rustybank.destroyui")]
        private void DestroycUI(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            FuncDestroyUi(player);
        }

        /// <summary>
        /// 入金処理
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        [ConsoleCommand("rustybank.deposit")]
        private void RustyBankDepositCommand(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            try
            {
                double depositAmount = Convert.ToDouble(args.Args[0]);

                double fees = GetFees(player);

                double cost = depositAmount + fees;

                if (!UseHasPerm(player))
                {
                    CreateFadeUI(player, lang.GetMessage("NoPerm", this));
                    return;
                }

                //Economics残高チェック
                double possession = Convert.ToDouble(Economics?.Call("Balance", player.userID));
                if (possession < cost)
                {
                    CreateFadeUI(player, lang.GetMessage("DoNotHavePossession", this));
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
                    bool isCalledEconomicsApi = false;
                    bool isRustyBankBalance = false;

                    var maxDepositBalance = GetMaxDepositBalance(player);

                    if (maxDepositBalance < playerData.Balance + depositAmount)
                    {
                        CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
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
                    
                    // if (permission.UserHasPermission(player.UserIDString, PermAdmin))
                    // {
                    //     if (Configs.Admin.MaxDepositBalance < playerData.Balance + depositAmount)
                    //     {
                    //         CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                    //         return;
                    //     }
                    //
                    //     try
                    //     {
                    //         var apiResult = Economics?.Call("Withdraw", player.userID, cost);
                    //         if (apiResult == null || !(bool)apiResult)
                    //         {
                    //             throw new Exception();
                    //         }
                    //         isCalledEconomicsApi = true;
                    //         playerData.Balance += depositAmount;
                    //         isRustyBankBalance = true;
                    //         if (!isCalledEconomicsApi || !isRustyBankBalance)
                    //         {
                    //             throw new Exception();
                    //         }
                    //     }
                    //     catch(Exception)
                    //     {
                    //         Puts("Deposit Exception");
                    //         if (isCalledEconomicsApi)
                    //         {
                    //             Economics?.Call("Deposit", player.userID, depositAmount);
                    //         }
                    //
                    //         if (isRustyBankBalance)
                    //         {
                    //             playerData.Balance -= depositAmount;
                    //         }
                    //     }
                    // }
                    // else if (permission.UserHasPermission(player.UserIDString, PermVip1))
                    // {
                    //     if (Configs.Vip1.MaxDepositBalance < playerData.Balance + depositAmount)
                    //     {
                    //         CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                    //         return;
                    //     }
                    //     try
                    //     {
                    //         var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
                    //         if (apiResult == null || !(bool)apiResult)
                    //         {
                    //             throw new Exception();
                    //         }
                    //         isCalledEconomicsApi = true;
                    //         playerData.Balance += depositAmount;
                    //         isRustyBankBalance = true;
                    //
                    //         if (!isCalledEconomicsApi || !isRustyBankBalance)
                    //         {
                    //             throw new Exception();
                    //         }
                    //     }
                    //     catch (Exception)
                    //     {
                    //         if (isCalledEconomicsApi)
                    //         {
                    //             Economics?.Call("Deposit", player.userID, depositAmount);
                    //         }
                    //
                    //         if (isRustyBankBalance)
                    //         {
                    //             playerData.Balance -= depositAmount;
                    //         }
                    //     }
                    // }
                    // else if (permission.UserHasPermission(player.UserIDString, PermVip2))
                    // {
                    //     if (Configs.Vip2.MaxDepositBalance < playerData.Balance + depositAmount)
                    //     {
                    //         CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                    //         return;
                    //     }
                    //     try
                    //     {
                    //         var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
                    //         if (apiResult == null || !(bool)apiResult)
                    //         {
                    //             throw new Exception();
                    //         }
                    //         isCalledEconomicsApi = true;
                    //         playerData.Balance += depositAmount;
                    //         isRustyBankBalance = true;
                    //
                    //         if (!isCalledEconomicsApi || !isRustyBankBalance)
                    //         {
                    //             throw new Exception();
                    //         }
                    //     }
                    //     catch (Exception)
                    //     {
                    //         if (isCalledEconomicsApi)
                    //         {
                    //             Economics?.Call("Deposit", player.userID, depositAmount);
                    //         }
                    //
                    //         if (isRustyBankBalance)
                    //         {
                    //             playerData.Balance -= depositAmount;
                    //         }
                    //     }
                    // }
                    // else if (permission.UserHasPermission(player.UserIDString, PermVip3))
                    // {
                    //     if (Configs.Vip3.MaxDepositBalance < playerData.Balance + depositAmount)
                    //     {
                    //         CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                    //         return;
                    //     }
                    //     try
                    //     {
                    //         var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
                    //         if (apiResult == null || !(bool)apiResult)
                    //         {
                    //             throw new Exception();
                    //         }
                    //         isCalledEconomicsApi = true;
                    //         playerData.Balance += depositAmount;
                    //         isRustyBankBalance = true;
                    //
                    //         if (!isCalledEconomicsApi || !isRustyBankBalance)
                    //         {
                    //             throw new Exception();
                    //         }
                    //     }
                    //     catch (Exception)
                    //     {
                    //         if (isCalledEconomicsApi)
                    //         {
                    //             Economics?.Call("Deposit", player.userID, depositAmount);
                    //         }
                    //
                    //         if (isRustyBankBalance)
                    //         {
                    //             playerData.Balance -= depositAmount;
                    //         }
                    //     }
                    // }
                    // else if (permission.UserHasPermission(player.UserIDString, PermDefault))
                    // {
                    //     if (Configs.Default.MaxDepositBalance < playerData.Balance + depositAmount)
                    //     {
                    //         CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                    //         return;
                    //     }
                    //     try
                    //     {
                    //         var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
                    //         if (apiResult == null || !(bool)apiResult)
                    //         {
                    //             throw new Exception();
                    //         }
                    //         isCalledEconomicsApi = true;
                    //         playerData.Balance += depositAmount;
                    //         isRustyBankBalance = true;
                    //
                    //         if (!isCalledEconomicsApi || !isRustyBankBalance)
                    //         {
                    //             throw new Exception();
                    //         }
                    //     }
                    //     catch (Exception)
                    //     {
                    //         if (isCalledEconomicsApi)
                    //         {
                    //             Economics?.Call("Deposit", player.userID, depositAmount);
                    //         }
                    //
                    //         if (isRustyBankBalance)
                    //         {
                    //             playerData.Balance -= depositAmount;
                    //         }
                    //     }
                    // }
                    // else
                    // {
                    //     CreateFadeUI(player, lang.GetMessage("NoPerm", this));
                    //     return;
                    // }
                }
            }
            catch
            {
                //
            }
            UpdateSubUi(player);

        }
        
        /// <summary>
        /// 出金処理
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
                    CreateFadeUI(player, lang.GetMessage("NoPerm", this));
                    return;
                }

                //RustyBank残高チェック
                PlayerData playerData;
                if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    CreateFadeUI(player, lang.GetMessage("NotExistsAccount", this));
                }
                else
                {
                    if (playerData.Balance < cost)
                    {
                        CreateFadeUI(player, lang.GetMessage("BalanceInsufficient", this));
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
            UpdateSubUi(player);
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
                        CreateFadeUI(player, lang.GetMessage("InternalError", this));
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
                CreateFadeUI(player, lang.GetMessage("DoNotHavePossession", this));
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
        
        //TODO: 口座OR手持ちから購入
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

        // [ConsoleCommand("rustybank.createBankMenuFromAdmin")]
        // private void RustyBankCreateBankMenuFromAdmin(ConsoleSystem.Arg args)
        // {
        //     var player = args.Connection.player as BasePlayer;
        //
        //     FuncDestroyUi(player);
        //     CommandBank(player, null, null);
        // }
        
        // [ConsoleCommand("rustybank.createbankshop")]
        // private void RustyBankCreateBankShopCommand(ConsoleSystem.Arg args)
        // {
        //     var player = args.Connection.player as BasePlayer;
        //
        //     Puts("STEP1");
        //     
        //     FuncDestroyUi(player);
        //
        //     Puts("STEP2");
        //     
        //     CreateBankShopUi(player);
        //     
        //     Puts("STEP3");
        // }
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

        #region UI Creation

        /// <summary>
        /// 管理メニュー
        /// </summary>
        /// <param name="player"></param>
        // private void CreateBankAdminMenu(BasePlayer player)
        // {
        //     //MainUI
        //     var mainUiElement = UI.CreateElementContainer(AdminUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
        //     UI.CreatePanel(ref mainUiElement, AdminUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);
        //
        //     //Label: Name
        //     var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
        //     UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), $"{this.Title} Ver.{this.Version}", 30, "0 0", "1 1");
        //     
        //     //Close
        //     UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.97", "rustybank.destroyui");
        //
        //     UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#32CD32", (float)1.0), "<color=#FFFFFF>プレイヤー残高照会</color>", 18, "0.01 0.3", "0.99 0.4", "rustybank.createaccount");
        //     UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#32CD32", (float)1.0), "<color=#FFFFFF>RustyBankメインメニュー</color>", 18, "0.01 0.5", "0.99 0.6", "rustybank.createBankMenuFromAdmin");
        //     
        //     
        //     CuiHelper.AddUi(player, mainUiElement);
        //     AddUiPanel(player, MainUiName);
        // }
        
        
        
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
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), $"{this.Title} Ver.{this.Version}", 30, "0 0", "1 1");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //説明
            const double initialDeposit = 500;
            
            var fees = GetFees(player);

            var cost = initialDeposit + fees;

            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#FFA500", (float)1.0), "<color=#FFFFFF>RustyBankアカウントがありません。</color>", 30, "0 0.5", "1 0.6");
            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#FFA500", (float)1.0), $"<color=#FFFFFF>アカウントの作成には{cost}豚コインが必要です。</color>", 30, "0 0.4", "1 0.5");
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#32CD32", (float)1.0), "<color=#FFFFFF>アカウントを作成する</color>", 18, "0.01 0.3", "0.99 0.4", "rustybank.createaccount");

            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.97", "rustybank.destroyui");

            CuiHelper.AddUi(player, mainUiElement);
            AddUiPanel(player, MainUiName);
        }

        /// <summary>
        /// RustyBankメインメニュー
        /// </summary>
        /// <param name="player"></param>
        private void CreateRustyBankMenu(BasePlayer player)
        {
            
            //MainUI
            var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
            UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);

            //Label: Name
            var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), Configs.ShowVersion? $"{this.Title} Ver.{this.Version}" : $"{this.Title}", 30, "0 0", "1 1");

            //入金
            var depositUiElement = UI.CreateElementContainer(DepositUiName, UI.Color("#f5f5f5", (float)0.4), "0.01 0.10", "0.475 0.75");
            //UI.CreateLabel(ref mainUiElement, _MainUiName, "", "Deposit", 15, "0.05 0.7", "0.95 0.8");
            UI.CreateLabel(ref depositUiElement, DepositUiName, "", "入金", 25, "0.05 0.85", "0.95 0.99");

            //UI.CreateButton(ref mainUiElement, _MainUiName, UI.Color("#0000FF", (float)0.5), "<color=#FFFFFF>50</color>", 18, "0.02 0.6", "0.124 0.7", "rustybank.deposit 50");
            //UI.CreateButton(ref mainUiElement, _MainUiName, UI.Color("#0000FF", (float)0.5), "<color=#FFFFFF>100</color>", 18, "0.127 0.6", "0.249 0.7", "rustybank.deposit 100");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>500</color>", 20, "0.02 0.63", "0.49 0.84", "rustybank.deposit 500");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>1,000</color>", 20, "0.52 0.63", "0.98 0.84", "rustybank.deposit 1000");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>5,000</color>", 20, "0.02 0.32", "0.49 0.61", "rustybank.deposit 5000");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>10,000</color>", 20, "0.52 0.32", "0.98 0.61", "rustybank.deposit 10000");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>50,000</color>", 20, "0.02 0.01", "0.49 0.3", "rustybank.deposit 50000");
            UI.CreateButton(ref depositUiElement, DepositUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>100,000</color>", 20, "0.52 0.01", "0.98 0.3", "rustybank.deposit 100000");


            //出金
            var withdrawUiElement = UI.CreateElementContainer(WithdrawUiName, UI.Color("#f5f5f5", (float)0.4), "0.525 0.10", "0.99 0.75");
            //UI.CreateLabel(ref mainUiElement, _MainUiName, "", "Withdraw", 15, "0.05 0.4", "0.95 0.5");
            UI.CreateLabel(ref withdrawUiElement, WithdrawUiName, "", "出金", 25, "0.05 0.85", "0.95 0.99");
            //Withdraw
            //UI.CreateButton(ref mainUiElement, _MainUiName, UI.Color("#0000FF", (float)0.5), "<color=#FFFFFF>50</color>", 18, "0.02 0.3", "0.124 0.4", "rustybank.withdraw 50");
            //UI.CreateButton(ref mainUiElement, _MainUiName, UI.Color("#0000FF", (float)0.5), "<color=#FFFFFF>100</color>", 18, "0.127 0.3", "0.249 0.4", "rustybank.withdraw 100");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>500</color>", 20, "0.02 0.63", "0.49 0.84", "rustybank.withdraw 500");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>1,000</color>", 20, "0.52 0.63", "0.98 0.84", "rustybank.withdraw 1000");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>5,000</color>", 20, "0.02 0.32", "0.49 0.61", "rustybank.withdraw 5000");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>10,000</color>", 20, "0.52 0.32", "0.98 0.61", "rustybank.withdraw 10000");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>50,000</color>", 20, "0.02 0.01", "0.49 0.3", "rustybank.withdraw 50000");
            UI.CreateButton(ref withdrawUiElement, WithdrawUiName, UI.Color("#000000", (float)0.75), "<color=#FFFFFF>100,000</color>", 20, "0.52 0.01", "0.98 0.3", "rustybank.withdraw 100000");

            //BankShop
            //UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#0000FF", (float)1.0), "<color=#FFFFFF>SHOP</color>", 18, "0.56 0.92", "0.75 0.99", "rustybank.createbankshop");

            //拡張
            if (Configs.IsExtension)
            {
                UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#0000FF", (float)1.0), "<color=#FFFFFF>拡張</color>", 18, "0.56 0.92", "0.75 0.99", "rustybank.createbankextensionmenu");
            }

            //Close
            UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.99", "rustybank.destroyui");

            //説明Label
            double fees = 0.0;
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
            else if (permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                if (Configs.Default.IsFee)
                {
                    fees = Configs.Default.Fee;
                }
            }
            UI.CreateLabel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.8), $"<color=#FFFFFF>取引手数料(Transaction Fees): {fees}</color>", 18, "0.1 0.03", "0.9 0.085");

            CuiHelper.AddUi(player, mainUiElement);
            CuiHelper.AddUi(player, nameLabelUiElement);
            CuiHelper.AddUi(player, depositUiElement);
            CuiHelper.AddUi(player, withdrawUiElement);
            AddUiPanel(player, MainUiName);
            AddUiPanel(player, NameLabelUiName);
            AddUiPanel(player, DepositUiName);
            AddUiPanel(player, WithdrawUiName);


            //SubUI
            UpdateSubUi(player);
        }

        private void UpdateSubUi(BasePlayer player)
        {
            if (IsUiPanelOpened(player, SubUiName))
            {
                DestroySubUi(player);
            }

            //SubUI
            var subUiElement = UI.CreateElementContainer(SubUiName, UI.Color("#000000", (float)0.0), "0.01 0.8", "0.99 0.9");
            UI.CreatePanel(ref subUiElement, SubUiName, UI.Color("#000000", (float)0.0), "0.1 0.01", "0.99 0.9", true);
            UI.CreateLabel(ref subUiElement, SubUiName, "", $"<=>", 30, "0.45 0.01", "0.55 0.9");

            //Label
            double _possession;
            if (GetPossession(player, out _possession))
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", $"Holdings:{_possession}", 30, "0.01 0.01", "0.50 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"手持ち:{_possession:#,0}", 30, "0.01 0.01", "0.50 0.9");
            }
            else
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", "Holdings:...", 30, "0.01 0.01", "0.50 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", "手持ち:...", 30, "0.01 0.01", "0.50 0.9");
            }

            double _balance;
            var maxDepositBalance = GetMaxDepositBalance(player);
            if (GetAccountBalance(player, out _balance))
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", $"Savings:{_balance}", 30, "0.51 0.01", "0.99 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance:#,0}/{maxDepositBalance:#,0}", 30, "0.51 0.01", "0.99 0.9");
            }
            else
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", "Savings:...", 30, "0.51 0.8", "0.99 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance:#,0}/{maxDepositBalance:#,0}", 30, "0.51 0.01", "0.99 0.9");
            }

            CuiHelper.AddUi(player, subUiElement);
            AddUiPanel(player, SubUiName);
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
                DestroyExtentionSubUi(player);
            }
            //SubUI
            var subUiElement = UI.CreateElementContainer(ExtensionSubUiName, UI.Color("#000000", (float)0.0), "0.01 0.55", "0.99 0.65");

            //Label
            double _balance;
            UI.CreateLabel(ref subUiElement, ExtensionSubUiName, "", $"現在預金枠: {maxDepositBalance:#,0}", 20, "0.25 0.01", "0.75 0.9");
            
            CuiHelper.AddUi(player, subUiElement);
            AddUiPanel(player, ExtensionSubUiName);
        }
        
        // private void CreateBankShopUi(BasePlayer player)
        // {
        //     //MainUI
        //     var mainUiElement = UI.CreateElementContainer(MainUiName, UI.Color("#000000", (float) 0.75), "0.0 0.0", "1 1");
        //     UI.CreatePanel(ref mainUiElement, MainUiName, UI.Color("#000000", (float)0.9), "0.01 0.01", "0.99 0.99", true);
        //
        //     //Label: Name
        //     var nameLabelUiElement = UI.CreateElementContainer(NameLabelUiName, UI.Color("#f5f5f5", (float)0.5), "0.01 0.90", "0.5 0.99");
        //     UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), $"{this.Title} Ver.{this.Version}", 30, "0 0", "1 1");
        //     
        //     //Close
        //     UI.CreateButton(ref mainUiElement, MainUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.99", "rustybank.destroyui");
        //     
        //     //商品
        //     var shopUiElement = UI.CreateElementContainer(ShopUiName, UI.Color("#f5f5f5", (float)0.4), "0.01 0.01", "0.99 0.85");
        //     
        //     UI.CreateButton(ref shopUiElement, ShopUiName, UI.Color("#FF0000", (float)1.0), "<color=#FFFFFF>Close</color>", 18, "0.8 0.92", "0.99 0.99", "");
        //
        //     CuiHelper.AddUi(player, mainUiElement);
        //     CuiHelper.AddUi(player, nameLabelUiElement);
        //     CuiHelper.AddUi(player, shopUiElement);
        //     AddUiPanel(player, MainUiName);
        //     AddUiPanel(player, NameLabelUiName);
        //     AddUiPanel(player, ShopUiName);
        // }
        
        //TODO: 販売UI
        // private void CreateShopItemContainer(BasePlayer player, int num)
        // {
        //     int num1 = num + 1;
        //     Vector2 posMin = CalcDRPos(num);
        //     Vector2 dimemsions = new Vector2(0.115f, 0.125f);
        //     Vector2 posMax = posMin + dimemsions;
        //
        //     string panelName = $"Entry{num}";
        //
        //     var dbEntry = UI.CreateElementContainer(panelName, "0 0 0 0", $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}");
        //     UI.CreatePanel(ref dbEntry, panelName, UI.Color("#F0F0F0", (float)0.5), $"0 0", $"1 1");
        //
        //     string bCommand = "";
        //     string bText = "";
        //     string bColor = "";
        //
        //     PlayerData pData;
        //     DateTime lTime;
        //
        //     TimeSpan interval;
        //
        //     DRP.TryGetValue(player.UserIDString, out pData);
        //     lTime = pData.LastTime;
        //     interval = now - lTime;
        //
        //
        //     if (num1 < DCounter)
        //     {
        //         bText = "Expired";
        //         bColor = UI.Color("#9F9F9F", (float)0.5);
        //     }
        //     else if (DCounter < num1)
        //     {
        //         bText = "X";
        //         bColor = UI.Color("#FF0000", (float)0.5);
        //     }
        //     else
        //     {
        //         if (interval.Days < 1 && DCounter != 1)
        //         {
        //             bText = "X";
        //             bColor = UI.Color("#FF0000", (float)0.5);
        //         }
        //         else
        //         {
        //             bText = "Get";
        //             bColor = UI.Color("#00FF00", (float)0.5);
        //             bCommand = $"GetReward {num}";
        //         }
        //     }
        //
        //     UI.CreateButton(ref dbEntry, panelName, bColor, bText, 14, $"0.72 0.80", $"0.98 0.97", bCommand);
        //
        //     string rewards = bShortname[num];
        //     int amount = bAmount[num];
        //
        //     string info = $"Rewards: {rewards}\nAmount: {amount}";
        //
        //     UI.CreateLabel(ref dbEntry, panelName, "", $"Day{num1}", 16, $"0.02 0.8", "0.72 1.0", TextAnchor.MiddleLeft);
        //     UI.CreateLabel(ref dbEntry, panelName, bColor, info, 14, $"0.02 0.01", "0.98 0.78", TextAnchor.UpperLeft);
        //
        //     CuiHelper.AddUi(player, dbEntry);
        // }
        //
        // private Vector2 CalcDRPos(int num)
        // {
        //     Vector2 position = new Vector2(0.1325f, 0.775f);
        //     Vector2 dimensions = new Vector2(0.115f, 0.125f);
        //     float offsetY = 0f;
        //     float offsetX = 0;
        //     if (0 <= num && num < 7)
        //     {
        //         offsetX = (0.005f + dimensions.x) * num;
        //     }
        //     if (6 < num && num < 14)
        //     {
        //         offsetX = (0.005f + dimensions.x) * (num - 7);
        //         offsetY = (-0.008f - dimensions.y) * 1;
        //     }
        //     if (13 < num && num < 21)
        //     {
        //         offsetX = (0.005f + dimensions.x) * (num - 14);
        //         offsetY = (-0.008f - dimensions.y) * 2;
        //     }
        //     if (20 < num && num < 28)
        //     {
        //         offsetX = (0.005f + dimensions.x) * (num - 21);
        //         offsetY = (-0.008f - dimensions.y) * 3;
        //     }
        //     if (27 < num && num < 31)
        //     {
        //         offsetX = (0.005f + dimensions.x) * (num - 28);
        //         offsetY = (-0.008f - dimensions.y) * 4;
        //     }
        //     return new Vector2(position.x + offsetX, position.y + offsetY);
        // }

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
        #endregion

        #region FindPlayer
        private ulong FindPlayersSingleId(string nameORidORip, BasePlayer player)
        {
            var targets = FindPlayers(nameORidORip);
            if (targets.Count > 1)
            {
                SendMessage(player, lang.GetMessage("MultiplePlayers", this) + string.Join(",", targets.Select(p => p.displayName).ToArray()));
                return 0;
            }
            ulong userId;
            if (targets.Count <= 0)
            {
                SendMessage(player, lang.GetMessage("PlayerNotFound", this));
                return 0;
            }
            else userId = targets.First().userID;
            return userId;
        }
        private BasePlayer FindPlayerSingle(string nameORidORip, BasePlayer player)
        {
            var targets = FindPlayers(nameORidORip);
            if (targets.Count <= 0)
            {
                SendMessage(player, lang.GetMessage("PlayerNotFound", this));
                return null;
            }
            if (targets.Count>1)
            {
                SendMessage(player, lang.GetMessage("MultiplePlayers", this) + string.Join(",", targets.Select(p => p.displayName).ToArray()));
                return null;
            }
            return targets.First();
        }
        private static HashSet<BasePlayer> FindPlayers(string nameORidORip) 
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameORidORip)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameORidORip))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameORidORip, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameORidORip))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameORidORip))
                    players.Add(sleepingPlayer);
                else if (!string.IsNullOrEmpty(sleepingPlayer.displayName) && sleepingPlayer.displayName.Contains(nameORidORip, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }
        #endregion
        
        #region Helper
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
