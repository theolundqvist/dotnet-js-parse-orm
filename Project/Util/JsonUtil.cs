using System;
using System.Collections.Generic;
using Newtonsoft.Json; //AOT
using RestSharp;

namespace Project.Util //TODO submodule??
{
    using _ = Newtonsoft.Json.JsonConvert;  
    public static class JsonUtil
    {

        public static string ToJson<T>(T obj, bool pretty = false)
        {
            var formatting = pretty ? Formatting.Indented : Formatting.None;
            return _.SerializeObject(obj, formatting);
        }
        public static T FromJson<T>(string json)
        {
            return _.DeserializeObject<T>(json);
        }
        public static string Prettify(string json)
        {
            return _.SerializeObject(_.DeserializeObject(json), Formatting.Indented);
        }
        [Obsolete]
        public static void AllowPrivateExcludeNull() {
            //_.SetDefaultResolver(Utf8Json.Resolvers.StandardResolver.AllowPrivateExcludeNull);
        }
        //public static void AllowPrivateIncludeNull()
        //{
        //    _.SetDefaultResolver(Utf8Json.Resolvers.StandardResolver.AllowPrivate);
        //}
        //public static void DisallowPrivateIncludeNull()
        //{
        //    _.SetDefaultResolver(Utf8Json.Resolvers.StandardResolver.Default);
        //}
        //public static void DisallowPrivateExcludeNull()
        //{
        //    _.SetDefaultResolver(Utf8Json.Resolvers.StandardResolver.ExcludeNull);
        //}
    }

}
