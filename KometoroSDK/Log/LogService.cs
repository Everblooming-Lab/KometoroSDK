using System.Runtime.CompilerServices;

namespace EverbloomingLab.KometoroSDK.Log
{
    public interface ILogService
    {
        void Print(object message, string messageType = LogControl.DEBUG,
                   [CallerMemberName] string memberName = LogControl.UNKNOWN,
                   [CallerFilePath] string filePath = LogControl.UNKNOWN,
                   [CallerLineNumber] int lineNumber = -1);

        void Print(object message, ELogMessagePresetType messageType = ELogMessagePresetType.Debug,
                   [CallerMemberName] string memberName = LogControl.UNKNOWN,
                   [CallerFilePath] string filePath = LogControl.UNKNOWN,
                   [CallerLineNumber] int lineNumber = -1);
    }

    public class LogServiceAdapter : ILogService
    {
        public void Print(object message, string messageType = LogControl.DEBUG,
                          [CallerMemberName] string memberName = LogControl.UNKNOWN,
                          [CallerFilePath] string filePath = LogControl.UNKNOWN,
                          [CallerLineNumber] int lineNumber = -1)
            => LogControl.Print(message, messageType, memberName, filePath, lineNumber);

        public void Print(object message, ELogMessagePresetType messageType = ELogMessagePresetType.Debug,
                          [CallerMemberName] string memberName = LogControl.UNKNOWN,
                          [CallerFilePath] string filePath = LogControl.UNKNOWN,
                          [CallerLineNumber] int lineNumber = -1)
            => LogControl.Print(message, messageType, memberName, filePath, lineNumber);
    }
}