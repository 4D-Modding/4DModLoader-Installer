using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _4DModLoader_Installer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // required to do that so that its only a single file instead of 4
            AppDomain.CurrentDomain.AssemblyResolve += (sender, arg) =>
            {
                if (arg.Name.StartsWith("Newtonsoft.Json")) return Assembly.Load(Properties.Resources.Newtonsoft_Json); 
                else if (arg.Name.StartsWith("Microsoft.WindowsAPICodePack.Shell")) return Assembly.Load(Properties.Resources.Microsoft_WindowsAPICodePack_Shell);
                else if (arg.Name.StartsWith("Microsoft.WindowsAPICodePack")) return Assembly.Load(Properties.Resources.Microsoft_WindowsAPICodePack);
                return null;
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Window());
        }
    }
}
