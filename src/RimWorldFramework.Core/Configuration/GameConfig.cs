using System.Collections.Generic;

namespace RimWorldFramework.Core.Configuration
{
    /// <summary>
    /// 游戏配置
    /// </summary>
    public class GameConfig
    {
        public GraphicsConfig Graphics { get; set; } = new();
        public AudioConfig Audio { get; set; } = new();
        public GameplayConfig Gameplay { get; set; } = new();
        public ModConfig Mods { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
    }

    /// <summary>
    /// 图形配置
    /// </summary>
    public class GraphicsConfig
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public bool Fullscreen { get; set; } = false;
        public int TargetFrameRate { get; set; } = 60;
        public string Quality { get; set; } = "Medium";
    }

    /// <summary>
    /// 音频配置
    /// </summary>
    public class AudioConfig
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 1.0f;
        public bool Muted { get; set; } = false;
    }

    /// <summary>
    /// 游戏玩法配置
    /// </summary>
    public class GameplayConfig
    {
        public string Difficulty { get; set; } = "Normal";
        public bool AutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 300; // 秒
        public bool PauseOnFocusLost { get; set; } = true;
    }

    /// <summary>
    /// 模组配置
    /// </summary>
    public class ModConfig
    {
        public bool EnableMods { get; set; } = true;
        public List<string> EnabledMods { get; set; } = new();
        public string ModsDirectory { get; set; } = "Mods";
        public bool AllowUnsafeMods { get; set; } = false;
    }

    /// <summary>
    /// 日志配置
    /// </summary>
    public class LoggingConfig
    {
        public string LogLevel { get; set; } = "Information";
        public bool LogToFile { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        public int MaxLogFiles { get; set; } = 10;
        public long MaxLogFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
    }
}