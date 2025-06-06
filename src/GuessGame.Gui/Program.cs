using System;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // .NET 6/7/8
            Application.Run(new MainForm());       // THIS must be called
        }
    }
}
