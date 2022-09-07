using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TrickCore
{
    public class ConsoleLogger : LogTarget
    {
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        public ConsoleLogger()
        {
            DefaultLog += obj => _logQueue.Enqueue($"<{ConsoleColor.Gray}>{GetTime()} - <{CurrentLogger.Tag}> {obj}</{ConsoleColor.Gray}>");
            InfoLog += obj => _logQueue.Enqueue($"<{ConsoleColor.Green}>{GetTime()} - <{CurrentLogger.Tag}> (INFO) {obj}</{ConsoleColor.Green}>");
            WarningLog += obj => _logQueue.Enqueue($"<{ConsoleColor.Yellow}>{GetTime()} - <{CurrentLogger.Tag}> (WARNING) {obj}</{ConsoleColor.Yellow}>");
            ErrorLog += obj => _logQueue.Enqueue($"<{ConsoleColor.Red}>{GetTime()} - <{CurrentLogger.Tag}> (ERROR) {obj}</{ConsoleColor.Red}>");

            DefaultLogFormat += (format, arguments) => _logQueue.Enqueue($"<{ConsoleColor.Gray}>{GetTime()} - <{CurrentLogger.Tag}> {string.Format(format?.ToString() ?? "null", arguments ?? new object[] { })}</{ConsoleColor.Gray}>");
            InfoLogFormat += (format, arguments) => _logQueue.Enqueue($"<{ConsoleColor.Green}>{GetTime()} - <{CurrentLogger.Tag}> (INFO) {string.Format(format?.ToString() ?? "null", arguments ?? new object[] { })}</{ConsoleColor.Green}>");
            WarningLogFormat += (format, arguments) => _logQueue.Enqueue($"<{ConsoleColor.Yellow}>{GetTime()} - <{CurrentLogger.Tag}> (WARNING) {string.Format(format?.ToString() ?? "null", arguments ?? new object[] { })}</{ConsoleColor.Yellow}>");
            ErrorLogFormat += (format, arguments) => _logQueue.Enqueue($"<{ConsoleColor.Red}>{GetTime()} - <{CurrentLogger.Tag}> (ERROR) {string.Format(format?.ToString() ?? "null", arguments ?? new object[] { })}</{ConsoleColor.Red}>");

            ExceptionLog += ex => _logQueue.Enqueue(string.Format($"<{ConsoleColor.Red}>{GetTime()} - <{CurrentLogger.Tag}> (EXCEPTION) {{0}} {{1}} {{2}}</{ConsoleColor.Red}>", ex.GetType().Name.ToUpper(), ex.Message, ex.StackTrace));

            TrickTask.StartNewTask(async () => await ConsoleUpdateTask(), TaskCreationOptions.LongRunning);
        }

        private async Task ConsoleUpdateTask()
        {
            while (true)
            {
                if (!_logQueue.IsEmpty)
                {
                    while (_logQueue.TryDequeue(out var str))
                    {
                        TrickConsole.WriteColorLine(str);
                    }
                }
                await Task.Delay(50);
            }
        }

        private string GetTime()
        {
            return $"{DateTime.Now:hh:mm:ss.ff}";
        }
    }
}