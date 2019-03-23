
namespace SAFE.NetworkDrive.ConsoleApp
{
    public class UserReader : StringReader
    {
        public string GetUserName()
            => GetString("username");

        public string GetPassword()
            => GetSecretString("password");
    }
}