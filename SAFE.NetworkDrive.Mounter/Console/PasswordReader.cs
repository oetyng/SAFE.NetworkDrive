
namespace SAFE.NetworkDrive.ConsoleApp
{
    public class PasswordReader : StringReader
    {
        public string GetPassword()
            => GetSecretString("password");
    }
}