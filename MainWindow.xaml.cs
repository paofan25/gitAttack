// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// 主窗口逻辑
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using gitAttack.Utils;
using gitAttack.ViewModels;

namespace gitAttack;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        // 注册资源转换器
        Resources.Add("BooleanToVisibilityConverter", new Utils.BooleanToVisibilityConverter());
        Resources.Add("InverseBooleanToVisibilityConverter", new Utils.InverseBooleanToVisibilityConverter());
        Resources.Add("ConflictStatusConverter", new ConflictStatusConverter());

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        InitializeComponent();

        // 监听密钥框变化
        ApiKeyBox.PasswordChanged += ApiKeyBox_PasswordChanged;

        // 从环境变量加载API密钥和URL（如果有）
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            ApiKeyBox.Password = apiKey;
            _viewModel.ApiKey = apiKey;
        }

        // 绑定模型下拉框默认选项
        if (ModelComboBox.Items.Count == 0)
        {
            foreach (var model in gitAttack.Models.ApiSettings.PredefinedModels)
            {
                _viewModel.ApiSettings.AddModelHistory(model);
            }
        }

        // 设置默认终端点
        if (string.IsNullOrEmpty(_viewModel.ApiEndpoint))
        {
            _viewModel.ApiEndpoint = "https://api.deepseek.com/chat/completions";
        }

        // 确保API端点格式正确
        EnsureApiEndpointFormat();

        // 初始化完成时显示设置提示
        StatusMessage.Text = "提示: 请确保API URL包含完整路径，如 https://api.deepseek.com/chat/completions";
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.ApiKey = passwordBox.Password;
        }
    }

    private void EnsureApiEndpointFormat()
    {
        // 检查当前API端点是否有效
        var endpoint = _viewModel.ApiEndpoint;
        if (!string.IsNullOrEmpty(endpoint))
        {
            // 如果不是以/chat/completions结尾，调整格式
            if (!endpoint.EndsWith("/chat/completions"))
            {
                if (endpoint.EndsWith("/v1"))
                {
                    _viewModel.ApiEndpoint = endpoint.Substring(0, endpoint.Length - 3) + "/chat/completions";
                }
                else if (!endpoint.Contains("/chat/"))
                {
                    if (endpoint.EndsWith("/"))
                    {
                        endpoint = endpoint.Substring(0, endpoint.Length - 1);
                    }
                    _viewModel.ApiEndpoint = endpoint + "/chat/completions";
                }
            }
        }
    }
}