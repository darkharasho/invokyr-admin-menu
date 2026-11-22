using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace InvokyrAdminMenu
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.darkharasho.invokyr.adminmenu";
        public const string PluginName = "InvokyrAdminMenu";
        public const string PluginVersion = "0.1.0";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<string> ToggleKey;
        internal static Shortcut ToggleShortcut => Shortcut.Parse(ToggleKey.Value);
        internal static ConfigEntry<bool> CloseOnEscape;
        internal static ConfigEntry<bool> BlockNewPlayers;
        internal static ConfigEntry<string> SpendingList;
        internal static ConfigEntry<string> BlockedMessage;
        internal static SpendingRules Rules;

        public override void Load()
        {
            Log = base.Log;

            ToggleKey = Config.Bind("General", "ToggleKey", "F1", new ConfigDescription(
                "Opens and closes the admin menu (a Unity KeyCode, optionally with modifiers: F1 or F1 + LeftControl). Only the host can open it.",
                null, new ConfigurationManagerAttributes { DispName = "Toggle key", Order = 3 }));
            CloseOnEscape = Config.Bind("General", "CloseOnEscape", true, new ConfigDescription(
                "Esc closes the admin menu.",
                null, new ConfigurationManagerAttributes { DispName = "Close on Esc", Order = 2 }));
            BlockNewPlayers = Config.Bind("Spending", "BlockNewPlayers", false, new ConfigDescription(
                "Players joining your games for the first time can't spend money until you allow them in the admin menu.",
                null, new ConfigurationManagerAttributes { DispName = "Block new players", Order = 1 }));
            BlockedMessage = Config.Bind("Spending", "BlockedMessage", "The host has turned off spending for you", new ConfigDescription(
                "What a blocked player sees when they try to buy something.",
                null, new ConfigurationManagerAttributes { DispName = "Blocked message", Order = 0 }));
            SpendingList = Config.Bind("Spending", "Players", "", new ConfigDescription(
                "Who may spend (SteamID64:allow or SteamID64:block, comma separated). Set from the admin menu.",
                null, new ConfigurationManagerAttributes { DispName = "Players", Order = -1 }));

            Rules = SpendingRules.Parse(SpendingList.Value, BlockNewPlayers.Value);
            BlockNewPlayers.SettingChanged += OnBlockNewPlayersChanged;

            SpendingLock.Apply(new Harmony(PluginGuid));
            AddComponent<AdminMenuController>();

            Log.LogInfo($"{PluginName} {PluginVersion} loaded; press {ToggleShortcut} while hosting to open the admin menu");
        }

        private static void OnBlockNewPlayersChanged(object sender, System.EventArgs args) => Rules.BlockNewPlayers = BlockNewPlayers.Value;

        internal static void SaveRules()
        {
            Rules.BlockNewPlayers = BlockNewPlayers.Value;
            SpendingList.Value = Rules.Serialize();
        }
    }
}
