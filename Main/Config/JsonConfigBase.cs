using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Timers;

namespace FluorescenceFullAutomatic.Config
{
    /// <summary>
    /// JSON格式配置基类，实现配置的核心功能
    /// </summary>
    public abstract class JsonConfigBase
    {
        private readonly string _fileName;
        private readonly string _filePath;
        private Timer _saveTimer;
        private bool _isDirty = false;
        private readonly object _saveLock = new object();

        /// <summary>
        /// 初始化配置基类
        /// </summary>
        /// <param name="fileName">配置文件名，如果为null则使用类名.json</param>
        protected JsonConfigBase(string fileName = null)
        {
            _fileName = fileName ?? GetType().Name + ".json";
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileName);

            // 初始化保存定时器，延迟500毫秒后保存
            _saveTimer = new Timer(500);
            _saveTimer.Elapsed += SaveTimer_Elapsed;
            _saveTimer.AutoReset = false;
            _saveTimer.Enabled = false;

            LoadConfig();
        }

        /// <summary>
        /// 标记配置已修改，需要保存
        /// </summary>
        protected void MarkDirty()
        {
            _isDirty = true;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        /// <summary>
        /// 定时器触发时保存配置
        /// </summary>
        private void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDirty)
            {
                Save();
                _isDirty = false;
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    // 文件存在，读取配置
                    string json = File.ReadAllText(_filePath);
                    JsonConvert.PopulateObject(json, this);
                }
                else
                {
                    // 文件不存在，创建默认配置
                    SaveDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
                // 出错时使用默认值
                SaveDefaultConfig();
            }
        }

        /// <summary>
        /// 保存默认配置到JSON文件
        /// </summary>
        private void SaveDefaultConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存默认配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存当前配置到JSON文件
        /// </summary>
        public void Save()
        {
            lock (_saveLock)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"保存配置失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新配置（从文件重新加载）
        /// </summary>
        public void Refresh()
        {
            LoadConfig();
        }
    }
}
