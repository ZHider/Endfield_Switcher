using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace Endfield_Switcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly BackupManager _backupManager = new BackupManager();
        public MainWindow()
        {
            InitializeComponent();
            RefreshList();
            LoadGamePath();
            CheckCurrentStatus();
        }

        private void RefreshList()
        {//刷新列表
            AccountList.ItemsSource = null;
            AccountList.ItemsSource = _backupManager.Accounts;
        }

        private void CheckCurrentStatus()
        {//检查当前游戏登录状态并更新状态文本
            string lastFingerPrint = Properties.Settings.Default.LastAccountHash;
            if (string.IsNullOrEmpty(lastFingerPrint))
            {
                TxtStatus.Text = "尚无操作记录";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
                return;
            }
            var lastAccount = _backupManager.Accounts.FirstOrDefault(a => a.FingerPrint == lastFingerPrint);
            if (lastAccount == null)
            {
                TxtStatus.Text = "记录对应的存档已被删除";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }
            string gameFile = Path.Combine(_backupManager._gameDataPath, "login_cache");
            string currentHash = _backupManager.ComputeFileHash(gameFile);

            if (currentHash == lastFingerPrint)
            {

                TxtStatus.Text = $"当前就绪：{lastAccount.DisplayName}";
                TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {

                TxtStatus.Text = $"上次加载：{lastAccount.DisplayName} (会话已变动)";
                TxtStatus.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
            }

        }
        private void LoadGamePath()
        {// 加载游戏路径到文本框
            string gamePath = Properties.Settings.Default.GameExePath;
            if (!string.IsNullOrEmpty(gamePath))
            {
                TxtGamePath.Text = gamePath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string processName = "Endfield";
          
                var Dialog = new InputDialog();
            if (Dialog.ShowDialog() == true)
            {
                string note = Dialog.InputText;
                try
                {
                    _backupManager.BackupAccount(note);
                    RefreshList();
                    CheckCurrentStatus();

                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }


            }
        }


        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            var selected = AccountList.SelectedItem as AccountInfo;
            string processName = "Endfield";
            var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);
            if(runningProcesses.Length > 0)
            {
                HandyControl.Controls.MessageBox.Show("请先关闭游戏再进行切换操作！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (selected == null)
            {
                HandyControl.Controls.MessageBox.Show("请先选择一个账户！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                _backupManager.SwitchAccount(selected);
                var result = HandyControl.Controls.MessageBox.Show("切换成成功是否立即启动游戏？", "成功", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    if (TxtGamePath == null)
                    {
                        HandyControl.Controls.MessageBox.Show("你的游戏路径尚未设置请先设置游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CheckCurrentStatus();
                        return;

                    }
                    System.Diagnostics.Process.Start(TxtGamePath.Text);
                }
                CheckCurrentStatus();

            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"切换失败：{ex.Message}", "错误");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = AccountList.SelectedItem as AccountInfo;
            if (selected == null) return;
            if (HandyControl.Controls.MessageBox.Show("确定要删除该账户备份吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _backupManager.DeleteAccount(selected);
                    RefreshList();
                    CheckCurrentStatus();
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "终末地主程序|Endfield.exe",
                Title = "请找到游戏安装目录"
            };
            if (dialog.ShowDialog() == true)
            {

                TxtGamePath.Text = dialog.FileName;
                Properties.Settings.Default.GameExePath = dialog.FileName;
                Properties.Settings.Default.Save();
            }


        }

        // 方法必须在这个类里面
        public void Rename_Click(object sender, RoutedEventArgs e)
        {
          
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is AccountInfo account)
            {

                var dialog = new InputDialog("修改备注", "请输入新的备注名：", account.DisplayName);
                if (dialog.ShowDialog() == true)
                {
                    account.DisplayName = dialog.InputText;


                    _backupManager.RenameAccount();
                    RefreshList();
                }
            }

        }

    }
}





