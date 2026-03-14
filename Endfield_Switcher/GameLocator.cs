using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Endfield_Switcher
{
    public static class GameLocator
    {
        /// <summary>
        /// 尝试通过注册表获得游戏路径
        /// </summary>
        /// <returns>成功返回游戏路径，失败返回 String.Empty</returns>
        public static string TryFindGameExePath()
        {
            string launcherPath = string.Empty;

            // 开始查找 launcherPath
            const string GameEXEName = "Endfield.exe";
            const string LauncherPathKeyName = "install_path";
            const string launcherKeyPath = @"Software\Hypergryph\Launcher";
            using (RegistryKey launcherKey = Registry.CurrentUser.OpenSubKey(launcherKeyPath))
            {
                if (launcherKey == null)
                {
                    Debug.WriteLine("未找到 Hypergryph Launcher 注册表项。");
                    return string.Empty;
                }

                foreach (string subKeyName in launcherKey.GetSubKeyNames())
                {
                    using (RegistryKey subKey = launcherKey.OpenSubKey(subKeyName))
                    {
                        if (subKey != null)
                        {
                            object value = subKey.GetValue(LauncherPathKeyName);
                            if (value is string installPath
                                && !string.IsNullOrWhiteSpace(installPath)
                                && Directory.Exists(installPath))
                                launcherPath = installPath;
                        }
                    } // subKey 自动 Dispose
                }
            } // launcherKey 自动 Dispose

            if (launcherPath == string.Empty)
            {
                Debug.WriteLine(
                    $"未在 {launcherKeyPath} 的任何子项中找到 {LauncherPathKeyName}，或者该文件夹实际不存在。"
                );
                return string.Empty;
            }

            // 确认游戏目录
            string gameExePath = Path.Combine(launcherPath, "games", "Endfield Game", GameEXEName);
            if (!Util.IsValidGamePath(gameExePath))
            {
                Debug.WriteLine("GamePath 查找失败：文件 " + gameExePath + " 不存在。");
                return string.Empty;
            }

            return gameExePath;
        }


        public static string FindGameDataPath()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // 定义多个候选路径（按优先级顺序）
            var candidatePaths = new[]
            {
                Path.Combine(appdata, "AppData", "Local", "Hypergryph", "Endfield"),
                Path.Combine(appdata, "AppData", "LocalLow", "Hypergryph", "Endfield")
            };

            foreach (string path in candidatePaths)
            {
                if (Directory.Exists(path))
                {
                    return FindLoginData(path);
                }
            }

            throw new DirectoryNotFoundException("未找到登录数据目录，请确保游戏已安装并至少运行过一次。");
        }

        public static string FindLoginData(string Paths)
        {
            var target = Directory.EnumerateDirectories(Paths, "sdk*");
            foreach (var dir in target)
            {
                bool loginFile = Directory.EnumerateFiles(dir, "login_cache").Any();
                if (loginFile)
                {
                    return dir;
                }
            }

            return string.Empty;
        }
    }
}