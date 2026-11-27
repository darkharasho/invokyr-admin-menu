// Fields are read by name by settings menus; not every one is set here.
#pragma warning disable CS0649
namespace InvokyrAdminMenu
{
    /// <summary>
    /// Tag for a setting's <see cref="BepInEx.Configuration.ConfigDescription"/> that tells a settings menu
    /// (such as BepInEx.ConfigurationManager) how to show it. Read by
    /// field name, so no reference to that mod is needed and the mod works the same without it.
    /// </summary>
    internal sealed class ConfigurationManagerAttributes
    {
        public string DispName;
        public int? Order;
        /// <summary>False hides the setting from settings menus (InvokyrConfig, ConfigurationManager).</summary>
        public bool? Browsable;
        public bool? ReadOnly;
        public bool? IsAdvanced;
    }
}
