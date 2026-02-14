using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation; 

namespace Endfield_Switcher
{
    /// <summary>
    /// InputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : HandyControl.Controls.Window
    {
        public string InputText { get; private set; }
        public InputDialog(string title = "请输入备注", string message = "给数据起个别名吧：", string defaultText = "")
        {
            InitializeComponent();
            this.Title = title;
            LblMessage.Text = message;
            TxtInput.Text = defaultText;
            if (!string.IsNullOrEmpty(defaultText))
            {
                TxtInput.SelectAll();
            }

            TxtInput.Focus();

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
          if(string.IsNullOrWhiteSpace(TxtInput.Text))
            {
                HandyControl.Controls.MessageBox.Show("输入不能为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            InputText = TxtInput.Text;
            DialogResult = true;
            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true // .NET 6/8 必须加这句，否则打不开网页
            });

            // 标记事件已处理，防止继续冒泡
            e.Handled = true;
        }




    }

  


}

