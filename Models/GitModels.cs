// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// Git相关数据模型
using System;
using System.Collections.Generic;

namespace gitAttack.Models
{
    // 提交信息
    public class CommitInfo
    {
        public string Sha { get; set; } = string.Empty;
        public string ShortSha { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int ParentCount { get; set; }
        public bool IsMergeCommit => ParentCount > 1;
    }

    // 分支信息
    public class BranchInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsRemote { get; set; }
        public bool IsHead { get; set; }
        public string UpstreamBranchName { get; set; } = string.Empty;
        public string Tip { get; set; } = string.Empty;
    }

    // 远程仓库信息
    public class RemoteInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string PushUrl { get; set; } = string.Empty;
    }

    // 文件状态信息
    public class FileStatus
    {
        public string FilePath { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    // 仓库状态信息
    public class RepositoryStatus
    {
        public string CurrentBranchName { get; set; } = string.Empty;
        public int Added { get; set; }
        public int Modified { get; set; }
        public int Removed { get; set; }
        public int Untracked { get; set; }
        public int Staged { get; set; }
        public int Missing { get; set; }
        public int Conflicted { get; set; }
        public List<FileStatus> Files { get; set; } = new List<FileStatus>();
    }

    // 合并结果
    public class MergeResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Commit { get; set; } = string.Empty;
        public bool HasConflicts { get; set; }
        public bool WasUpToDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}