//using System;
//using System.Collections.Generic;

//namespace SAFE.NetworkDrive.Mounter.Config
//{
//    public static class DriveConfigExtensions
//    {
//        public static IDictionary<string, string> GetParameters(this DriveConfig config)
//        {
//            if (config == null)
//                throw new ArgumentNullException(nameof(config));

//            var parameters = config.Parameters;
//            if (string.IsNullOrEmpty(parameters))
//                return null;

//            var result = new Dictionary<string, string>();
//            foreach (var parameter in parameters.Split('|'))
//            {
//                var components = parameter.Split(new[] { '=' }, 2);
//                result.Add(components[0], components.Length == 2 ? components[1] : null);
//            }

//            return result;
//        }
//    }
//}