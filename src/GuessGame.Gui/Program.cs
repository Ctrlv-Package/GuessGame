using System;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Global error handling logs
            Application.ThreadException += (s, e) =>
                MessageBox.Show("Unhandled Thread Exception: " + e.Exception.Message);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show("Unhandled Domain Exception: " + ((Exception)e.ExceptionObject).Message);

            MessageBox.Show("Main started");  // ✅ Startup checkpoint

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
