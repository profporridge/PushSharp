using System;
using System.Diagnostics;
using PushSharp.Core;

namespace GlobalPushService
{
    class PushSharpLogger : ILogger
    {
        public PushSharpLogger()
        {
        }

        public void Debug(string format, params object[] objs)
        {
            Trace.WriteLine(String.Format(format,objs));
        }

        public void Info(string format, params object[] objs)
        {
            Trace.WriteLine(String.Format(format, objs));
        }

        public void Warning(string format, params object[] objs)
        {
            Trace.WriteLine(String.Format(format, objs));
        }

        public void Error(string format, params object[] objs)
        {
            Trace.WriteLine(String.Format(format, objs));
            Trace.TraceError(format, objs);
        }
    }
}