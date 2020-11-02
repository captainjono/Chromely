using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chromely
{
    public static class LoggerBetter
    {
        public static Action<string> OnInformation = e => ToConsole(e, ConsoleColor.White);
        public static Action<string> OnDebug = e => ToConsole(e, ConsoleColor.Gray);
        public static Action<string, Exception> OnError = (e, ex) => ToConsole(e, ConsoleColor.Red);
        public static string LogId { get; set; }

        static LoggerBetter()
        {
            LoggerBetter.WriteToDebug();
        }

        public static bool IsNullOrWhitespace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
        public static Action<string, ConsoleColor> ToConsole = (msg, col) =>    
        {
            var curCol = Console.ForegroundColor;
            Console.ForegroundColor = col;
            var filteredMsg = FilterBlackListedItems(msg);
            Console.WriteLine(filteredMsg);
            Console.ForegroundColor = curCol;
        };

        public const string RedactedString = "Redacted";
        private static readonly Regex[] BlacklistPatterns = new[]
        {
            new Regex("\"userFullName\"\\s*:\\s*\"[^\"]*\"", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline)
        };

        public static string FilterBlackListedItems(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                return msg;
            }

            // If we add more blacklisted this will need to be updated, but its fine for one.
            var filteredMsg = msg;
            foreach (var pattern in BlacklistPatterns)
            {
                filteredMsg = pattern.Replace(filteredMsg, RedactedString);
            }
            return filteredMsg;
        }

        public static string LogInfo(this string toLog)
        {
            OnInformation(WithThreadName($"[INF] {toLog}"));
            return toLog;
        }

        public static string LogDebug(this string toLog)
        {
            OnDebug(WithThreadName($"[DBG] {toLog}"));
            return toLog;
        }

        public static string LogError(this string toLog, Exception e = null)
        {
            var error = $"[ERR] {toLog}: {e?.GetBaseException().ToString() ?? "(No error info)"}";
            OnError(WithThreadName(error), e);
            return toLog;
        }
        private static string WithThreadName(string s)
        {
            return $"[{DateTime.Now:HH:mm:ss.fff}]{(LogId.IsNullOrWhitespace() ? "" : $"[{LogId}]")}[{(Thread.CurrentThread.Name.IsNullOrWhiteSpace() ? Thread.CurrentThread.ManagedThreadId.ToString() : $"{Thread.CurrentThread.ManagedThreadId}][{Thread.CurrentThread.Name}")}] {s}";
        }

        /// <summary>
        /// Sets up the logger to write to a specific file for Errors and Information
        /// </summary>
        /// <param name="filename"></param>
        public static void WriteToFile(string filename)
        {
            StreamWriter logFile = null;
            StreamWriter logFileHtml = null;


            OneThreadAtATime(() =>
            {
                // Make sure the Director exists before trying to create the file, We can not assume it is there.
                // Note the stream is opened by the main thread and left open for the entire life of the application
                logFile = CreateOrOpenReadLogFile(filename);

                var existingError = OnError;
                var existingInfo = OnInformation;

                OnError = (e, ex) =>
                {
                    OneThreadAtATime(() =>
                    {
                        existingError(e, ex);
                        WriteLogs(e, logFile);
                    }, _writeLock);
                };

                OnDebug = (e) =>
                {
                    OneThreadAtATime(() =>
                    {
                        existingInfo(e);
                        WriteLogs(e, logFile);

                    }, _writeLock);
                };

                OnInformation = e =>
                {
                    OneThreadAtATime(() =>
                    {
                        existingInfo(e);
                        WriteLogs(e, logFile);
                    }, _writeLock);
                };
            }, _writeLock);
        }

        private static StreamWriter CreateOrOpenReadLogFile(string filename)
        {
            CreateLogDirectoryIfNotExists(filename);
            return new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        }

        private static void CreateLogDirectoryIfNotExists(string filename)
        {
            var f = new FileInfo(filename);

            if (f.DirectoryName != null && !Directory.Exists(f.DirectoryName))
            {
                var di = Directory.CreateDirectory(f.DirectoryName);
                if (!di.Exists)
                {
                    throw new Exception($"There was a problem trying to create DIR({di.FullName}) for logging");
                }
            }
        }

        private static string GetTruncatedLogMsg(string msg)
        {
            //We dont want to log all the data, this will cause a huge burden on Replay devices if we're logging all encrypted and backup data
            //that is transmitted between Replay and Remote. In the case of information level logs, it should be sufficient to log the first part of the message
            //and indicate it has been truncated.
            return msg.Length > 5000 ? $"Truncated (original length: {msg.Length}): {msg.Substring(0, 250)}...{msg.Substring(msg.Length - 250, 250)}" : msg; ;
        }

        private static void WriteLogs(string msg, StreamWriter logFile)
        {
            var truncatedMsg = GetTruncatedLogMsg(msg);
            var filteredMsg = FilterBlackListedItems(truncatedMsg);
            logFile.WriteLine(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes($"[{DateTime.Now.ToString("s")}] {filteredMsg}")));
            logFile.Flush();
        }

        private static void WriteHtmlLogs(string msg, bool isError, StreamWriter logFileHtml)
        {
            var truncatedMsg = GetTruncatedLogMsg(msg);
            var filteredMsg = FilterBlackListedItems(truncatedMsg);
            logFileHtml.WriteLine($"<div class=\"{(isError ? "err" : "inf")}\"><span class=\"time\">[{DateTimeOffset.Now.ToString("s")}]</span> {filteredMsg}</div>");
            logFileHtml.Flush();
        }

        private static object _writeLock = new object();
        private static string _logId;

        public static void WriteToDebug()
        {
            OnInformation = info => OneThreadAtATime(() => Debug.WriteLine(info), _writeLock);
            OnDebug = info => OneThreadAtATime(() => Debug.WriteLine(info), _writeLock);
            OnError = (e, ex) => OneThreadAtATime(() => Debug.WriteLine(e), _writeLock);
        }

        public static void OneThreadAtATime(Action toRun, object locker)
        {
            lock (locker)
            {
                try
                {
                    toRun();
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"LOG FAILED: {e}");
                }
            }
        }

        public static string LogInfo(this string s, object id)
        {
            return $"[{NameOf(id)}] {s}".LogInfo();
        }

        public static string LogError(this string s, object id, Exception e)
        {
            return $"[{NameOf(id)}] {s}".LogError(e);
        }

        private static string NameOf(object id)
        {
            return id is string ? (string)id : (id?.GetType().Name ?? "null");
        }

        public static string LogDebug(this string s, object id)
        {
            return $"[{NameOf(id)}] {s}".LogInfo();
        }

        public static string LogRaw(this string s, string id = null)
        {
            OnInformation($"{id ?? ""}{s}");

            return s;
        }

        public static void SpinFor(this TimeSpan toSpin)
        {
            var start = DateTime.Now;
            while (DateTime.Now - start  < toSpin)
            {
                Thread.SpinWait(20);
                GC.Collect(2);
            }
        }
    }
}