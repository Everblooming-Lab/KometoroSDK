using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EverbloomingLab.KometoroSDK.Log
{
    public static class LogControl
    {
        internal const string UNKNOWN = "unknown";
        internal const string NULL = "null";
        internal const string DEBUG = "Debug";
        internal const string KMTR = "KMTR_log";

        public static bool _IsStarted { get; private set; }

        public static string? _LogFilePath { get; private set; }

        public static ELogMode _LogMode { get; private set; } = ELogMode.Normal;

        public static event Action<string>? _OnLogConsole;
        private static FileStream? _fileStream;
        private static StreamWriter? _writer;

        private static long _logSerialNumber;

        private static BlockingCollection<string>? _logQueue;
        private static Task? _writeTask;
        private static CancellationTokenSource? _cancellationTokenSource;

        public static void Print(object? message, LogTag messageType = default(LogTag),
                                 [CallerMemberName] string memberName = UNKNOWN,
                                 [CallerFilePath] string filePath = UNKNOWN,
                                 [CallerLineNumber] int lineNumber = -1)
        {
            if (!_IsStarted) return;

            if (filePath != UNKNOWN) filePath = Path.GetFileName(filePath);

            if (message is IEnumerable enumerable && !(message is string))
            {
                foreach (var item in enumerable)
                {
                    Print(item, messageType, memberName, filePath, lineNumber);
                }

                return;
            }

            var msgStr = message switch
            {
                ILogOut obj => obj.LogOut(),
                null => NULL,
                _ => message.ToString(),
            };

            PrintDispatch(msgStr!, messageType.ToString(), memberName, filePath, lineNumber);
        }

        public static void Print(string message, LogTag messageType = default(LogTag),
                                 [CallerMemberName] string memberName = UNKNOWN,
                                 [CallerFilePath] string filePath = UNKNOWN,
                                 [CallerLineNumber] int lineNumber = -1)
        {
            if (!_IsStarted) return;
            if (filePath != UNKNOWN) filePath = Path.GetFileName(filePath);

            PrintDispatch(message, messageType.ToString(), memberName, filePath, lineNumber);
        }

        public static void Print<T>(T message, LogTag messageType = default(LogTag),
                                    [CallerMemberName] string memberName = UNKNOWN,
                                    [CallerFilePath] string filePath = UNKNOWN,
                                    [CallerLineNumber] int lineNumber = -1) where T : struct
        {
            if (!_IsStarted) return;
            if (filePath != UNKNOWN) filePath = Path.GetFileName(filePath);

            var msgStr = message switch
            {
                ILogOut obj => obj.LogOut(),
                _ => message.ToString(),
            };

            PrintDispatch(msgStr!, messageType.ToString(), memberName, filePath, lineNumber);
        }

        public static void Print<T>(Func<T> messageFactory, LogTag messageType = default(LogTag),
                                    [CallerMemberName] string memberName = UNKNOWN,
                                    [CallerFilePath] string filePath = UNKNOWN,
                                    [CallerLineNumber] int lineNumber = -1)
        {
            if (!_IsStarted) return;
            if (filePath != UNKNOWN) filePath = Path.GetFileName(filePath);
            Print(messageFactory.Invoke(), messageType, memberName, filePath, lineNumber);
        }

        public static void Start(string logFilePath, bool crashMode = false) => Start(logFilePath, KMTR, ELogMode.Normal, null, crashMode);

        public static void Start(string logFilePath, string fileName, Action<string>? onLogConsole = null, bool crashMode = false) =>
            Start(logFilePath, fileName, ELogMode.Normal, onLogConsole, crashMode);

        public static void Start(string logFilePath, string fileName, ELogMode mode, Action<string>? onLogConsole, bool crashMode)
        {
            if (_IsStarted) return;
            _LogFilePath = Path.Combine(logFilePath, $"{fileName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log");
            _LogMode = mode;

            if (mode != ELogMode.Silence) _OnLogConsole += onLogConsole;

            if (mode != ELogMode.ConsoleOnly && mode != ELogMode.RawPassOnly)
            {
                _fileStream = new FileStream(_LogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(_fileStream, Encoding.UTF8);
                _writer.AutoFlush = crashMode;
                _logQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
                _cancellationTokenSource = new CancellationTokenSource();
                _writeTask = Task.Factory.StartNew(WriteLogFileTask, TaskCreationOptions.LongRunning);
            }

            _IsStarted = true;
        }

        public static void Start(Action<string>? onLogConsole)
        {
            if (_IsStarted) return;

            _OnLogConsole += onLogConsole;
            _LogMode = ELogMode.ConsoleOnly;
            _IsStarted = true;

        }

        public static void Stop()
        {
            if (!_IsStarted) return;
            _IsStarted = false;
            _logQueue?.CompleteAdding();

            try
            {
                _writeTask?.Wait(300);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine($"[CRITICAL] LogControl IO Error: {e.Message}");
#if UNITY_EDITOR || UNITY_STANDALONE
    UnityEngine.Debug.LogError($"[CRITICAL] LogControl IO Error: {e.Message}");
#endif
                // 清空当前队列，防止内存暴涨
                while (_logQueue.TryTake(out _)) { }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[ERROR] LogControl Error: {e.Message}");
#if UNITY_EDITOR || UNITY_STANDALONE
    UnityEngine.Debug.LogError($"[ERROR] LogControl Error: {e.Message}");
#endif
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _fileStream = null;
            _logQueue?.Dispose();
            _logQueue = null;

            _OnLogConsole = null;
            Interlocked.Exchange(ref _logSerialNumber, 0);
        }

        private static string GetConsoleFormat(string message, string messageType, string memberName, string filePath, int lineNum, long logNum)
            => $"{messageType}: {message}\n"
             + $"{logNum:0000},{memberName},{filePath},{lineNum}";

        private static string GetLogFileFormat(string message, string messageType, string memberName, string filePath, int lineNum, long logNum)
            => $"{messageType}: {message} | {logNum}, {DateTime.Now:HH:mm:ss.fff} | {memberName},{filePath},{lineNum}";

        private static void PrintToFile(string msg)
        {
            if (_logQueue is null || _logQueue.IsAddingCompleted) return;
            try
            {
                _logQueue.Add(msg);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[ERROR] LogControl Error: {e.Message}");
#if UNITY_EDITOR || UNITY_STANDALONE
    UnityEngine.Debug.LogError($"[ERROR] LogControl Error: {e.Message}");
#endif
            }
        }

        private static void PrintDispatch(string msg, string msgType, string memName, string filePath, int linNum)
        {
            var sn = Interlocked.Increment(ref _logSerialNumber);

            switch (_LogMode)
            {
                case ELogMode.Normal:
                    _OnLogConsole?.Invoke(GetConsoleFormat(msg, msgType, memName, filePath, linNum, sn));
                    PrintToFile(GetLogFileFormat(msg, msgType, memName, filePath, linNum, sn));
                    break;
                case ELogMode.RawPassOnly:
                    _OnLogConsole?.Invoke(msg);
                    break;
                case ELogMode.RawPassAndLog:
                    _OnLogConsole?.Invoke(msg);
                    PrintToFile(GetLogFileFormat(msg, msgType, memName, filePath, linNum, sn));
                    break;
                case ELogMode.Silence:
                    PrintToFile(GetLogFileFormat(msg, msgType, memName, filePath, linNum, sn));
                    break;
                case ELogMode.ConsoleOnly:
                    _OnLogConsole?.Invoke(GetConsoleFormat(msg, msgType, memName, filePath, linNum, sn));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void WriteLogFileTask()
        {
            if (_logQueue is null || _writer is null) return;

            try
            {
                foreach (var log in _logQueue.GetConsumingEnumerable(_cancellationTokenSource!.Token))
                {
                    _writer.WriteLine(log);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException e)
            {
                Console.Error.WriteLine($"[CRITICAL] LogControl IO Error: {e.Message}");
#if UNITY_EDITOR || UNITY_STANDALONE
    UnityEngine.Debug.LogError($"[CRITICAL] LogControl IO Error: {e.Message}");
#endif
                // 清空当前队列，防止内存暴涨
                while (_logQueue.TryTake(out _)) { }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[ERROR] LogControl Error: {e.Message}");
#if UNITY_EDITOR || UNITY_STANDALONE
    UnityEngine.Debug.LogError($"[ERROR] LogControl Error: {e.Message}");
#endif
            }
        }
    }

    /// <summary>
    /// 使用LogControl的输出接口
    /// </summary>
    public interface ILogOut
    {
        /// <summary>
        /// 使用标准化输出接口需要继承的接口，实现该接口的对象，可直接使用LogService进行复杂对象的log输出
        /// </summary>
        /// <returns>返回的字符串将输出到LogService中</returns>
        public string? LogOut();
    }

    public enum ELogMessagePresetType
    {
        Debug,
        Launcher,
        Data,
        Asset,
        Manager,
        Event,
        Ui,
        PlayerControl,
        Sound,
        Animation,
        Other,
        ConsoleDisplay,
        Warning,
        Error,
    }

    public enum ELogMode
    {
        Normal,        // 正常前后台
        ConsoleOnly,   // 只输出到控制台
        RawPassOnly,   // 只输出到控制台，不输出到文件（原生输出）
        RawPassAndLog, // 输出到控制台，并输出到文件（原生输出）
        Silence,       // 不输出到控制台，只输出到文件
    }

    public readonly struct LogTag
    {
        private readonly string? value;
        public LogTag(string value) => this.value = value;
        public static implicit operator LogTag(string val) => new LogTag(val);
        public static implicit operator LogTag(ELogMessagePresetType val) => new LogTag(_log_tags[(int)val]);
        private static readonly string[] _log_tags = Enum.GetNames(typeof(ELogMessagePresetType));
        public override string ToString() => string.IsNullOrEmpty(value) ? LogControl.DEBUG : value;
    }
}