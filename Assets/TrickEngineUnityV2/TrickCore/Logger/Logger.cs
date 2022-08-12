using System;

namespace TrickCore
{
    public sealed class Logger
    {
        public enum VerboseTypes
        {
            /// <summary>
            /// Completely disables logging
            /// </summary>
            Disabled = 0,

            /// <summary>
            /// Logs info and up (<see cref="Logger.LogInfo(object)"/>)
            /// </summary>
            Info,

            /// <summary>
            /// Logs default and up (<see cref="Logger.Log(object)"/>)
            /// </summary>
            Default,

            /// <summary>
            /// Logs warnings and up (<see cref="Logger.LogWarning(object)"/>)
            /// </summary>
            Warning,

            /// <summary>
            /// Only logs errors and exceptions (<see cref="Logger.LogError(object)"/>)
            /// </summary>
            Error,

            /// <summary>
            /// Only logs exceptions (<see cref="Logger.LogException(System.Exception)"/>)
            /// </summary>
            Exception
        }

        /// <summary>
        /// Log verbosity
        /// </summary>
        public VerboseTypes Verbosity = VerboseTypes.Info;
        
        public static readonly Logger Core = new Logger("CORE");
        public static readonly Logger Sql = new Logger("SQL");
        public static readonly Logger Network = new Logger("NETWORK");
        public static readonly Logger Login = new Logger("LOGIN");

        /// <summary>
        /// The logger instance for game related logs.
        /// </summary>
        public static readonly Logger Game = new Logger("GAME");
        
        /// <summary>
        /// Gets the current Tag of the Logger object.
        /// </summary>
        public string Tag { get; private set; }

        public Logger(string tag)
        {
            Tag = tag;
        }

        /// <summary>
        /// Gets or sets the log target. Defaulted to Console output.
        /// </summary>
        public static LogTarget LogTarget { get; set; } = new ConsoleLogger();

        /// <summary>Log a message to the console.</summary>
        /// <param name="obj">Log object</param>
        /// <filterpriority>1</filterpriority>
        public void Log(object obj)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Default) return;
            LogTarget.CurrentLogger = this;
            LogTarget.DefaultLog?.Invoke(obj);
        }

        /// <summary>Log a message to the console.</summary>
        /// <param name="format">A composite format string</param>
        /// <param name="arg">The log arguments</param>
        /// <filterpriority>1</filterpriority>
        public void Log(object format, params object[] arg)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Default) return;
            LogTarget.CurrentLogger = this;
            LogTarget.DefaultLogFormat?.Invoke(format, arg);
        }

        /// <summary>Log an info message to the console.</summary>
        /// <param name="obj">Log object</param>
        /// <filterpriority>1</filterpriority>
        public void LogInfo(object obj)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Info) return;
            LogTarget.CurrentLogger = this;
            LogTarget.InfoLog?.Invoke(obj);
        }

        /// <summary>Log an info message to the console.</summary>
        /// <param name="format">A composite format string</param>
        /// <param name="arg">The log arguments</param>
        /// <filterpriority>1</filterpriority>
        public void LogInfo(object format, params object[] arg)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Info) return;
            LogTarget.CurrentLogger = this;
            LogTarget.InfoLogFormat?.Invoke(format, arg);
        }

        /// <summary>Log a warning message to the console.</summary>
        /// <param name="obj">Log object</param>
        /// <filterpriority>1</filterpriority>
        public void LogWarning(object obj)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Warning) return;
            LogTarget.CurrentLogger = this;
            LogTarget.WarningLog?.Invoke(obj);
        }

        /// <summary>Log a warning message to the console.</summary>
        /// <param name="format">A composite format string</param>
        /// <param name="arg">The log arguments</param>
        /// <filterpriority>1</filterpriority>
        public void LogWarning(object format, params object[] arg)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Warning) return;
            LogTarget.CurrentLogger = this;
            LogTarget.WarningLogFormat?.Invoke(format, arg);
        }

        /// <summary>Log an error message to the console.</summary>
        /// <param name="obj">Log object</param>
        /// <filterpriority>1</filterpriority>
        public void LogError(object obj)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Error) return;
            LogTarget.CurrentLogger = this;
            LogTarget.ErrorLog?.Invoke(obj);
        }

        /// <summary>Log an error message to the console.</summary>
        /// <param name="format">A composite format string</param>
        /// <param name="arg">The log arguments</param>
        /// <filterpriority>1</filterpriority>
        public void LogError(object format, params object[] arg)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Error) return;
            LogTarget.CurrentLogger = this;
            LogTarget.ErrorLogFormat?.Invoke(format, arg);
        }
        
        /// <summary>Log an exception to the console.</summary>
        /// <param name="exception">The exception</param>
        /// <filterpriority>1</filterpriority>
        public void LogException(Exception exception)
        {
            if (Verbosity == VerboseTypes.Disabled || Verbosity > VerboseTypes.Exception) return;
            if (exception == null) return;

            LogTarget.CurrentLogger = this;
            LogTarget.ExceptionLog?.Invoke(exception);
        }
    }
}
