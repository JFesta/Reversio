using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Logging
{
    public interface ILogger
    {
        void Verbose(string message);
        void Debug(string message);
        void Information(string message);
        void Warning(string message);
        void Error(string message);
        void Error(Exception exception);
        void Fatal(string message);
        void Fatal(Exception exception);
    }
}
