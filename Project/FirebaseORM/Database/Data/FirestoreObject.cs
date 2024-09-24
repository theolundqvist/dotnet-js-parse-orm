using System.Reflection;
using Project.Util;
using Debugger = Project.Util.Debugger;

namespace Project.FirebaseORM.Database.Data
{[Obsolete("Use new Project.Backend")]
    public abstract class FirestoreObject
    {
        protected Dictionary<string, dynamic> Doc = new();
        protected Dictionary<string, dynamic> Fields = new();

        /// <summary>
        /// Creates a FirestoreObject from json data formatted according to the firestore rules.
        /// </summary>
        /// <param name="data"></param>
        protected FirestoreObject(string data=null)
        {
            Init(data ?? ToFirestoreJson());
        }

        private void Init(string json)
        {
            Doc = JsonUtil.FromJson<dynamic>(json);
            if (!TryGetField("fields", out Fields, Doc))
                Fields = new();
        }


        /// <summary>
        /// Formats this <see cref="FirestoreObject"/> as an json string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToFirestoreJson();

        /// <summary>
        /// Use to parse an firestore json and create an object according to the type.
        /// </summary>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T BuildFromJson<T>(string json) where T : FirestoreObject =>
            (T) Activator.CreateInstance(typeof(T), json);
        
        /// <returns>
        /// Json string that represents the document. 
        /// </returns>
        public string ToFirestoreJson()
        {
            var fields = this.GetType().GetFields();
            var content = fields.Where(t => !t.IsNotSerialized).Select(t => FieldToJson(this, t)).ToList();
            return "{\"fields\":{" + string.Join(",",content) + "}}";
        }

        #region protected
        

        #region serialization
        
        /// <summary>
        /// Translate runtime Type to firestore type string
        /// </summary>
        public static string GetTypeString(Type type)
        { 
            if (type.BaseType?.Name == "FirestoreObject") return "mapValue";
            return type.Name switch
            {
                "String" => "stringValue",
                "Boolean" => "booleanValue",
                "Int32" => "integerValue", //All firestore ints are Int64
                "Int64" => "integerValue",
                "Single" => "doubleValue", // There is no floatValue in Firestore
                "Double" => "doubleValue",
                _ => ""
            };
        } 

        /// <returns> Serialized field <b><paramref name="f"/></b> of <b><paramref name="obj"/></b>, field will be serialized in the "firestore" way </returns>
        protected static string FieldToJson(object obj, FieldInfo f)
        {
            var typeString = GetTypeString(f.FieldType);
            var d = typeString is "doubleValue" or "mapValue"  ? "" : "\"";
            if (typeString == "mapValue" && f.GetValue(obj) == null)
                return "\"" + f.Name + "\":" + "{" + "\"" + typeString + "\"" + ": {}}"; 
            return f.FieldType.IsArray switch
            {
                true => "\"" + f.Name + "\":" + "{" + ArrayToJson(f.GetValue(obj)) + "}",
                _ => "\"" + f.Name + "\":" + "{" + "\"" + typeString + "\"" + ":" + d + "" +
                     f.GetValue(obj) + "" + d + "}"
            };
        }
        
        
        /// <returns> Serialized array (<b><paramref name="obj"/></b>), array will be serialized in the "firestore" way </returns>
        protected static string ArrayToJson(object obj)
        {
            var a = (dynamic[]) obj;
            var res = "";
            if (a != null && a.Length > 0)
            {
                if (a[0] == null) throw new Exception("Can't serialize array if it contains elements that are null");
                var typeString = GetTypeString(a[0].GetType());
                //var q = GetTypeString(a[0].GetType()) == "doubleValue" ? "" : "\"";
                var q = typeString is "doubleValue" or "mapValue"  ? "" : "\"";
                //if (typeString == "mapValue" && a[0].GetValue(obj) == null)
                    //return "{" + "\"" + typeString + "\"" + ": {}}"; 

                res = string.Join(",",
                    a.Select(ai =>
                    {
                        
                        if (typeString == "mapValue" && ai == null) return "{" + "\"" + typeString + "\"" + ": {}}"; 
                        if (ai == null) throw new Exception("Can't serialize array if it contains elements that are null");
                        return "{"
                               + "\"" + GetTypeString(ai.GetType()) + "\""
                               + ":" + q + "" + ai + "" + q
                               + "}";
                    }));
            }
            return "\"arrayValue\":{" + "\"values\":[" + res +"]}";
        }
        #endregion
        
        #region Methods to try get values
        
        /// <summary>
        /// If array with name: <b><paramref name="name"/></b> is obtainable from underlying json data, modifies ref <b><paramref name="result"/></b> with the value.
        /// </summary>
        /// <returns>True if <paramref name="result"/> was modified.</returns>
        protected bool TryGetArray<T>(string name, ref T[] result)
        {
            dynamic dynArr = null;
            if (!TryGetField(name, out Dictionary<string, dynamic> field) ||
                !TryGetField("arrayValue", out field, field) ||
                !TryGetField("values", out dynArr, field) || dynArr == null ||
                dynArr.Count == 0)
            {
                result ??= Array.Empty<T>();
                return false;
            }
            
            var fsType = GetTypeString(typeof(T));
            T[] res = new T[dynArr.Count];
            for (int i = 0; i < dynArr.Count; i++)
            {
                T val;
                if (fsType == "mapValue")
                {
                    Debugger.Print(JsonUtil.ToJson(dynArr[i]["mapValue"]));
                    val = (T) Activator.CreateInstance(typeof(T), JsonUtil.ToJson(dynArr[i]["mapValue"]));
                    Debugger.Print(JsonUtil.ToJson(val));
                    //if(!TryGetField(fsType, ))
                }
                else if (!TryGetField(fsType, out val, dynArr[i])) throw new Exception("Value is of the wrong type, elements in " + name + " should be of type " +
                    typeof(T).Name + " but is " + JsonUtil.ToJson(dynArr[i]));
                res[i] = val;
            }
            result = res;
            return true;
        }

        protected bool TryGetObject<T>(string name, ref T result) where T : FirestoreObject
        {
            if (!TryGetField(name, out Dictionary<string, dynamic> fieldValue) ||
                !TryGetField("mapValue", out Dictionary<string, dynamic> mapValue, fieldValue)) return false;
            result = FirestoreObject.BuildFromJson<T>(JsonUtil.ToJson(mapValue));
            return true;
        }
        
        /// <summary>
        /// If attribute of name <b><paramref name="name"/></b> is obtainable from underlying json data, modifies ref <b><paramref name="result"/></b> with the value.
        /// </summary>
        /// <returns>True if <paramref name="result"/> was modified.</returns>
        protected static bool TryGetValue<TE>(string name, ref TE result, Dictionary<string, dynamic> dict) 
        {
            if (typeof(TE).IsArray) throw new ArgumentException("Use method 'TryGetArray' for array values");
            Dictionary<string, dynamic> field;
            if (!TryGetField(name, out field, dict)) return false;
            var fsType = GetTypeString(typeof(TE));
            TE val;
            if (!TryGetField(fsType, out val, field)) return false;
            if (val == null && fsType == GetTypeString(typeof(string))) val = (TE)(object)"";
            result = val;
            
            return true;
        }

        /// <summary>
        /// If attribute of name <b><paramref name="name"/></b> is obtainable from underlying json data, modifies ref <b><paramref name="result"/></b> with the value.
        /// </summary>
        /// <returns>True if <paramref name="result"/> was modified.</returns>
        protected bool TryGetValue<TE>(string name, ref TE result) => TryGetValue(name, ref result, Fields);

        /// <summary>
        /// If field of name <b><paramref name="name"/></b> is obtainable from the Dictionary, modifies ref <b><paramref name="result"/></b> with the value.
        /// </summary>
        /// <returns>True if field by name: <b><paramref name="name"/></b> was found.</returns>
        protected static bool TryGetField<E>(string name, out E result, Dictionary<string, dynamic> dict)
        {
            if (dict.ContainsKey(name))
            {
                var val = dict[name];
                if (val is E)
                {
                    result = val;
                    return true;
                }
                if (val.GetType().ToString() == "System.String" &&
                    typeof(E).Name == "Int32")
                {
                    result = (E)(object) int.Parse(val);
                    return true;
                }
                if (val.GetType().ToString() == "System.String" &&
                    typeof(E).Name == "Int64")
                {
                    result = (E)(object) long.Parse(val);
                    return true;
                }
            }
            result = default;
            return false;
        }


        /// <summary>
        /// If field of name <b><paramref name="name"/></b> is obtainable from underlying json data, modifies ref <b><paramref name="result"/></b> with the value.
        /// </summary>
        /// <returns>True if field by name: <b><paramref name="name"/></b> was found.</returns>
        protected bool TryGetField<E>(string name, out E result) => TryGetField(name, out result, Fields);
        
        #endregion
        
        
        #endregion
    }
    
    
}
