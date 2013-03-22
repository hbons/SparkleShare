#if !NETFX_CORE
using System;
using SparkleLib;

namespace ServiceStack.Logging.Support.Logging
{
    /// <summary>
    /// Default logger is to Console.WriteLine
    /// 
    /// Made public so its testable
    /// </summary>
    public class SparkleShareLogger : ILog
    {
        const string DEBUG = "DEBUG: ";
        const string ERROR = "ERROR: ";
        const string FATAL = "FATAL: ";
        const string INFO = "INFO: ";
        const string WARN = "WARN: ";

        string type = null;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public SparkleShareLogger(string type)
        {
            this.type = type;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public SparkleShareLogger(Type type)
        {
            this.type = type.ToString();
        }
        
        #region ILog Members
        
        public bool IsDebugEnabled { get { return true; } }
        
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        private static void Log(object message, Exception exception,string type)
        {
            string msg = message == null ? string.Empty : message.ToString();
            if (exception != null)
            {
                msg += ", Exception: " + exception.Message;
            }
            SparkleLogger.LogInfo (type,msg);
        }
        
        /// <summary>
        /// Logs the format.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        private static void LogFormat(object message,string type, params object[] args)
        {
            string msg = message == null ? string.Empty : message.ToString();
            SparkleLogger.LogInfo (type,msg);
        }
        
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private static void Log(object message,string type)
        {
            string msg = message == null ? string.Empty : message.ToString();
            SparkleLogger.LogInfo (type,msg);
        }
        
        public void Debug(object message, Exception exception)
        {
            Log(DEBUG + message, exception,type);
        }
        
        public void Debug(object message)
        {
            Log(DEBUG + message,type);
        }
        
        public void DebugFormat(string format, params object[] args)
        {
            LogFormat(DEBUG + format, type,args);
        }
        
        public void Error(object message, Exception exception)
        {
            Log(ERROR + message, exception,type);
        }
        
        public void Error(object message)
        {
            Log(ERROR + message,type);
        }
        
        public void ErrorFormat(string format, params object[] args)
        {
            LogFormat(ERROR + format, type, args);
        }
        
        public void Fatal(object message, Exception exception)
        {
            Log(FATAL + message, exception,type);
        }
        
        public void Fatal(object message)
        {
            Log(FATAL + message,type);
        }
        
        public void FatalFormat(string format, params object[] args)
        {
            LogFormat(FATAL + format,type, args);
        }
        
        public void Info(object message, Exception exception)
        {
            Log(INFO + message, exception,type);
        }
        
        public void Info(object message)
        {
            Log(INFO + message,type);
        }
        
        public void InfoFormat(string format, params object[] args)
        {
            LogFormat(INFO + format,type, args);
        }
        
        public void Warn(object message, Exception exception)
        {
            Log(WARN + message, exception,type);
        }
        
        public void Warn(object message)
        {
            Log(WARN + message,type);
        }
        
        public void WarnFormat(string format, params object[] args)
        {
            LogFormat(WARN + format, type, args);
        }
        
        #endregion
    }
}
#endif
