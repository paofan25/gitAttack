// 本文件为Unity场景冲突可视化与AI辅助合并工具的一部分
// 设置服务逻辑
using System.IO;
using System.Text.Json;
using gitAttack.Models;

namespace gitAttack.Services
{
    public class SettingsService
    {
        private const string SettingsFileName = "api_settings.json";
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            // 保存在应用程序目录下
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "UnityMergeAI"
            );

            // 确保目录存在
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsFilePath = Path.Combine(appDataPath, SettingsFileName);
        }

        // 加载设置
        public ApiSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ApiSettings>(json);
                    return settings ?? new ApiSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置时出错: {ex.Message}");
            }

            return new ApiSettings();
        }

        // 保存设置
        public bool SaveSettings(ApiSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置时出错: {ex.Message}");
                return false;
            }
        }
    }
}