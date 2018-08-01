using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Logging
{
    public static class Log
    {
        public static ILogger Logger { get; set; }
        
        public static void Verbose(string message)
        {
            Logger.Verbose(message);
        }

        public static void Debug(string message)
        {
            Logger.Debug(message);
        }

        public static void Information(string message)
        {
            Logger.Information(message);
        }

        public static void Warning(string message)
        {
            Logger.Warning(message);
        }

        public static void Error(string message)
        {
            Logger.Error(message);
        }

        public static void Error(Exception exception)
        {
            Logger.Error(exception);
        }

        public static void Fatal(string message)
        {
            Logger.Fatal(message);
        }

        public static void Fatal(Exception exception)
        {
            Logger.Fatal(exception);
        }
    }
}
