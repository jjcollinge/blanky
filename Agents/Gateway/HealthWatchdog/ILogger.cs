using System;
using System.Runtime.CompilerServices;

namespace HealthWatchdog
{
    public interface ILogger
    {
        void LogError(string info, Exception ex = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0);
        void LogInformation(string info, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0);
        void LogTimedEvent(double timeInMs, string name, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceLineNum = 0);
    }
}