using System;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    public class Loggable
    {
        private Object _lock = new object();

        protected void LogMessage(string msg)
        {
            lock (_lock) { Logger.LogMessage(msg); }
        }

        protected void LogMessage(string[] msg)
        {
            lock (_lock) { Logger.LogMessage(msg); }
        }
    }
}
