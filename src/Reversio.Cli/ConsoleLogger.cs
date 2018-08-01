/*
 * Copyright 2018 Jacopo Festa 
 * This file is part of Reversio.
 * Reversio is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Reversio is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License
 * along with Reversio. If not, see <http://www.gnu.org/licenses/>.
*/

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
