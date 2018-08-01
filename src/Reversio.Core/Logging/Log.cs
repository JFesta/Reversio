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
