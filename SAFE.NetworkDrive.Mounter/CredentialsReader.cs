
namespace SAFE.NetworkDrive.Mounter
{
    public class CredentialsReader : StringReader
    {
        public string GetLocator()
            => GetSecretString("locator");

        public string GetSecret()
            => GetSecretString("secret");
    }
}