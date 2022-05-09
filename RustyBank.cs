using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    [Info("RustyBank", "babu77", "0.2.0")]
    [Description("...")]
    public class RustyBank : RustPlugin
    {
        #region Definitions(Global)
        private const string PName = "<color=yellow>[RustyBank]:</color>";
        private const string PermDefault = "rustybank.default";
        private const string PermVip1 = "rustybank.vip1";
        private const string PermAdmin = "rustybank.admin";
        private const string MainUiName = "RustyBankUIMain";
        private const string SubUiName = "RustyBankUISub";
        private const string FadeUiName = "RustyBankUIFade";
        private const string NameLabelUiName = "RustyBankUINameLabel";
        private const string DepositUiName = "RustyBankUIDeposit";
        private const string WithdrawUiName = "RustyBankUIWithdraw";
        private Configurations Configs = new Configurations();
        private DynamicConfigFile _rbDataFile;
        private Dictionary<ulong, PlayerData> _dataPlayerRb;
        private DynamicConfigFile _rbNpcDataFile;
        private List<ulong> _dataNpc;
        private Dictionary<ulong, List<string>> OpenUiPanel = new Dictionary<ulong, List<string>>();
        private List<ulong> _mangedList = new List<ulong>();


        [PluginReference] 
        private Plugin Economics;
        #endregion

        #region Classes

        private class PlayerData
        {
            public double Balance { get; set; }
        }

        private class Configurations
        {
            //public bool UseEconomics { get; set; }
            public DbConnection MySqlConnection { get; set; }
            public UserConfigs Default { get; set; }
            public UserConfigs Vip1 { get; set; }
            public UserConfigs Admin { get; set; }

            public Configurations()
            {
                this.MySqlConnection = new DbConnection();
                this.Default = new UserConfigs();
                this.Admin = new UserConfigs();
                this.Vip1 = new UserConfigs();
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

            //MysqlConnection
            // Configs.MySqlConnection.Address = Convert.ToString(GetConfig("01_MySql", "Address", "localhost"));
            // Configs.MySqlConnection.Port = Convert.ToInt32(GetConfig("01_MySql", "Port", 3306));
            // Configs.MySqlConnection.DbName = Convert.ToString(GetConfig("01_MySql", "DataBase", "DB_RustyBank"));
            // Configs.MySqlConnection.UserName = Convert.ToString(GetConfig("01_MySql", "UserName", "RustyBank"));
            // Configs.MySqlConnection.Password = Convert.ToString(GetConfig("01_MySql", "Password", "password"));

            //Admin
            Configs.Admin.MaxDepositBalance = Convert.ToDouble(GetConfig("10_Admin", "Max_Deposit_Balance", 999999));
            Configs.Admin.IsFee = Convert.ToBoolean(GetConfig("10_Admin", "Use_Fee", false));
            //Configs.Admin.FeeType = Convert.ToInt32(GetConfig("10_Admin", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Admin.Fee = Convert.ToDouble(GetConfig("10_Admin", "Fee", 250));
            //Configs.Admin.FeeInterval = Convert.ToSingle(GetConfig("10_Admin", "Fee_Interval", 3600));

            //Default
            Configs.Default.MaxDepositBalance = Convert.ToDouble(GetConfig("11_Default", "Max_Deposit_Balance", 100000));
            Configs.Default.IsFee = Convert.ToBoolean(GetConfig("11_Default", "Use_Fee", false));
            //Configs.Default.FeeType = Convert.ToInt32(GetConfig("11_Default", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Default.Fee = Convert.ToDouble(GetConfig("11_Default", "Fee", 250));
            //Configs.Default.FeeInterval = Convert.ToSingle(GetConfig("11_Default", "Fee_Interval", 3600));

            //Vip1
            Configs.Vip1.MaxDepositBalance = Convert.ToDouble(GetConfig("12_Vip1", "Max_Deposit_Balance", 200000));
            Configs.Vip1.IsFee = Convert.ToBoolean(GetConfig("12_Vip1", "Use_Fee", false));
            //Configs.Vip1.FeeType = Convert.ToInt32(GetConfig("12_Vip1", "Fee_Type(0: Amount, 1: Percentage(0-100))", 0));
            Configs.Vip1.Fee = Convert.ToDouble(GetConfig("12_Vip1", "Fee", 250));
            //Configs.Vip1.FeeInterval = Convert.ToSingle(GetConfig("12_Vip1", "Fee_Interval", 3600));
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
                {"NotExistsAccount", "Your don't have account."}
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

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (_mangedList.Contains(player.userID))
            {
                _dataNpc.Add(npc.userID);
                
                if (_mangedList.Contains(player.userID))
                {
                    _mangedList.Remove(player.userID);
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
                    CommandBank(player, "", null);
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

        private bool GetAccountBalance(BasePlayer player, out double balance)
        {
            balance = 0.0;
            try
            {
                PlayerData playerData;

                if (!_dataPlayerRb.TryGetValue(player.userID, out playerData))
                {
                    playerData = new PlayerData();
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
            else if (permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                if (Configs.Default.IsFee)
                {
                    fees = Configs.Default.Fee;
                }
            }

            return fees;
        }

        private bool ExistsAccount(BasePlayer player)
        {
            PlayerData playerData;
            return _dataPlayerRb.TryGetValue(player.userID, out playerData);
        }

        private void FuncDestroyUi(BasePlayer player)
        {
            DestroySubUi(player);
            CuiHelper.DestroyUi(player, MainUiName);
            CuiHelper.DestroyUi(player, NameLabelUiName);
            CuiHelper.DestroyUi(player, DepositUiName);
            CuiHelper.DestroyUi(player, WithdrawUiName);
            RemoveUiPanel(player, MainUiName);
            RemoveUiPanel(player, NameLabelUiName);
            RemoveUiPanel(player, DepositUiName);
            RemoveUiPanel(player, WithdrawUiName);
            //CuiHelper.DestroyUi(player, "db");
            //DestroyEntries(player);
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
                if (_mangedList.Contains(player.userID))
                {
                    return;
                }
                else
                {
                    _mangedList.Add(player.userID);
                }
            }
            else if(args[0].ToLower().Equals("remove"))
            {
                //
            }
            
        }

        [ChatCommand("bank")]
        private void CommandBank(BasePlayer player, string command, string[] args)
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
                CreateRustyBankMenu(player);
            }
            else if(permission.UserHasPermission(player.UserIDString, PermDefault))
            {
                CreateRustyBankMenu(player);
            }
            else
            {
                SendMessage(player, lang.GetMessage("NoPerm", this));
            }

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
                    playerData = new PlayerData();
                }
                else
                {
                    bool isCalledEconomicsApi = false;
                    bool isRustyBankBalance = false;
                    if (permission.UserHasPermission(player.UserIDString, PermAdmin))
                    {
                        if (Configs.Admin.MaxDepositBalance < playerData.Balance + depositAmount)
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
                    }
                    else if (permission.UserHasPermission(player.UserIDString, PermVip1))
                    {
                        if (Configs.Vip1.MaxDepositBalance < playerData.Balance + depositAmount)
                        {
                            CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                            return;
                        }
                        try
                        {
                            var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
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
                        catch (Exception)
                        {
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
                    else if (permission.UserHasPermission(player.UserIDString, PermDefault))
                    {
                        if (Configs.Default.MaxDepositBalance < playerData.Balance + depositAmount)
                        {
                            CreateFadeUI(player, lang.GetMessage("BalanceExceedsLimit", this));
                            return;
                        }
                        try
                        {
                            var apiResult = Economics?.Call("Withdraw", player.userID, depositAmount);
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
                        catch (Exception)
                        {
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
                    else
                    {
                        CreateFadeUI(player, lang.GetMessage("NoPerm", this));
                        return;
                    }
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

                if (!permission.UserHasPermission(player.UserIDString, PermAdmin) &&
                    !permission.UserHasPermission(player.UserIDString, PermVip1) &&
                    !permission.UserHasPermission(player.UserIDString, PermDefault))
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

            double fees = GetFees(player);

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

        private void DestroyFadeUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FadeUiName);
            RemoveUiPanel(player, FadeUiName);
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
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), "Rusty Bank Ver.β", 30, "0 0", "1 1");

            //説明
            double initialDeposit = 500;
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
            UI.CreateLabel(ref nameLabelUiElement, NameLabelUiName, UI.Color("#FFA500", (float)1.0), $"Rusty Bank Ver.{this.Version}", 30, "0 0", "1 1");

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
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"手持ち:{_possession}", 30, "0.01 0.01", "0.50 0.9");
            }
            else
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", "Holdings:...", 30, "0.01 0.01", "0.50 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", "手持ち:...", 30, "0.01 0.01", "0.50 0.9");
            }

            double _balance;
            if (GetAccountBalance(player, out _balance))
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", $"Savings:{_balance}", 30, "0.51 0.01", "0.99 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance}", 30, "0.51 0.01", "0.99 0.9");
            }
            else
            {
                //UI.CreateLabel(ref subUiElement, _SubUiName, "", "Savings:...", 30, "0.51 0.8", "0.99 0.9");
                UI.CreateLabel(ref subUiElement, SubUiName, "", $"預金:{_balance}", 30, "0.51 0.01", "0.99 0.9");
            }

            CuiHelper.AddUi(player, subUiElement);
            AddUiPanel(player, SubUiName);
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
        #endregion

        //#region FindPlayer
        //private ulong FindPlayersSingleId(string nameORidORip, BasePlayer player)
        //{
        //    var targets = FindPlayers(nameORidORip);
        //    if (targets.Count > 1)
        //    {
        //        SendMessage(player, lang.GetMessage("MultiplePlayers", this) + string.Join(",", targets.Select(p => p.displayName).ToArray()));
        //        return 0;
        //    }
        //    ulong userId;
        //    if (targets.Count <= 0)
        //    {
        //        SendMessage(player, lang.GetMessage("PlayerNotFound", this));
        //        return 0;
        //    }
        //    else userId = targets.First().userID;
        //    return userId;
        //}
        //private BasePlayer FindPlayerSingle(string nameORidORip, BasePlayer player)
        //{
        //    var targets = FindPlayers(nameORidORip);
        //    if (targets.Count <= 0)
        //    {
        //        SendMessage(player, lang.GetMessage("PlayerNotFound", this));
        //        return null;
        //    }
        //    if (targets.Count > 1)
        //    {
        //        SendMessage(player, lang.GetMessage("MultiplePlayers", this) + string.Join(",", targets.Select(p => p.displayName).ToArray()));
        //        return null;
        //    }
        //    return targets.First();
        //}
        //private static HashSet<BasePlayer> FindPlayers(string nameORidORip)
        //{
        //    var players = new HashSet<BasePlayer>();
        //    if (string.IsNullOrEmpty(nameORidORip)) return players;
        //    foreach (var activePlayer in BasePlayer.activePlayerList)
        //    {
        //        if (activePlayer.UserIDString.Equals(nameORidORip))
        //            players.Add(activePlayer);
        //        else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameORidORip, CompareOptions.IgnoreCase))
        //            players.Add(activePlayer);
        //        else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameORidORip))
        //            players.Add(activePlayer);
        //    }
        //    foreach (var sleeptingPlayer in BasePlayer.sleepingPlayerList)
        //    {
        //        if (sleeptingPlayer.UserIDString.Equals(nameORidORip))
        //            players.Add(sleeptingPlayer);
        //        else if (!string.IsNullOrEmpty(sleeptingPlayer.displayName) && sleeptingPlayer.displayName.Contains(nameORidORip, CompareOptions.IgnoreCase))
        //            players.Add(sleeptingPlayer);
        //    }
        //    return players;
        //}
        //#endregion

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
