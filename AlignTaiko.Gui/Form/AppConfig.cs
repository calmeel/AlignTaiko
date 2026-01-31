using System.Text.Json;

namespace AlignTaiko.Gui
{
    internal sealed class AppConfig
    {
        public string UiCulture { get; set; } = "en-US";
        public bool BackupEnabled { get; set; } = true;

        private static string ConfigDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlignTaiko");

        private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

        public static AppConfig LoadOrDefault()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static string LoadCultureNameOrDefault()
            => string.IsNullOrWhiteSpace(LoadOrDefault().UiCulture) ? "en-US" : LoadOrDefault().UiCulture;

        public static bool LoadBackupEnabledOrDefault()
            => LoadOrDefault().BackupEnabled;

        public static void SaveCulture(string cultureName)
        {
            var cfg = LoadOrDefault();
            cfg.UiCulture = cultureName;
            Save(cfg);
        }

        public static void SaveBackupEnabled(bool enabled)
        {
            var cfg = LoadOrDefault();
            cfg.BackupEnabled = enabled;
            Save(cfg);
        }

        private static void Save(AppConfig cfg)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // ignore
            }
        }
    }
}
