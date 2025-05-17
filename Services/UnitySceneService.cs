// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// Unity场景服务逻辑
using System.IO;
using gitAttack.Models;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace gitAttack.Services;

public class UnitySceneService
{
    // Unity场景文件是YAML格式
    public bool IsValidUnityScene(string content)
    {
        try
        {
            var input = new StringReader(content);
            var yaml = new YamlStream();
            yaml.Load(input);

            // 验证是否有Unity场景必须的元素
            var rootNode = yaml.Documents[0].RootNode;
            if (rootNode is YamlMappingNode mappingNode)
            {
                // Unity场景文件通常包含这些关键节点
                return mappingNode.Children.ContainsKey("SceneSettings") ||
                       mappingNode.Children.ContainsKey("MonoBehaviour") ||
                       mappingNode.Children.ContainsKey("GameObject");
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, object> ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<Dictionary<string, object>>(content);
    }

    public string GetYamlDiff(string ourContent, string theirContent)
    {
        try
        {
            var ourYaml = ParseYaml(ourContent);
            var theirYaml = ParseYaml(theirContent);

            // 查找差异（这里简化处理，实际可能需要更复杂的比较逻辑）
            var differences = new Dictionary<string, (object? Ours, object? Theirs)>();

            foreach (var key in ourYaml.Keys.Union(theirYaml.Keys))
            {
                var ourValue = ourYaml.ContainsKey(key) ? ourYaml[key] : null;
                var theirValue = theirYaml.ContainsKey(key) ? theirYaml[key] : null;

                if (!Equals(ourValue, theirValue))
                {
                    differences[key] = (ourValue, theirValue);
                }
            }

            // 生成差异描述
            var diffDescription = new System.Text.StringBuilder();
            foreach (var (key, (ourValue, theirValue)) in differences)
            {
                diffDescription.AppendLine($"属性: {key}");
                diffDescription.AppendLine($"- 我们的版本: {ourValue?.ToString() ?? "不存在"}");
                diffDescription.AppendLine($"- 他们的版本: {theirValue?.ToString() ?? "不存在"}");
                diffDescription.AppendLine();
            }

            return diffDescription.ToString();
        }
        catch
        {
            // 无法解析YAML，返回原始文本差异
            return $"我们的版本:\n{ourContent}\n\n他们的版本:\n{theirContent}";
        }
    }

    public void AnalyzeConflict(ConflictSection conflict)
    {
        // 分析冲突内容，生成人类可读的描述
        if (IsValidUnityScene(conflict.OurContent) && IsValidUnityScene(conflict.TheirContent))
        {
            var diffText = GetYamlDiff(conflict.OurContent, conflict.TheirContent);
            conflict.Description = $"Unity场景冲突分析:\n{diffText}";
        }
        else
        {
            conflict.Description = "无法解析为有效的Unity场景数据。";
        }
    }
}