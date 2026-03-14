using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;


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
            if (string.IsNullOrEmpty(gamePath))
            {
                // 尝试自动查找游戏位置
                gamePath = GameLocator.TryFindGameExePath();
                if (!string.IsNullOrEmpty(gamePath))
                {
                    Properties.Settings.Default.GameExePath = gamePath;
                    Properties.Settings.Default.Save();
                }
            }
            if (!string.IsNullOrEmpty(gamePath))
                TxtGamePath.Text = gamePath;

        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = AccountList.SelectedItem as AccountInfo;

            // 情况 1：已选中项目 → 询问是否覆盖
            if (selectedItem != null)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "您已经选择了一个项目，要替换它吗？", "确认",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Asterisk,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                {
                    ReplaceExistingAccount(selectedItem);
                    return; // 替换完成，无需新建
                }
                // 如果是 No，则继续执行“新建”逻辑（等效于未选中）
            }

            // 情况 2：未选中 或 用户选择“No” → 新建备份
            string note = ShowInputDialog();
            if (note != null)
                CreateNewBackup(note);


            void ReplaceExistingAccount(AccountInfo account)
            {
                try
                {
                    _backupManager.BackupAccount(account.DisplayName);
                    _backupManager.DeleteAccount(account);
                    RefreshList();
                    CheckCurrentStatus();
                }
                catch (Exception ex)
                {
                    ShowError($"保存失败：{ex.Message}");
                }
            }

            void CreateNewBackup(string _note)
            {
                try
                {
                    _backupManager.BackupAccount(_note);
                    RefreshList();
                    CheckCurrentStatus();
                }
                catch (Exception ex)
                {
                    ShowError($"保存失败：{ex.Message}");
                }
            }

            string ShowInputDialog()
            {
                var dialog = new InputDialog();
                return dialog.ShowDialog() == true ? dialog.InputText : null;
            }

            void ShowError(string message)
            {
                HandyControl.Controls.MessageBox.Show(
                    message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            // 1. 验证前置条件
            if (Util.IsGameRunning("Endfield"))
            {
                Util.ShowError("请先关闭游戏再进行切换操作！");
                return;
            }

            var selected = AccountList.SelectedItem as AccountInfo;
            if (selected == null)
            {
                Util.ShowError("请先选择一个账户！");
                return;
            }

            // 2. 执行切换
            try
            {
                _backupManager.SwitchAccount(selected);
            }
            catch (Exception ex)
            {
                Util.ShowError($"切换失败：{ex.Message}");
                return;
            }

            // 3. 切换成功后询问是否启动游戏
            var result = Util.ChooseQuestion("切换成功！是否立即启动游戏？");
            if (result != MessageBoxResult.Yes)
            {
                CheckCurrentStatus();
                return;
            }

            // 4. 启动游戏（需有效路径）
            string gamePath = TxtGamePath.Text;
            if (!Util.IsValidGamePath(gamePath))
            {
                Util.ShowWarning("你的游戏路径尚未设置，请先设置游戏路径。");
                CheckCurrentStatus();
                return;
            }

            try
            {
                Process.Start(gamePath);
                CheckCurrentStatus();
            }
            catch (Exception ex)
            {
                Util.ShowError($"无法启动游戏：{ex.Message}");
                CheckCurrentStatus();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = AccountList.SelectedItem as AccountInfo;
            if (selected != null
                && Util.ChooseWarning("确定要删除该账户备份吗？") == MessageBoxResult.Yes)
            {
                try
                {
                    _backupManager.DeleteAccount(selected);
                    RefreshList();
                    CheckCurrentStatus();
                }
                catch (Exception ex)
                {
                    Util.ShowError($"删除失败：{ex.Message}");
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

        public void Unselected_Click(object sender, RoutedEventArgs e)
        {

            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is AccountInfo account)
            {
                AccountList.SelectedItem = null;
            }
        }

    }
}





