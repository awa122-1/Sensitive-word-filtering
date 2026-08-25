using BepInEx.Configuration;

namespace AmongUsFilterMod.Config
{
    public static class ConfigManager
    {
        // 模组指针总开关
        public static ConfigEntry<bool> UseModCursor { get; set; }

        public static void Init(ConfigFile config)
        {
            UseModCursor = config.Bind(
                "Customizations",
                "UseModCursor",
                true,
                "是否使用模组自定义鼠标指针"
            );
        }
    }
}