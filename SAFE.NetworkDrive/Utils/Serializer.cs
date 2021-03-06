﻿using Newtonsoft.Json;
using System;

namespace SAFE.NetworkDrive.Gateways.Utils
{
    public static class Serializer
    {
        public static JsonSerializerSettings SerializerSettings;

        static Serializer()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                Culture = new System.Globalization.CultureInfo(string.Empty)
                {
                    NumberFormat = new System.Globalization.NumberFormatInfo
                    {
                        CurrencyDecimalDigits = 31
                    }
                },
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new PrivateMembersContractResolver()
            };
            SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            JsonConvert.DefaultSettings = () => SerializerSettings;
        }

        public static string Json(this object data)
            => JsonConvert.SerializeObject(data, SerializerSettings);

        public static T Parse<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json, SerializerSettings);

        public static object Parse(this string json, string typeName)
        {
            var type = Type.GetType(typeName, AssemblyResolver, null);
            return JsonConvert.DeserializeObject(json, type, SerializerSettings); // //JsonConvert.DefaultSettings = () => SerializerSettings;
        }

        static System.Reflection.Assembly AssemblyResolver(System.Reflection.AssemblyName assemblyName)
        {
            assemblyName.Version = null; // we don't want assembly version to impact readability of earlier data
            return System.Reflection.Assembly.Load(assemblyName);
        }
    }
}