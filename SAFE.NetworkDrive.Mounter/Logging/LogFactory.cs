
namespace SAFE.NetworkDrive.Utils
{
    public class LogFactory
    {
        public static ILogger GetLogger(string name)
        {
            var factory = new NLog.LogFactory();
            var logger = factory.GetLogger(name);
            return new Logger(logger);
        }
    }

    class Logger : ILogger
    {
        readonly NLog.ILogger _logger;

        public Logger(NLog.ILogger logger) => _logger = logger;

        public void Debug(string msg) => _logger.Debug(msg);
        public void Error(string msg) => _logger.Error(msg);
        public void Info(string msg) => _logger.Info(msg);
        public void Trace(string msg) => _logger.Trace(msg);
        public void Warn(string msg) => _logger.Warn(msg);
    }
}
