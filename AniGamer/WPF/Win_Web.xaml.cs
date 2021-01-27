using System;
using System.Collections.Generic;
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

namespace WPF
{
    /// <summary>
    /// Win_Web.xaml 的互動邏輯
    /// </summary>
    public partial class Win_Web : Window
    {
        public Win_Web()
        {
            InitializeComponent();

            WB_Main.Navigate("https://user.gamer.com.tw/login.php");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Module.Local.CookiesSTR =  WB_Main.Document.Cookie;
            this.Close();
        }
    }
}
