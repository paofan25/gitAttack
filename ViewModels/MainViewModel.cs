// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// 主视图模型
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using gitAttack.AI;
using gitAttack.Models;
using gitAttack.Services;

namespace gitAttack.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GitService _gitService;
    private readonly UnitySceneService _unitySceneService;
    private readonly AIService _aiService;
    private readonly SettingsService _settingsService;

    private string _repositoryPath = string.Empty;
    private UnitySceneConflict? _selectedConflict;
    private ConflictSection? _selectedConflictSection;
    private string _statusMessage = "请选择Git仓库目录";
    private bool _isLoading = false;
    private string _apiKey = string.Empty;
    private string _apiEndpoint = "https://api.deepseek.com/chat/completions";
    private string _selectedModel = "deepseek-reasoner";
    private string _repositoryName = string.Empty;
    private bool _isValidRepository = false;
    private ApiSettings _apiSettings;
    private ApiKeyHistory? _selectedApiKeyHistory;
    private ApiEndpointHistory? _selectedEndpointHistory;
    private ApiModelHistory? _selectedModelHistory;

    public ObservableCollection<UnitySceneConflict> Conflicts { get; } = new();

    public ApiSettings ApiSettings => _apiSettings;

    public ApiKeyHistory? SelectedApiKeyHistory
    {
        get => _selectedApiKeyHistory;
        set
        {
            if (_selectedApiKeyHistory != value)
            {
                _selectedApiKeyHistory = value;
                OnPropertyChanged();
                if (value != null)
                {
                    ApiKey = value.ApiKey;
                }
            }
        }
    }

    public ApiEndpointHistory? SelectedEndpointHistory
    {
        get => _selectedEndpointHistory;
        set
        {
            if (_selectedEndpointHistory != value)
            {
                _selectedEndpointHistory = value;
                OnPropertyChanged();
                if (value != null)
                {
                    ApiEndpoint = value.Url;
                }
            }
        }
    }

    public ApiModelHistory? SelectedModelHistory
    {
        get => _selectedModelHistory;
        set
        {
            if (_selectedModelHistory != value)
            {
                _selectedModelHistory = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedModel = value.ModelId;
                }
            }
        }
    }

    public string RepositoryPath
    {
        get => _repositoryPath;
        set
        {
            if (_repositoryPath != value)
            {
                _repositoryPath = value;
                OnPropertyChanged();
            }
        }
    }

    public string RepositoryName
    {
        get => _repositoryName;
        set
        {
            if (_repositoryName != value)
            {
                _repositoryName = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsValidRepository
    {
        get => _isValidRepository;
        set
        {
            if (_isValidRepository != value)
            {
                _isValidRepository = value;
                OnPropertyChanged();
            }
        }
    }

    public UnitySceneConflict? SelectedConflict
    {
        get => _selectedConflict;
        set
        {
            if (_selectedConflict != value)
            {
                _selectedConflict = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConflictSelected));
            }
        }
    }

    public ConflictSection? SelectedConflictSection
    {
        get => _selectedConflictSection;
        set
        {
            if (_selectedConflictSection != value)
            {
                _selectedConflictSection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConflictSectionSelected));

                // 当选择了冲突区域，自动分析
                if (value != null)
                {
                    // AnalyzeConflictSection(value);
                }
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

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;

                // 更新设置并保存历史记录
                _apiSettings.AddApiKeyHistory(value);
                _settingsService.SaveSettings(_apiSettings);

                // 配置AI服务
                _aiService.Configure(_apiKey, _apiEndpoint, _selectedModel);

                OnPropertyChanged();
            }
        }
    }

    public string ApiEndpoint
    {
        get => _apiEndpoint;
        set
        {
            if (_apiEndpoint != value)
            {
                _apiEndpoint = value;

                // 更新设置并保存历史记录
                _apiSettings.AddEndpointHistory(value);
                _settingsService.SaveSettings(_apiSettings);

                // 配置AI服务
                _aiService.Configure(_apiKey, _apiEndpoint, _selectedModel);

                OnPropertyChanged();
            }
        }
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (_selectedModel != value)
            {
                _selectedModel = value;

                // 更新设置并保存历史记录
                _apiSettings.AddModelHistory(value);
                _settingsService.SaveSettings(_apiSettings);

                // 配置AI服务
                _aiService.Configure(_apiKey, _apiEndpoint, _selectedModel);

                OnPropertyChanged();
            }
        }
    }

    public bool IsConflictSelected => SelectedConflict != null;
    public bool IsConflictSectionSelected => SelectedConflictSection != null;

    public ICommand OpenRepositoryCommand { get; }
    public ICommand RefreshConflictsCommand { get; }
    public ICommand SaveResolvedFileCommand { get; }
    public ICommand UseOurVersionCommand { get; }
    public ICommand UseTheirVersionCommand { get; }
    public ICommand UseAiSuggestionCommand { get; }
    public ICommand InitializeRepositoryCommand { get; }
    public ICommand RemoveApiKeyCommand { get; }
    public ICommand RemoveEndpointCommand { get; }
    public ICommand RemoveModelCommand { get; }
    public ICommand RefreshAiSuggestionCommand { get; }

    public MainViewModel()
    {
        _settingsService = new SettingsService();
        _apiSettings = _settingsService.LoadSettings();

        _gitService = new GitService();
        _unitySceneService = new UnitySceneService();
        _aiService = new AIService();

        // 初始化API设置
        _apiKey = _apiSettings.CurrentApiKey;
        _apiEndpoint = _apiSettings.CurrentEndpoint;
        _selectedModel = _apiSettings.CurrentModel;
        _aiService.Configure(_apiKey, _apiEndpoint, _selectedModel);

        OpenRepositoryCommand = new RelayCommand(OpenRepository);
        RefreshConflictsCommand = new RelayCommand(RefreshConflicts);
        SaveResolvedFileCommand = new RelayCommand(SaveResolvedFile, () => SelectedConflict?.IsResolved == true);
        UseOurVersionCommand = new RelayCommand(UseOurVersion, () => SelectedConflictSection != null);
        UseTheirVersionCommand = new RelayCommand(UseTheirVersion, () => SelectedConflictSection != null);
        UseAiSuggestionCommand = new RelayCommand(UseAiSuggestionAsync, () => SelectedConflictSection != null && !string.IsNullOrEmpty(ApiKey));
        InitializeRepositoryCommand = new RelayCommand(InitializeRepository, () => !string.IsNullOrEmpty(RepositoryPath) && !IsValidRepository);
        RemoveApiKeyCommand = new RelayCommand<ApiKeyHistory>(RemoveApiKey);
        RemoveEndpointCommand = new RelayCommand<ApiEndpointHistory>(RemoveEndpoint);
        RemoveModelCommand = new RelayCommand<ApiModelHistory>(RemoveModel);
        RefreshAiSuggestionCommand = new RelayCommand(RefreshAiSuggestion, () => SelectedConflictSection != null && !string.IsNullOrEmpty(ApiKey));
    }

    private void RemoveApiKey(ApiKeyHistory history)
    {
        if (history != null)
        {
            _apiSettings.RemoveApiKeyHistory(history);
            _settingsService.SaveSettings(_apiSettings);
            OnPropertyChanged(nameof(ApiSettings));
        }
    }

    private void RemoveEndpoint(ApiEndpointHistory history)
    {
        if (history != null)
        {
            _apiSettings.RemoveEndpointHistory(history);
            _settingsService.SaveSettings(_apiSettings);
            OnPropertyChanged(nameof(ApiSettings));
        }
    }

    private void RemoveModel(ApiModelHistory history)
    {
        if (history != null)
        {
            _apiSettings.RemoveModelHistory(history);
            _settingsService.SaveSettings(_apiSettings);
            OnPropertyChanged(nameof(ApiSettings));
        }
    }

    private void RefreshAiSuggestion()
    {
        if (SelectedConflictSection != null && !string.IsNullOrEmpty(ApiKey))
        {
            _ = GetAiSuggestionAsync(SelectedConflictSection);
        }
    }

    private void OpenRepository()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择Git仓库目录",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        var result = dialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.OK)
        {
            RepositoryPath = dialog.SelectedPath;

            // 检查是否是有效的Git仓库
            IsValidRepository = _gitService.IsGitRepository(RepositoryPath);

            if (IsValidRepository)
            {
                if (_gitService.OpenRepository(RepositoryPath))
                {
                    RepositoryName = _gitService.GetRepositoryName();
                    StatusMessage = $"已打开Git仓库：{RepositoryName}";
                    RefreshConflicts();
                }
                else
                {
                    IsValidRepository = false;
                    RepositoryName = string.Empty;
                    StatusMessage = "无法打开仓库，请检查访问权限";
                }
            }
            else
            {
                RepositoryName = Path.GetFileName(RepositoryPath) ?? string.Empty;
                StatusMessage = $"【{RepositoryName}】不是有效的Git仓库。可以点击'初始化仓库'按钮创建新仓库。";
            }
        }
    }

    private void InitializeRepository()
    {
        if (string.IsNullOrEmpty(RepositoryPath))
        {
            StatusMessage = "请先选择一个目录";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在初始化Git仓库...";

        try
        {
            if (_gitService.InitializeRepository(RepositoryPath))
            {
                IsValidRepository = true;
                RepositoryName = _gitService.GetRepositoryName();
                StatusMessage = $"已成功初始化Git仓库：{RepositoryName}";
                RefreshConflicts();
            }
            else
            {
                StatusMessage = "初始化Git仓库失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"初始化Git仓库时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshConflicts()
    {
        StatusMessage = "正在刷新冲突文件列表...";
        IsLoading = true;

        try
        {
            Conflicts.Clear();
            SelectedConflict = null;
            SelectedConflictSection = null;

            var conflictedFiles = _gitService.GetConflictedUnitySceneFiles();

            foreach (var filePath in conflictedFiles)
            {
                try
                {
                    var conflict = _gitService.ParseConflictFile(filePath);
                    Conflicts.Add(conflict);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"解析文件 {filePath} 时出错: {ex.Message}";
                }
            }

            StatusMessage = $"找到 {Conflicts.Count} 个冲突的Unity场景文件";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新冲突文件列表时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SaveResolvedFile()
    {
        if (SelectedConflict?.IsResolved == true)
        {
            try
            {
                _gitService.SaveResolvedFile(SelectedConflict);
                StatusMessage = $"已保存解决的文件: {SelectedConflict.FileName}";

                // 刷新冲突列表
                RefreshConflicts();
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存文件时出错: {ex.Message}";
            }
        }
    }

    private void UseOurVersion()
    {
        if (SelectedConflictSection != null)
        {
            SelectedConflictSection.ResolvedContent = SelectedConflictSection.OurContent;
            StatusMessage = "已选择我们的版本";
            OnPropertyChanged(nameof(SelectedConflictSection));
            OnPropertyChanged(nameof(SelectedConflict));
        }
    }

    private void UseTheirVersion()
    {
        if (SelectedConflictSection != null)
        {
            SelectedConflictSection.ResolvedContent = SelectedConflictSection.TheirContent;
            StatusMessage = "已选择他们的版本";
            OnPropertyChanged(nameof(SelectedConflictSection));
            OnPropertyChanged(nameof(SelectedConflict));
        }
    }

    private async void UseAiSuggestionAsync()
    {
        if (SelectedConflictSection != null && !string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "正在请求AI建议...";
            IsLoading = true;

            try
            {
                var mergedContent = await _aiService.GetMergedContent(SelectedConflictSection);
                SelectedConflictSection.ResolvedContent = mergedContent;
                StatusMessage = "已应用AI建议的合并";

                OnPropertyChanged(nameof(SelectedConflictSection));
                OnPropertyChanged(nameof(SelectedConflict));
            }
            catch (Exception ex)
            {
                StatusMessage = $"获取AI建议时出错: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void AnalyzeConflictSection(ConflictSection section)
    {
        try
        {
            _unitySceneService.AnalyzeConflict(section);

            if (!string.IsNullOrEmpty(ApiKey))
            {
                _ = GetAiSuggestionAsync(section);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"分析冲突区域时出错: {ex.Message}";
        }
    }

    private async Task GetAiSuggestionAsync(ConflictSection section)
    {
        StatusMessage = "正在获取AI分析...";
        IsLoading = true;

        try
        {
            var suggestion = await _aiService.GetConflictResolutionSuggestion(section);
            section.SuggestionFromAI = suggestion;
            OnPropertyChanged(nameof(SelectedConflictSection));
            StatusMessage = "已获取AI分析";
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取AI分析时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// 一个简单的命令实现
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

// 带参数的命令实现
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return parameter == null || parameter is T p && (_canExecute == null || _canExecute(p));
    }

    public void Execute(object? parameter)
    {
        if (parameter is T p)
        {
            _execute(p);
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}