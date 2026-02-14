using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;


namespace Endfield_Switcher
{
    public class BackupManager
    {
        public readonly string _gameDataPath;//游戏数据路径
        private readonly string _baseDir; //程序运行目录
        private readonly string _backupPath; //备份路径
        private readonly string _indexFilePath;//json索引文件路径

        public List<AccountInfo> Accounts { get; private set; }


        public BackupManager()
        { //这个构造函数的作用是检查路径以及是否有备份的文件夹，如果没有就创建一个，并且检查游戏数据路径是否存在，如果不存在就抛出异常提示用户。
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");//备份路径

            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localdata = Path.Combine(appdata, "AppData", "Local", "Hypergryph", "Endfield");

            if (Directory.Exists(localdata))
            {
                _gameDataPath = FindLoginData(localdata);//游戏数据路径
            }

            else if (Directory.Exists(Path.Combine(appdata, "AppData", "LocalLow", "Hypergryph", "Endfield")))
            {
                _gameDataPath = FindLoginData(Path.Combine(appdata, "AppData", "LocalLow", "Hypergryph", "Endfield"));//游戏数据路径
            }
            if (_gameDataPath == string.Empty)
            {
                throw new DirectoryNotFoundException("未找到登录数据目录，请确保游戏已安装并至少运行过一次。");
            }

            _baseDir = AppDomain.CurrentDomain.BaseDirectory;

            _indexFilePath = Path.Combine(_baseDir, "accounts.json");
            LoadAccount();
        }

        public void LoadAccount()
        {
            if (File.Exists(_indexFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_indexFilePath);
                    Accounts = JsonConvert.DeserializeObject<List<AccountInfo>>(json) ?? new List<AccountInfo>(); ;

                }
                catch
                {//如果读取或解析JSON文件失败，可能是因为文件损坏或格式不正确。在这种情况下，我们可以选择创建一个新的空列表来存储账户信息，以确保程序能够继续运行。
                    Accounts = new List<AccountInfo>();
                }
            }
            else
            {
                Accounts = new List<AccountInfo>();
                SaveJson();

            }


        }

        public void SaveJson()
        {
            string json = JsonConvert.SerializeObject(Accounts, Formatting.Indented);
            File.WriteAllText(_indexFilePath, json);
        }

        public string ComputeFileHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

            }


        }

        public void BackupAccount(string note)
        {
            //创建唯一的备份文件夹
            string folderName = Guid.NewGuid().ToString("N");
            string tagetdir = Path.Combine(_backupPath, folderName);
            Directory.CreateDirectory(tagetdir);

            //复制游戏数据到备份文件夹
            string[] files = { "login_cache", "login_cache.crc" };
            string Hash = "";
            foreach (string file in files)
            {
                string sourceFile = Path.Combine(_gameDataPath, file);
                string destFile = Path.Combine(tagetdir, file);
                if (File.Exists(sourceFile))
                {
                    using (var inputStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var outputStream = new FileStream(destFile, FileMode.Create))
                    {
                        inputStream.CopyTo(outputStream);
                    }
                    if (file == "login_cache") Hash = ComputeFileHash(sourceFile);//计算主文件的哈希值
                }
            }

            var newaccount = new AccountInfo
            {
                Id = Guid.NewGuid().ToString(),
                FolderName = folderName,
                DisplayName = note,
                FingerPrint = Hash,
                LastBackupTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Accounts.Insert(0, newaccount); //将新的备份信息插入到列表的开头
            SaveJson();
        }

        private string FindLoginData(string Paths)
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

        public void SwitchAccount(AccountInfo account)
        {
            string sourceDir = Path.Combine(_backupPath, account.FolderName);
            if (!Directory.Exists(sourceDir))
            {
                throw new FileNotFoundException("备份文件夹找不到了！请尝试删除此记录。");
            }
            //清理掉当前游戏数据文件夹中的登录数据
            string[] files = { "login_cache", "login_cache.crc" };
            foreach (var file in files)
            {
                string gameFile = Path.Combine(_gameDataPath, file);
                if (File.Exists(gameFile))
                {
                    File.Delete(gameFile);
                }
            }

            //复制备份数据到游戏数据文件夹
            foreach (var file in files)
            {
                string src = Path.Combine(sourceDir, file);
                string dest = Path.Combine(_gameDataPath, file);
                if (File.Exists(src))
                {
                    File.Copy(src, dest, true);
                }
            }
            //把当前账户的指纹保存到设置中，以便下次启动时识别当前账户
            string targetFile = Path.Combine(_gameDataPath, "login_cache");
            string snapshotHash = ComputeFileHash(targetFile);
            Properties.Settings.Default.LastAccountHash = snapshotHash;
            Properties.Settings.Default.Save();

        }
        public void DeleteAccount(AccountInfo account)
        {
            string dir = Path.Combine(_backupPath, account.FolderName);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Accounts.Remove(account);
            SaveJson();

        }
        public void RenameAccount()
        { //不行暴露这个方法
            SaveJson();
        }

    }
}