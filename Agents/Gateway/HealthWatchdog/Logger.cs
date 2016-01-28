using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HealthWatchdog
{
    class ETWLogger : ILogger
    {
        public void LogError(string info, Exception ex = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0)
        {
            ServiceEventSource.Current.ServiceMessage(info, ex, memberName, filePath, sourceLineNum);
        }

        public void LogInformation(string info, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0)
        {
            ServiceEventSource.Current.ServiceMessage(info, memberName, filePath, sourceLineNum);

        }

        public void LogTimedEvent(double timeInMs, string name, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0)
        {
            ServiceEventSource.Current.ServiceMessage(name, timeInMs, memberName, filePath, sourceLineNum);

        }
    }

    public class TraceLoggingProvider : ILogger
    {
        public bool HasSeenError = false;

        public void LogInformation(string info, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceLineNum = 0)
        {
            if (memberName != string.Empty)
            {
                var callingMemberDetails = string.Format("MemberName: {0}, Line: {1}, FilePath: {2}", memberName, filePath, sourceLineNum);
                Trace.WriteLine(info + callingMemberDetails);
            }
            Trace.WriteLine(info);
        }

        public void LogError(string info, Exception ex = null, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceLineNum = 0)
        {
            HasSeenError = true;
            if (memberName != string.Empty)
            {
                var callingMemberDetails = string.Format("MemberName: {0}, Line: {1}, FilePath: {2}", memberName, filePath, sourceLineNum);
                Trace.WriteLine(info + callingMemberDetails);
            }
            Trace.WriteLine(info);
        }

        public void LogTimedEvent(double timeInMs, string name, [CallerMemberName]string memberName = "", [CallerFilePath]string filePath = "", [CallerLineNumber]int sourceLineNum = 0)
        {
            Trace.WriteLine(name + " took:" + timeInMs);
        }
    }
}
