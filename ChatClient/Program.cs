using System;
using System.Windows.Forms;
using ChatClient.Forms;

namespace ChatClient
{
    internal static class Program
    {
        /// <summary>
        /// Điểm vào của WinForms client.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new LoginForm());
        }
    }
}


