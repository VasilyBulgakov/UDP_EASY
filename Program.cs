using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;

namespace UDP_EASY
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location));
            if(processes.Length > 1)
            {
                Console.WriteLine("Closing extra processes");
                var curProcess = Process.GetCurrentProcess();
                foreach (Process p in processes)
                {
                    if (p.Id == curProcess.Id) continue;
                    p.Kill();
                }
                Console.WriteLine("Done");
            }

            
            var reader = new UDPReader();

            while (true)
            {
                reader.Update();
                System.Threading.Thread.Sleep(10);
            }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("exit");
        }
    }
}
