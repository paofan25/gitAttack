// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// 主要负责AI服务相关逻辑
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using gitAttack.Models;

namespace gitAttack.AI;

public class AIService
{
    private readonly HttpClient _httpClient;
    private string _apiKey = string.Empty;
    private string _apiEndpoint = "https://api.deepseek.com/chat/completions";
    private string _model = "deepseek-reasoner";

    public AIService()
    {
        _httpClient = new HttpClient();
    }

    public bool Configure(string apiKey, string apiEndpoint = "", string model = "")
    {
        _apiKey = apiKey;
        if (!string.IsNullOrEmpty(apiEndpoint))
        {
            _apiEndpoint = apiEndpoint;

            // 确保端点URL包含完整的/chat/completions路径
            if (!_apiEndpoint.EndsWith("/chat/completions"))
            {
                // 如果是/v1结尾，则替换为/chat/completions
                if (_apiEndpoint.EndsWith("/v1"))
                {
                    _apiEndpoint = _apiEndpoint.Substring(0, _apiEndpoint.Length - 3) + "/chat/completions";
                }
                // 如果只是基础URL，则添加/chat/completions
                else if (!_apiEndpoint.Contains("/chat/"))
                {
                    // 移除末尾的斜杠(如果有)
                    if (_apiEndpoint.EndsWith("/"))
                    {
                        _apiEndpoint = _apiEndpoint.Substring(0, _apiEndpoint.Length - 1);
                    }
                    _apiEndpoint += "/chat/completions";
                }
            }
        }

        if (!string.IsNullOrEmpty(model))
        {
            _model = model;
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        System.Diagnostics.Debug.WriteLine($"配置AI服务: 端点={_apiEndpoint}, 模型={_model}");
        return !string.IsNullOrEmpty(_apiKey);
    }

    public async Task<string> GetConflictResolutionSuggestion(ConflictSection conflict)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "请先配置API密钥";
        }

        try
        {
            var requestData = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个Unity场景冲突解决专家。请分析下面冲突的两个版本的YAML内容，并提供最佳的解决方案。解释冲突的主要差异，并说明为什么你的解决方案是最佳的。" },
                    new { role = "user", content = $"分析这个Unity场景冲突:\n\n版本1 (我们的版本):\n{conflict.OurContent}\n\n版本2 (他们的版本):\n{conflict.TheirContent}\n\n提供解决方案，并解释为什么这是最佳方案。" }
                },
                temperature = 0.3,
                max_tokens = 1000
            };

            // 直接使用StringContent发送请求，确保Content-Type正确
            var jsonContent = JsonSerializer.Serialize(requestData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"发送请求到: {_apiEndpoint}");
            System.Diagnostics.Debug.WriteLine($"请求内容: {jsonContent}");

            // 发送请求
            var response = await _httpClient.PostAsync(_apiEndpoint, httpContent);

            // 记录响应信息，无论成功与否
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"响应状态: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            // 检查响应状态
            if (!response.IsSuccessStatusCode)
            {
                return $"调用AI API出错: {response.StatusCode} - {responseContent}";
            }

            var resultJson = JsonDocument.Parse(responseContent);

            // 提取AI的回复
            var choices = resultJson.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                var aiContent = message.GetProperty("content").GetString();
                return aiContent ?? "AI未能提供有用的建议";
            }

            return "未能获取AI建议";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AI API错误详情: {ex}");
            return $"调用AI API出错: {ex.Message}";
        }
    }

    public async Task<string> GetMergedContent(ConflictSection conflict)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "请先配置API密钥";
        }

        try
        {
            var requestData = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个Unity场景合并专家。根据两个冲突版本的内容，生成一个合并后的版本。只返回合并后的YAML内容，不要包含任何解释或说明。" },
                    new { role = "user", content = $"合并这两个Unity场景版本:\n\n版本1 (我们的版本):\n{conflict.OurContent}\n\n版本2 (他们的版本):\n{conflict.TheirContent}" }
                },
                temperature = 0.2,
                max_tokens = 2000
            };

            // 直接使用StringContent发送请求
            var jsonContent = JsonSerializer.Serialize(requestData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 调试信息
            System.Diagnostics.Debug.WriteLine($"响应状态: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            // 检查响应状态
            if (!response.IsSuccessStatusCode)
            {
                return $"调用AI API出错: {response.StatusCode} - {responseContent}";
            }

            var resultJson = JsonDocument.Parse(responseContent);

            // 提取AI的回复
            var choices = resultJson.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                var aiContent = message.GetProperty("content").GetString();
                return aiContent ?? conflict.OurContent; // 如果AI未能提供合并内容，则保留我们的版本
            }

            return conflict.OurContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AI API错误详情: {ex}");
            return conflict.OurContent; // 出错时保留我们的版本
        }
    }
}