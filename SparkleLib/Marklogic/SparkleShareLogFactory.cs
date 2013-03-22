#if !NETFX_CORE
using System;

namespace ServiceStack.Logging.Support.Logging
{
    /// <summary>
    /// Creates a Console Logger, that logs all messages to: System.Console
    /// 
    /// Made public so its testable
    /// </summary>
    public class SparkleShareLogFactory : ILogFactory
    {
        public ILog GetLogger(Type type)
        {
            return new SparkleShareLogger(type);
        }

        public ILog GetLogger(string typeName)
        {
            return new SparkleShareLogger(typeName);
        }
    }
}
#endif
