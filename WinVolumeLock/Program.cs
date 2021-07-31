using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace WinVolumeLock
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "uac.login")
                {
                    return 0;
                }
            }

            Application.EnableVisualStyles();
            Application.Run(new MainApplicationContext());

            return 0;
        }

        public static bool GetUacPrivs()
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                    Arguments = "uac.login",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };

            try
            {
                p.Start();
                p.WaitForExit();

                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
