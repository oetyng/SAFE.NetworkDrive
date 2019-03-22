using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace SAFE.NetworkDrive.Gateways.Utils
{
    // http://stackoverflow.com/questions/4066947/private-setters-in-json-net
    // Without this, collections will be null, since all events have private setters.
    public class PrivateMembersContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(
            MemberInfo member,
            MemberSerialization memberSerialization)
        {
            //TODO: Maybe cache
            var prop = base.CreateProperty(member, memberSerialization);

            if (!prop.Writable)
            {
                var property = member as PropertyInfo;
                if (property != null)
                {
                    var hasPrivateSetter = property.GetSetMethod(true) != null;
                    prop.Writable = hasPrivateSetter;
                }
            }

            return prop;
        }
    }
}