
namespace SAFE.NetworkDrive.Mounter
{
    public class CredentialsReader : StringReader
    {
        public string GetLocator()
        {
            return GetSecretString("locator");
        }

        public string GetSecret()
        {
            return GetSecretString("secret");
        }
    }
}