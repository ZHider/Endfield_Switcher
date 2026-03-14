using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Endfield_Switcher
{
    public static class Util
    {
        public static bool IsGameRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public static bool IsValidGamePath(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public static void ShowError(string message)
        {
            HandyControl.Controls.MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowWarning(string message)
        {
            HandyControl.Controls.MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static MessageBoxResult ChooseQuestion(string message)
        {
            return HandyControl.Controls.MessageBox.Show(
                message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Information);
        }

        public static MessageBoxResult ChooseWarning(string message)
        {
            return HandyControl.Controls.MessageBox.Show(
                message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }
    }
}
