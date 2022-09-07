using System;

namespace TrickCore
{
    public class LogTarget
    {
        public delegate void LogActionException(Exception ex);
        public delegate void LogAction(object obj);
        public delegate void LogFormatAction(object format, params object[] arguments);
        public delegate void LogActionCustom(string tag, object obj);
        public delegate void LogFormatActionCustom(string tag, object format, params object[] arguments);

        public LogAction DefaultLog;
        public LogAction InfoLog;
        public LogAction WarningLog;
        public LogAction ErrorLog;

        public LogFormatAction DefaultLogFormat;
        public LogFormatAction InfoLogFormat;
        public LogFormatAction WarningLogFormat;
        public LogFormatAction ErrorLogFormat;

        public LogActionException ExceptionLog;
        
        public Logger CurrentLogger { get; set; }
    }
}