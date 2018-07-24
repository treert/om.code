using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadWebsite
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {


            //// > https://blog.csdn.net/zcalll/article/details/6590879
            //AllocConsole();


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            Application.ApplicationExit += (sender, e) =>
            {
                XLocalSave.singleton.SaveConfig();
            };

            //FreeConsole();
        }
    }
}
