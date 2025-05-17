// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// Git服务逻辑
using System.IO;
using System.Text.RegularExpressions;
using gitAttack.Models;
using LibGit2Sharp;
// 添加别名解决命名冲突
using LibFileStatus = LibGit2Sharp.FileStatus;
using LibMergeResult = LibGit2Sharp.MergeResult;
using LibRepositoryStatus = LibGit2Sharp.RepositoryStatus;
using ModelFileStatus = gitAttack.Models.FileStatus;
using ModelMergeResult = gitAttack.Models.MergeResult;
using ModelRepositoryStatus = gitAttack.Models.RepositoryStatus;

namespace gitAttack.Services;

public class GitService
{
    private string _repoPath = string.Empty;

    public string GetRepositoryName()
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return string.Empty;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            // 获取仓库根目录的文件夹名
            var directoryInfo = new DirectoryInfo(repo.Info.WorkingDirectory);
            return directoryInfo.Name;
        }
        catch
        {
            return Path.GetFileName(_repoPath);
        }
    }

    public bool OpenRepository(string path)
    {
        try
        {
            // 尝试找到Git仓库根目录
            var repoPath = Repository.Discover(path);
            if (string.IsNullOrEmpty(repoPath))
            {
                return false;
            }

            _repoPath = repoPath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 初始化一个新的Git仓库
    public bool InitializeRepository(string path)
    {
        try
        {
            // 检查目录是否存在，不存在则创建
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // 初始化仓库
            Repository.Init(path);

            // 打开初始化的仓库
            return OpenRepository(path);
        }
        catch
        {
            return false;
        }
    }

    // 检查目录是否为Git仓库
    public bool IsGitRepository(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        var repoPath = Repository.Discover(path);
        return !string.IsNullOrEmpty(repoPath);
    }

    public List<string> GetConflictedUnitySceneFiles()
    {
        var conflictedFiles = new List<string>();

        if (string.IsNullOrEmpty(_repoPath))
        {
            return conflictedFiles;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 获取有冲突的文件
            var status = repo.RetrieveStatus();

            // 调试信息
            System.Diagnostics.Debug.WriteLine($"仓库状态: {status.IsDirty} (Dirty), 文件总数: {status.Count()}");

            // 检查所有冲突的文件
            var conflictEntries = status.Where(s => s.State.HasFlag(LibFileStatus.Conflicted)).ToList();
            System.Diagnostics.Debug.WriteLine($"找到 {conflictEntries.Count} 个冲突文件");

            foreach (var entry in conflictEntries)
            {
                var filePath = entry.FilePath;
                System.Diagnostics.Debug.WriteLine($"冲突文件: {filePath}, 状态: {entry.State}");

                // 检查是否为Unity场景文件（.unity扩展名）
                if (filePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    var fullPath = Path.Combine(repo.Info.WorkingDirectory, filePath);
                    conflictedFiles.Add(fullPath);
                    System.Diagnostics.Debug.WriteLine($"添加Unity场景冲突文件: {fullPath}");
                }
            }

            // 如果没有找到冲突的Unity场景文件，扫描文件系统查找可能的合并冲突标记
            if (conflictedFiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("通过Git API未找到冲突文件，尝试直接扫描文件内容...");
                var workingDir = repo.Info.WorkingDirectory;
                var unityFiles = Directory.GetFiles(workingDir, "*.unity", SearchOption.AllDirectories);
                System.Diagnostics.Debug.WriteLine($"找到 {unityFiles.Length} 个Unity场景文件");

                // 搜索文件中的合并冲突标记
                foreach (var unityFile in unityFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(unityFile);
                        if (content.Contains("<<<<<<<") && content.Contains("=======") && content.Contains(">>>>>>>"))
                        {
                            conflictedFiles.Add(unityFile);
                            System.Diagnostics.Debug.WriteLine($"通过内容扫描发现冲突: {unityFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"读取文件时出错: {unityFile}, {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取冲突文件时发生错误: {ex.Message}");
        }

        return conflictedFiles;
    }

    public UnitySceneConflict ParseConflictFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("冲突文件不存在", filePath);
        }

        var content = File.ReadAllText(filePath);
        var lines = File.ReadAllLines(filePath);

        var conflict = new UnitySceneConflict
        {
            FilePath = filePath,
            OriginalContent = content
        };

        // 使用正则表达式查找冲突的部分
        var conflictPattern = @"<<<<<<< .*?\r?\n(.*?)\r?\n=======\r?\n(.*?)\r?\n>>>>>>> .*?";
        var matches = Regex.Matches(content, conflictPattern, RegexOptions.Singleline);

        int currentLine = 0;

        foreach (Match match in matches)
        {
            // 计算冲突开始的行号
            var conflictStartPos = match.Index;
            var startLineNumber = content.Substring(0, conflictStartPos).Count(c => c == '\n');

            // 计算冲突结束的行号
            var conflictEndPos = match.Index + match.Length;
            var endLineNumber = content.Substring(0, conflictEndPos).Count(c => c == '\n');

            var ourContent = match.Groups[1].Value;
            var theirContent = match.Groups[2].Value;

            conflict.ConflictSections.Add(new ConflictSection
            {
                StartLineNumber = startLineNumber,
                EndLineNumber = endLineNumber,
                OurContent = ourContent,
                TheirContent = theirContent
            });

            currentLine = endLineNumber + 1;
        }

        return conflict;
    }

    public void SaveResolvedFile(UnitySceneConflict conflict)
    {
        if (!conflict.IsResolved)
        {
            throw new InvalidOperationException("存在未解决的冲突");
        }

        var content = conflict.OriginalContent;

        // 从最后一个冲突开始替换，这样不会影响前面冲突的索引位置
        for (int i = conflict.ConflictSections.Count - 1; i >= 0; i--)
        {
            var section = conflict.ConflictSections[i];

            // 找出冲突部分的原始内容
            var conflictStart = GetPositionFromLineNumber(content, section.StartLineNumber);
            var conflictEnd = GetPositionFromLineNumber(content, section.EndLineNumber) +
                              GetLineLength(content, section.EndLineNumber);

            // 替换冲突内容为解决后的内容
            content = content.Substring(0, conflictStart) +
                     section.ResolvedContent +
                     content.Substring(conflictEnd);
        }

        // 保存文件
        File.WriteAllText(conflict.FilePath, content);
    }

    private int GetPositionFromLineNumber(string content, int lineNumber)
    {
        int position = 0;
        int currentLine = 0;

        while (currentLine < lineNumber && position < content.Length)
        {
            if (content[position] == '\n')
            {
                currentLine++;
            }
            position++;
        }

        return position;
    }

    private int GetLineLength(string content, int lineNumber)
    {
        var startPosition = GetPositionFromLineNumber(content, lineNumber);
        var endPosition = startPosition;

        while (endPosition < content.Length && content[endPosition] != '\n')
        {
            endPosition++;
        }

        return endPosition - startPosition;
    }

    // 获取Git提交历史
    public List<CommitInfo> GetCommitHistory(int maxCount = 100)
    {
        var commits = new List<CommitInfo>();

        if (string.IsNullOrEmpty(_repoPath))
        {
            return commits;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 计算提交数量
            var count = 0;
            foreach (var commit in repo.Commits)
            {
                if (count >= maxCount) break;

                commits.Add(new CommitInfo
                {
                    Sha = commit.Sha,
                    ShortSha = commit.Sha.Substring(0, 7),
                    Message = commit.MessageShort,
                    Author = commit.Author.Name,
                    Email = commit.Author.Email,
                    Date = commit.Author.When.DateTime,
                    ParentCount = commit.Parents.Count()
                });

                count++;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取提交历史时出错: {ex.Message}");
        }

        return commits;
    }

    // 获取分支列表
    public List<BranchInfo> GetBranches()
    {
        var branches = new List<BranchInfo>();

        if (string.IsNullOrEmpty(_repoPath))
        {
            return branches;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            foreach (var branch in repo.Branches)
            {
                branches.Add(new BranchInfo
                {
                    Name = branch.FriendlyName,
                    IsRemote = branch.IsRemote,
                    IsHead = branch.IsCurrentRepositoryHead,
                    UpstreamBranchName = branch.RemoteName,
                    Tip = branch.Tip?.Sha
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取分支列表时出错: {ex.Message}");
        }

        return branches;
    }

    // 切换分支
    public bool CheckoutBranch(string branchName)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            var branch = repo.Branches[branchName];

            if (branch == null)
            {
                return false;
            }

            var options = new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force
            };

            Commands.Checkout(repo, branch, options);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换分支时出错: {ex.Message}");
            return false;
        }
    }

    // 创建新分支
    public bool CreateBranch(string branchName)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            repo.CreateBranch(branchName);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建分支时出错: {ex.Message}");
            return false;
        }
    }

    // 提交更改
    public string Commit(string message, string authorName, string authorEmail)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return string.Empty;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 签名信息
            var signature = new Signature(authorName, authorEmail, DateTimeOffset.Now);

            // 提交
            var commit = repo.Commit(message, signature, signature);
            return commit.Sha;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交更改时出错: {ex.Message}");
            return string.Empty;
        }
    }

    // 添加文件到暂存区
    public bool StageFile(string filePath)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            Commands.Stage(repo, filePath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"暂存文件时出错: {ex.Message}");
            return false;
        }
    }

    // 取消暂存文件
    public bool UnstageFile(string filePath)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            Commands.Unstage(repo, filePath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"取消暂存文件时出错: {ex.Message}");
            return false;
        }
    }

    // 合并分支
    public ModelMergeResult MergeBranch(string branchName)
    {
        var result = new ModelMergeResult { Success = false };

        if (string.IsNullOrEmpty(_repoPath))
        {
            return result;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 获取当前分支和目标分支
            var currentBranch = repo.Head;
            var mergeBranch = repo.Branches[branchName];

            if (mergeBranch == null)
            {
                result.ErrorMessage = $"找不到分支: {branchName}";
                return result;
            }

            // 设置合并选项
            var options = new MergeOptions
            {
                FastForwardStrategy = FastForwardStrategy.Default
            };

            // 执行合并
            var signature = new Signature(
                repo.Config.GetValueOrDefault<string>("user.name", "Unknown"),
                repo.Config.GetValueOrDefault<string>("user.email", "unknown@example.com"),
                DateTimeOffset.Now);

            var mergeResult = repo.Merge(mergeBranch, signature, options);

            // 处理合并结果
            result.Status = mergeResult.Status.ToString();

            switch (mergeResult.Status)
            {
                case MergeStatus.Conflicts:
                    result.Success = false;
                    result.HasConflicts = true;
                    result.ErrorMessage = "合并过程中产生了冲突";
                    break;
                case MergeStatus.FastForward:
                case MergeStatus.NonFastForward:
                    result.Success = true;
                    result.Commit = mergeResult.Commit?.Sha;
                    break;
                case MergeStatus.UpToDate:
                    result.Success = true;
                    result.WasUpToDate = true;
                    break;
                default:
                    result.Success = false;
                    result.ErrorMessage = $"合并状态: {mergeResult.Status}";
                    break;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"合并分支时出错: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(result.ErrorMessage);
            return result;
        }
    }

    // 获取远程仓库信息
    public List<RemoteInfo> GetRemotes()
    {
        var remotes = new List<RemoteInfo>();

        if (string.IsNullOrEmpty(_repoPath))
        {
            return remotes;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            foreach (var remote in repo.Network.Remotes)
            {
                remotes.Add(new RemoteInfo
                {
                    Name = remote.Name,
                    Url = remote.Url,
                    PushUrl = remote.PushUrl ?? remote.Url
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取远程仓库信息时出错: {ex.Message}");
        }

        return remotes;
    }

    // 添加远程仓库
    public bool AddRemote(string name, string url)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            repo.Network.Remotes.Add(name, url);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"添加远程仓库时出错: {ex.Message}");
            return false;
        }
    }

    // 推送到远程仓库
    public bool Push(string remoteName, string branchName, string username = null, string password = null)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 获取远程和本地分支引用
            var remote = repo.Network.Remotes[remoteName];
            if (remote == null)
            {
                System.Diagnostics.Debug.WriteLine($"找不到远程仓库: {remoteName}");
                return false;
            }

            var localBranch = repo.Branches[branchName];
            if (localBranch == null)
            {
                System.Diagnostics.Debug.WriteLine($"找不到本地分支: {branchName}");
                return false;
            }

            // 构建推送引用规范
            var pushRefSpec = $"{localBranch.CanonicalName}:{localBranch.CanonicalName}";

            // 配置推送选项
            var pushOptions = new PushOptions();

            // 如果提供了凭据，添加凭据处理
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                pushOptions.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = username, Password = password };
            }

            // 执行推送
            repo.Network.Push(remote, pushRefSpec, pushOptions);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"推送到远程仓库时出错: {ex.Message}");
            return false;
        }
    }

    // 从远程仓库拉取
    public bool Pull(string remoteName, string branchName, string username = null, string password = null)
    {
        if (string.IsNullOrEmpty(_repoPath))
        {
            return false;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 获取远程
            var remote = repo.Network.Remotes[remoteName];
            if (remote == null)
            {
                System.Diagnostics.Debug.WriteLine($"找不到远程仓库: {remoteName}");
                return false;
            }

            // 配置获取选项
            var fetchOptions = new FetchOptions();

            // 如果提供了凭据，添加凭据处理
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                fetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = username, Password = password };
            }

            // 获取远程引用
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, "拉取更新");

            // 设置合并选项
            var options = new MergeOptions
            {
                FastForwardStrategy = FastForwardStrategy.Default
            };

            // 合并FETCH_HEAD
            var signature = new Signature(
                repo.Config.GetValueOrDefault<string>("user.name", "Unknown"),
                repo.Config.GetValueOrDefault<string>("user.email", "unknown@example.com"),
                DateTimeOffset.Now);

            // 修复FetchHead问题
            var fetchHeadRef = repo.Refs["FETCH_HEAD"];
            if (fetchHeadRef == null)
            {
                System.Diagnostics.Debug.WriteLine("无法找到FETCH_HEAD引用");
                return false;
            }

            // 获取目标Commit对象用于合并
            var commitToMerge = fetchHeadRef.ResolveToDirectReference().Target as Commit;
            if (commitToMerge == null)
            {
                System.Diagnostics.Debug.WriteLine("无法解析FETCH_HEAD为有效的提交对象");
                return false;
            }

            var mergeResult = repo.Merge(commitToMerge, signature, options);

            return mergeResult.Status != MergeStatus.Conflicts;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"拉取远程仓库时出错: {ex.Message}");
            return false;
        }
    }

    // 获取仓库状态
    public ModelRepositoryStatus GetRepositoryStatus()
    {
        var status = new ModelRepositoryStatus();

        if (string.IsNullOrEmpty(_repoPath))
        {
            return status;
        }

        try
        {
            using var repo = new Repository(_repoPath);

            // 获取当前分支
            var currentBranch = repo.Head;
            status.CurrentBranchName = currentBranch.FriendlyName;

            // 处理状态
            var repoStatus = repo.RetrieveStatus();

            // 统计状态
            status.Added = repoStatus.Added.Count();
            status.Modified = repoStatus.Modified.Count();
            status.Removed = repoStatus.Removed.Count();
            status.Untracked = repoStatus.Untracked.Count();
            status.Staged = repoStatus.Staged.Count();
            status.Missing = repoStatus.Missing.Count();
            // 修复Conflicted属性访问
            status.Conflicted = repoStatus.Where(item => item.State.HasFlag(LibFileStatus.Conflicted)).Count();

            // 文件列表
            foreach (var item in repoStatus)
            {
                status.Files.Add(new ModelFileStatus
                {
                    FilePath = item.FilePath,
                    State = item.State.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取仓库状态时出错: {ex.Message}");
        }

        return status;
    }
}