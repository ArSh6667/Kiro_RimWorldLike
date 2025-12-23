using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组冲突检测器接口
    /// </summary>
    public interface IModConflictDetector
    {
        /// <summary>
        /// 检测模组冲突
        /// </summary>
        /// <param name="mods">要检测的模组列表</param>
        /// <returns>冲突检测结果</returns>
        Task<ModConflictDetectionResult> DetectConflictsAsync(IEnumerable<IMod> mods);

        /// <summary>
        /// 检测特定类型的冲突
        /// </summary>
        /// <param name="mods">要检测的模组列表</param>
        /// <param name="conflictType">冲突类型</param>
        /// <returns>检测到的冲突列表</returns>
        Task<IEnumerable<ModConflict>> DetectSpecificConflictsAsync(IEnumerable<IMod> mods, ConflictType conflictType);

        /// <summary>
        /// 生成冲突解决建议
        /// </summary>
        /// <param name="conflict">冲突</param>
        /// <returns>解决建议列表</returns>
        Task<IEnumerable<ConflictResolution>> GenerateResolutionSuggestionsAsync(ModConflict conflict);
    }
}