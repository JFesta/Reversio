using Reversio.Core.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Console = Colorful.Console;
using System.Drawing;

namespace Reversio.Cli
{
    public class ConsoleLogger : ILogger
    {
        private int _minLevel;
        private bool _fullExceptionLog;

        public ConsoleLogger(int minLevel, bool fullExceptionLog)
        {
            _minLevel = minLevel;
            _fullExceptionLog = fullExceptionLog;
        }

        public void Verbose(string message)
        {
            if (_minLevel <= 1)
                Console.WriteLine(message);
        }

        public void Debug(string message)
        {
            if (_minLevel <= 2)
                Console.WriteLine(message);
        }

        public void Information(string message)
        {
            if (_minLevel <= 3)
                Console.WriteLine(message);
        }

        public void Warning(string message)
        {
            if (_minLevel <= 4)
                Console.WriteLine(message, Color.Yellow);
        }

        public void Error(string message)
        {
            if (_minLevel <= 5)
                Console.WriteLine(message, Color.Red);
        }

        public void Error(Exception exception)
        {
            if (_minLevel <= 5)
                Console.WriteLine(_fullExceptionLog ? exception.ToString() : exception.Message, Color.Red);
        }

        public void Fatal(string message)
        {
            if (_minLevel <= 6)
                Console.WriteLine(message, Color.Red);
        }

        public void Fatal(Exception exception)
        {
            if (_minLevel <= 6)
                Console.WriteLine(_fullExceptionLog ? exception.ToString() : exception.Message, Color.Red);
        }

        private void WriteLineBackground(string message, Color fg)
        {
            Console.BackgroundColor = fg;
            Console.WriteLine(message);
            Console.BackgroundColor = Color.Transparent;
        }
    }
}
