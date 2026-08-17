using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace TommyPOS.Utils
{
    public class AppSettings
    {
        public int FormWidth { get; set; } = 1400;
        public int FormHeight { get; set; } = 820;
        public bool IsMaximized { get; set; } = false;
    }

    public static class FormConfig
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        if (settings.FormWidth < 1000) settings.FormWidth = 1000;
                        if (settings.FormHeight < 650) settings.FormHeight = 650;
                        return settings;
                    }
                }
            }
            catch { }

            return new AppSettings();
        }

        public static void SaveSettings(int width, int height, bool isMaximized)
        {
            try
            {
                var settings = new AppSettings
                {
                    FormWidth = width < 1000 ? 1000 : width,
                    FormHeight = height < 650 ? 650 : height,
                    IsMaximized = isMaximized
                };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public static void ApplySettingsToForm(Form form)
        {
            var settings = LoadSettings();
            if (settings.IsMaximized)
            {
                form.WindowState = FormWindowState.Maximized;
            }
            else
            {
                form.WindowState = FormWindowState.Normal;
                form.Size = new Size(settings.FormWidth, settings.FormHeight);
            }
        }
    }
}
