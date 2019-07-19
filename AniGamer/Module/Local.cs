using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Module
{
    public class Local
    {

        #region 專案資訊

        public static Decimal Ver = 1.0M; //目前系統版本
        public static String ProgramName = "";  //程式名稱    

        #endregion

        #region 設定值

        public static String AniDir = AppDomain.CurrentDomain.BaseDirectory + "Ani";

        public static String Quality = "720";

        public static String TmpDir = AppDomain.CurrentDomain.BaseDirectory + "Tmp";

        public static String SetupFile = AppDomain.CurrentDomain.BaseDirectory + "Setup.xml";

        public static String ListFile = AppDomain.CurrentDomain.BaseDirectory + "DownloadList.xml";

        public static String ProxyIP = "";

        public static Int32 ProxyPort = 0;

        public static String ProxyUser = "";

        public static String ProxyPass = "";

        #endregion


        #region 視窗

        public static WPF.WPF_MainForm MainForm;

        #endregion

        #region 運算資訊

        [DllImport("uxtheme.dll", EntryPoint = "#95")]
        public static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);
        [DllImport("uxtheme.dll", EntryPoint = "#96")]
        public static extern uint GetImmersiveColorTypeFromName(IntPtr pName);
        [DllImport("uxtheme.dll", EntryPoint = "#98")]
        public static extern int GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        public static Color GetThemeColor(string name)  // 取得Windows主題色
        {
            var colorSetEx = GetImmersiveColorFromColorSetEx((uint)GetImmersiveUserColorSetPreference(false, false),
                GetImmersiveColorTypeFromName(Marshal.StringToHGlobalUni(name)),
            false, 0);
            var colour = Color.FromArgb((byte)((0xFF000000 & colorSetEx) >> 24), (byte)(0x000000FF & colorSetEx), (byte)((0x0000FF00 & colorSetEx) >> 8), (byte)((0x00FF0000 & colorSetEx) >> 16));
            return colour;
        }

        public static string RandomString(int length)
        {
            var str = "abcdefghijklmnopqrstuvwxyz0123456789";
            var next = new Random(DateTime.UtcNow.Millisecond);
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }

        #endregion

    }
}
