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
            // PerMonitorV2 ensures consistent DPI scaling across different monitors
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set default font for consistency across all forms
            Application.SetDefaultFont(new System.Drawing.Font("Segoe UI", 9F));

            Application.Run(new LoginForm());
        }
    }
}


