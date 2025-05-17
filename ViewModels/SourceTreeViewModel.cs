// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// SourceTree视图模型
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using gitAttack.Models;
using gitAttack.Services;

namespace gitAttack.ViewModels;

public class SourceTreeViewModel : INotifyPropertyChanged
{
    private readonly GitService _gitService;

    // 数据集合
    public ObservableCollection<CommitInfo> CommitHistory { get; } = new();
    public ObservableCollection<BranchInfo> Branches { get; } = new();
    public ObservableCollection<RemoteInfo> Remotes { get; } = new();
    public ObservableCollection<FileStatus> ChangedFiles { get; } = new();

    // 状态信息
    private RepositoryStatus _repoStatus = new();
    private string _statusMessage = string.Empty;
    private bool _isLoading = false;
    private bool _isRefreshing = false;

    // 选中的对象
    private CommitInfo? _selectedCommit;
    private BranchInfo? _selectedBranch;
    private RemoteInfo? _selectedRemote;
    private FileStatus? _selectedFile;

    // 表单输入
    private string _newBranchName = string.Empty;
    private string _commitMessage = string.Empty;
    private string _remoteName = string.Empty;
    private string _remoteUrl = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    // 构造函数
    public SourceTreeViewModel(GitService gitService)
    {
        _gitService = gitService;

        // 初始化命令
        RefreshCommand = new RelayCommand(RefreshAll);
        CreateBranchCommand = new RelayCommand(CreateBranch, () => !string.IsNullOrEmpty(NewBranchName));
        CheckoutBranchCommand = new RelayCommand(CheckoutBranch, () => SelectedBranch != null);
        StageFileCommand = new RelayCommand(StageFile, () => SelectedFile != null);
        UnstageFileCommand = new RelayCommand(UnstageFile, () => SelectedFile != null);
        CommitCommand = new RelayCommand(Commit, () => !string.IsNullOrEmpty(CommitMessage) && RepoStatus.Staged > 0);
        MergeBranchCommand = new RelayCommand(MergeBranch, () => SelectedBranch != null);
        AddRemoteCommand = new RelayCommand(AddRemote, () => !string.IsNullOrEmpty(RemoteName) && !string.IsNullOrEmpty(RemoteUrl));
        PushCommand = new RelayCommand(Push, () => SelectedRemote != null);
        PullCommand = new RelayCommand(Pull, () => SelectedRemote != null);
    }

    // 属性
    public RepositoryStatus RepoStatus
    {
        get => _repoStatus;
        set
        {
            if (_repoStatus != value)
            {
                _repoStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }

    public CommitInfo? SelectedCommit
    {
        get => _selectedCommit;
        set
        {
            if (_selectedCommit != value)
            {
                _selectedCommit = value;
                OnPropertyChanged();
            }
        }
    }

    public BranchInfo? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (_selectedBranch != value)
            {
                _selectedBranch = value;
                OnPropertyChanged();
            }
        }
    }

    public RemoteInfo? SelectedRemote
    {
        get => _selectedRemote;
        set
        {
            if (_selectedRemote != value)
            {
                _selectedRemote = value;
                OnPropertyChanged();
            }
        }
    }

    public FileStatus? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (_selectedFile != value)
            {
                _selectedFile = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewBranchName
    {
        get => _newBranchName;
        set
        {
            if (_newBranchName != value)
            {
                _newBranchName = value;
                OnPropertyChanged();
            }
        }
    }

    public string CommitMessage
    {
        get => _commitMessage;
        set
        {
            if (_commitMessage != value)
            {
                _commitMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string RemoteName
    {
        get => _remoteName;
        set
        {
            if (_remoteName != value)
            {
                _remoteName = value;
                OnPropertyChanged();
            }
        }
    }

    public string RemoteUrl
    {
        get => _remoteUrl;
        set
        {
            if (_remoteUrl != value)
            {
                _remoteUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand RefreshCommand { get; }
    public ICommand CreateBranchCommand { get; }
    public ICommand CheckoutBranchCommand { get; }
    public ICommand StageFileCommand { get; }
    public ICommand UnstageFileCommand { get; }
    public ICommand CommitCommand { get; }
    public ICommand MergeBranchCommand { get; }
    public ICommand AddRemoteCommand { get; }
    public ICommand PushCommand { get; }
    public ICommand PullCommand { get; }

    // 方法
    public void RefreshAll()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        StatusMessage = "正在刷新仓库状态...";

        try
        {
            // 获取提交历史
            RefreshCommits();

            // 获取分支列表
            RefreshBranches();

            // 获取远程仓库信息
            RefreshRemotes();

            // 获取文件状态
            RefreshStatus();

            StatusMessage = $"刷新完成。当前分支: {RepoStatus.CurrentBranchName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新仓库信息时出错: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void RefreshCommits()
    {
        var commits = _gitService.GetCommitHistory();
        CommitHistory.Clear();
        foreach (var commit in commits)
        {
            CommitHistory.Add(commit);
        }
    }

    private void RefreshBranches()
    {
        var branches = _gitService.GetBranches();
        Branches.Clear();
        foreach (var branch in branches)
        {
            Branches.Add(branch);
        }
    }

    private void RefreshRemotes()
    {
        var remotes = _gitService.GetRemotes();
        Remotes.Clear();
        foreach (var remote in remotes)
        {
            Remotes.Add(remote);
        }
    }

    private void RefreshStatus()
    {
        RepoStatus = _gitService.GetRepositoryStatus();
        ChangedFiles.Clear();
        foreach (var file in RepoStatus.Files)
        {
            ChangedFiles.Add(file);
        }
    }

    private void CreateBranch()
    {
        IsLoading = true;
        StatusMessage = $"正在创建分支: {NewBranchName}...";

        try
        {
            if (_gitService.CreateBranch(NewBranchName))
            {
                StatusMessage = $"成功创建分支: {NewBranchName}";
                NewBranchName = string.Empty;
                RefreshBranches();
            }
            else
            {
                StatusMessage = $"创建分支失败: {NewBranchName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建分支时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CheckoutBranch()
    {
        if (SelectedBranch == null) return;

        IsLoading = true;
        StatusMessage = $"正在切换到分支: {SelectedBranch.Name}...";

        try
        {
            if (_gitService.CheckoutBranch(SelectedBranch.Name))
            {
                StatusMessage = $"已切换到分支: {SelectedBranch.Name}";
                RefreshAll();
            }
            else
            {
                StatusMessage = $"切换分支失败: {SelectedBranch.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"切换分支时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StageFile()
    {
        if (SelectedFile == null) return;

        IsLoading = true;
        StatusMessage = $"正在暂存文件: {SelectedFile.FilePath}...";

        try
        {
            if (_gitService.StageFile(SelectedFile.FilePath))
            {
                StatusMessage = $"已暂存文件: {SelectedFile.FilePath}";
                RefreshStatus();
            }
            else
            {
                StatusMessage = $"暂存文件失败: {SelectedFile.FilePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"暂存文件时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UnstageFile()
    {
        if (SelectedFile == null) return;

        IsLoading = true;
        StatusMessage = $"正在取消暂存文件: {SelectedFile.FilePath}...";

        try
        {
            if (_gitService.UnstageFile(SelectedFile.FilePath))
            {
                StatusMessage = $"已取消暂存文件: {SelectedFile.FilePath}";
                RefreshStatus();
            }
            else
            {
                StatusMessage = $"取消暂存文件失败: {SelectedFile.FilePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消暂存文件时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Commit()
    {
        if (string.IsNullOrEmpty(CommitMessage)) return;

        IsLoading = true;
        StatusMessage = "正在提交更改...";

        try
        {
            var authorName = "Unity Merge AI"; // 可以从配置获取
            var authorEmail = "unity.merge.ai@example.com"; // 可以从配置获取

            var commitSha = _gitService.Commit(CommitMessage, authorName, authorEmail);

            if (!string.IsNullOrEmpty(commitSha))
            {
                StatusMessage = $"已成功提交更改: {commitSha.Substring(0, 7)}";
                CommitMessage = string.Empty;
                RefreshAll();
            }
            else
            {
                StatusMessage = "提交更改失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"提交更改时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void MergeBranch()
    {
        if (SelectedBranch == null) return;

        IsLoading = true;
        StatusMessage = $"正在合并分支: {SelectedBranch.Name}...";

        try
        {
            var result = _gitService.MergeBranch(SelectedBranch.Name);

            if (result.Success)
            {
                if (result.WasUpToDate)
                {
                    StatusMessage = $"分支已是最新: {SelectedBranch.Name}";
                }
                else
                {
                    StatusMessage = $"已成功合并分支: {SelectedBranch.Name}";
                }
                RefreshAll();
            }
            else
            {
                if (result.HasConflicts)
                {
                    StatusMessage = $"合并产生冲突，需要解决冲突后再继续";
                    RefreshAll();
                }
                else
                {
                    StatusMessage = $"合并失败: {result.ErrorMessage}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"合并分支时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddRemote()
    {
        if (string.IsNullOrEmpty(RemoteName) || string.IsNullOrEmpty(RemoteUrl)) return;

        IsLoading = true;
        StatusMessage = $"正在添加远程仓库: {RemoteName}...";

        try
        {
            if (_gitService.AddRemote(RemoteName, RemoteUrl))
            {
                StatusMessage = $"已添加远程仓库: {RemoteName} ({RemoteUrl})";
                RemoteName = string.Empty;
                RemoteUrl = string.Empty;
                RefreshRemotes();
            }
            else
            {
                StatusMessage = $"添加远程仓库失败: {RemoteName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加远程仓库时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Push()
    {
        if (SelectedRemote == null) return;

        IsLoading = true;
        StatusMessage = $"正在推送到远程仓库: {SelectedRemote.Name}...";

        try
        {
            string branchName = RepoStatus.CurrentBranchName;

            if (_gitService.Push(SelectedRemote.Name, branchName, Username, Password))
            {
                StatusMessage = $"已成功推送到远程仓库: {SelectedRemote.Name}/{branchName}";
                RefreshAll();
            }
            else
            {
                StatusMessage = $"推送到远程仓库失败: {SelectedRemote.Name}/{branchName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"推送到远程仓库时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Pull()
    {
        if (SelectedRemote == null) return;

        IsLoading = true;
        StatusMessage = $"正在从远程仓库拉取: {SelectedRemote.Name}...";

        try
        {
            string branchName = RepoStatus.CurrentBranchName;

            if (_gitService.Pull(SelectedRemote.Name, branchName, Username, Password))
            {
                StatusMessage = $"已成功从远程仓库拉取: {SelectedRemote.Name}/{branchName}";
                RefreshAll();
            }
            else
            {
                StatusMessage = $"从远程仓库拉取失败: {SelectedRemote.Name}/{branchName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"从远程仓库拉取时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // 属性更改通知
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}