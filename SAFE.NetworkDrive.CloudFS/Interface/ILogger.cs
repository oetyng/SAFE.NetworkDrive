
namespace SAFE.NetworkDrive
{
    public interface ILogger
    {
        void Trace(string msg);
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
        void Debug(string msg);
    }
}