namespace gitAttack.Models;

/// <summary>
/// Unity场景冲突信息，包含文件路径、原始内容和冲突区块列表。
/// </summary>
public class UnitySceneConflict
{
    /// <summary>
    /// 场景文件的完整路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// 场景文件名（不含路径）
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);
    /// <summary>
    /// 场景文件的原始内容
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;
    /// <summary>
    /// 冲突区块列表
    /// </summary>
    public List<ConflictSection> ConflictSections { get; set; } = new List<ConflictSection>();
    /// <summary>
    /// 是否所有冲突区块都已解决
    /// </summary>
    public bool IsResolved => ConflictSections.All(c => c.IsResolved);
}

/// <summary>
/// 单个冲突区块，包含起止行号、双方内容、AI建议等。
/// </summary>
public class ConflictSection
{
    /// <summary>
    /// 冲突区块起始行号
    /// </summary>
    public int StartLineNumber { get; set; }
    /// <summary>
    /// 冲突区块结束行号
    /// </summary>
    public int EndLineNumber { get; set; }
    /// <summary>
    /// 我方（HEAD）内容
    /// </summary>
    public string OurContent { get; set; } = string.Empty; // HEAD内容
    /// <summary>
    /// 对方（分支）内容
    /// </summary>
    public string TheirContent { get; set; } = string.Empty; // 其他分支内容
    /// <summary>
    /// 已解决的内容（如有）
    /// </summary>
    public string ResolvedContent { get; set; } = string.Empty;
    /// <summary>
    /// 当前区块是否已解决
    /// </summary>
    public bool IsResolved => !string.IsNullOrEmpty(ResolvedContent);
    /// <summary>
    /// AI生成的冲突描述
    /// </summary>
    public string Description { get; set; } = string.Empty; // AI生成的描述
    /// <summary>
    /// AI生成的合并建议
    /// </summary>
    public string SuggestionFromAI { get; set; } = string.Empty; // AI建议
}