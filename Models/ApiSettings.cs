// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// API设置模型
using System.Collections.ObjectModel;

namespace gitAttack.Models;

public class ApiKeyHistory
{
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }
}

public class ApiEndpointHistory
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }
}

public class ApiModelHistory
{
    public string Name { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }
}

public class ApiSettings
{
    public string CurrentApiKey { get; set; } = string.Empty;
    public string CurrentEndpoint { get; set; } = "https://api.deepseek.com/chat/completions";
    public string CurrentModel { get; set; } = "deepseek-reasoner";

    public ObservableCollection<ApiKeyHistory> ApiKeyHistories { get; set; } = new();
    public ObservableCollection<ApiEndpointHistory> EndpointHistories { get; set; } = new();
    public ObservableCollection<ApiModelHistory> ModelHistories { get; set; } = new();

    // 预设模型列表
    public static string[] PredefinedModels = new[]
    {
        "deepseek-chat",
        "deepseek-reasoner"
    };

    // 添加API密钥历史记录
    public void AddApiKeyHistory(string key, string name = "")
    {
        if (string.IsNullOrEmpty(key))
            return;

        var existing = ApiKeyHistories.FirstOrDefault(h => h.ApiKey == key);
        if (existing != null)
        {
            existing.LastUsed = DateTime.Now;
            if (!string.IsNullOrEmpty(name))
                existing.Name = name;
        }
        else
        {
            ApiKeyHistories.Add(new ApiKeyHistory
            {
                ApiKey = key,
                Name = string.IsNullOrEmpty(name) ? $"密钥 {ApiKeyHistories.Count + 1}" : name,
                LastUsed = DateTime.Now
            });
        }

        CurrentApiKey = key;
    }

    // 添加API端点历史记录
    public void AddEndpointHistory(string url, string name = "")
    {
        if (string.IsNullOrEmpty(url))
            return;

        var existing = EndpointHistories.FirstOrDefault(h => h.Url == url);
        if (existing != null)
        {
            existing.LastUsed = DateTime.Now;
            if (!string.IsNullOrEmpty(name))
                existing.Name = name;
        }
        else
        {
            EndpointHistories.Add(new ApiEndpointHistory
            {
                Url = url,
                Name = string.IsNullOrEmpty(name) ? url : name,
                LastUsed = DateTime.Now
            });
        }

        CurrentEndpoint = url;
    }

    // 添加模型历史记录
    public void AddModelHistory(string modelId, string name = "")
    {
        if (string.IsNullOrEmpty(modelId))
            return;

        var existing = ModelHistories.FirstOrDefault(h => h.ModelId == modelId);
        if (existing != null)
        {
            existing.LastUsed = DateTime.Now;
            if (!string.IsNullOrEmpty(name))
                existing.Name = name;
        }
        else
        {
            ModelHistories.Add(new ApiModelHistory
            {
                ModelId = modelId,
                Name = string.IsNullOrEmpty(name) ? modelId : name,
                LastUsed = DateTime.Now
            });
        }

        CurrentModel = modelId;
    }

    // 删除API密钥历史记录
    public void RemoveApiKeyHistory(ApiKeyHistory history)
    {
        ApiKeyHistories.Remove(history);
    }

    // 删除API端点历史记录
    public void RemoveEndpointHistory(ApiEndpointHistory history)
    {
        EndpointHistories.Remove(history);
    }

    // 删除模型历史记录
    public void RemoveModelHistory(ApiModelHistory history)
    {
        ModelHistories.Remove(history);
    }
}