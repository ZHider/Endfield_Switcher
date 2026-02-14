using System;

namespace Endfield_Switcher
{
    public class AccountInfo
    {
        // 唯一ID
        public string Id { get; set; }
        //文件夹名称
        public string FolderName { get; set; }
        //备注
        public string DisplayName { get; set; }
        // 指纹
        public string FingerPrint { get; set; }
        //备份时间
        public string LastBackupTime { get; set; }
      
    }
}