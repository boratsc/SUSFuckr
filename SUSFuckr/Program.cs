using System;
using System.Windows.Forms;

namespace SUSFuckr
{
    static class Program
    {
        

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static void ShowConsole()
        {
            AllocConsole();
            Console.WriteLine("Debug console opened.");
        }
        [STAThread]
        static void Main()
        {
            //ShowConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}