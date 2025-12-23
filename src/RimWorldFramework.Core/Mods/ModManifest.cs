using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组清单文件
    /// </summary>
    public class ModManifest
    {
        /// <summary>
        /// 模组ID（唯一标识符）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// 模组名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// 模组版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// 模组描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// 模组作者
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; }

        /// <summary>
        /// 模组主页URL
        /// </summary>
        [JsonPropertyName("homepage")]
        public string Homepage { get; set; }

        /// <summary>
        /// 支持的游戏版本
        /// </summary>
        [JsonPropertyName("supportedGameVersions")]
        public List<string> SupportedGameVersions { get; set; } = new List<string>();

        /// <summary>
        /// 模组依赖项
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<ModDependencyInfo> Dependencies { get; set; } = new List<ModDependencyInfo>();

        /// <summary>
        /// 模组入口点
        /// </summary>
        [JsonPropertyName("entryPoints")]
        public List<ModEntryPoint> EntryPoints { get; set; } = new List<ModEntryPoint>();

        /// <summary>
        /// 模组资源
        /// </summary>
        [JsonPropertyName("resources")]
        public ModResources Resources { get; set; } = new ModResources();

        /// <summary>
        /// 模组权限要求
        /// </summary>
        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// 模组标签
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 模组配置
        /// </summary>
        [JsonPropertyName("configuration")]
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 清单版本
        /// </summary>
        [JsonPropertyName("manifestVersion")]
        public int ManifestVersion { get; set; } = 1;
    }

    /// <summary>
    /// 模组依赖信息
    /// </summary>
    public class ModDependencyInfo
    {
        /// <summary>
        /// 依赖的模组ID
        /// </summary>
        [JsonPropertyName("modId")]
        public string ModId { get; set; }

        /// <summary>
        /// 最小版本
        /// </summary>
        [JsonPropertyName("minVersion")]
        public string MinVersion { get; set; }

        /// <summary>
        /// 最大版本
        /// </summary>
        [JsonPropertyName("maxVersion")]
        public string MaxVersion { get; set; }

        /// <summary>
        /// 是否为可选依赖
        /// </summary>
        [JsonPropertyName("optional")]
        public bool Optional { get; set; } = false;
    }

    /// <summary>
    /// 模组入口点
    /// </summary>
    public class ModEntryPoint
    {
        /// <summary>
        /// 入口点类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// 程序集路径
        /// </summary>
        [JsonPropertyName("assembly")]
        public string Assembly { get; set; }

        /// <summary>
        /// 类名
        /// </summary>
        [JsonPropertyName("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// 方法名
        /// </summary>
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        /// <summary>
        /// 加载顺序
        /// </summary>
        [JsonPropertyName("loadOrder")]
        public int LoadOrder { get; set; } = 0;
    }

    /// <summary>
    /// 模组资源
    /// </summary>
    public class ModResources
    {
        /// <summary>
        /// 纹理资源路径
        /// </summary>
        [JsonPropertyName("textures")]
        public List<string> Textures { get; set; } = new List<string>();

        /// <summary>
        /// 音频资源路径
        /// </summary>
        [JsonPropertyName("audio")]
        public List<string> Audio { get; set; } = new List<string>();

        /// <summary>
        /// 数据文件路径
        /// </summary>
        [JsonPropertyName("data")]
        public List<string> Data { get; set; } = new List<string>();

        /// <summary>
        /// 脚本文件路径
        /// </summary>
        [JsonPropertyName("scripts")]
        public List<string> Scripts { get; set; } = new List<string>();

        /// <summary>
        /// 本地化文件路径
        /// </summary>
        [JsonPropertyName("localization")]
        public Dictionary<string, string> Localization { get; set; } = new Dictionary<string, string>();
    }
}