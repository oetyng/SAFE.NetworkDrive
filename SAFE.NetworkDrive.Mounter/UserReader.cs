
namespace SAFE.NetworkDrive.Mounter
{
    public class UserReader : StringReader
    {
        public string GetUserName()
        {
            return GetString("username");
        }

        public string GetPassword()
        {
            return GetSecretString("password");
        }
    }
}