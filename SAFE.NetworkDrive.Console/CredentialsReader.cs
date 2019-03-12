
namespace SAFE.NetworkDrive.Console
{
    public class CredentialsReader : StringReader
    {
        public string GetLocator()
            => GetSecretString("locator");

        public string GetSecret()
            => GetSecretString("secret");
    }
}