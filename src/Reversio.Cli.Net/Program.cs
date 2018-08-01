using Reversio.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Reversio.Cli;
using Reversio.Core.Settings;
using Reversio.Core.Utils;

namespace Reversio.CommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            var cli = new CommandLineInterface();
            cli.Execute(args);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press a key to exit...");
                Console.ReadKey();
            }
        }
    }
}
