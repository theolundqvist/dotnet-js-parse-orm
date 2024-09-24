using Project.Util;

namespace Project.FirebaseORM.Database.Data
{
    using RU = ReflectionUtil;
    /// <summary>
    /// Class library for wrapping firestore json data and simplifing parsing process.
    /// </summary>
    [Obsolete]
    public static class OldData
    {
        #region valueTypes
        [Obsolete("Use new Project.Backend")]
        public abstract class FirestoreValue
        {
            internal G getValue<G>()
            {
                G value = RU.New<G>();

                RU.ForAllAttributes(this, attr =>
                {
                    //Debugger.Print(attr.Name + " : " + attr.Type.Name + " = " + attr.GetValue(this).ToString());

                    if (attr.IsAttribute && attr.Type == typeof(G))
                    {
                        value = (G) attr.GetValue(this);
                    }
                    else if (attr.Name.Contains("integer"))
                    {
                        value = (G) (int.Parse(attr.GetValue(this).ToString()) as object);
                    }
                });
                return value;
            }

            internal FirestoreValue setValue<G>(G value)
            {
                RU.ForAllAttributes(this, att =>
                {
                    if (att.Type == typeof(G) && !att.Name.Contains("Parsed"))
                    {
                        att.SetValue(this, value);
                    }
                    else if(att.Type == typeof(string))
                    {
                        att.SetValue(this, value.ToString());
                    }
                });
                return this;
            }

            internal Type GetWrappedType()
            {
                Type t = null;
                RU.ForAllAttributes(this, att =>
                {
                    if(att.IsAttribute) t = att.Type;
                });
                return t;
            }
        }


        public class String : FirestoreValue
        {
            public string stringValue { get; set; } = "";
            public String(string stringValue) => this.stringValue = stringValue;
            public String() { } //!! NEEDED FOR INSTANTIATION
        }
        
        public class Integer : FirestoreValue
        {
            [System.Runtime.Serialization.IgnoreDataMember]
            internal int integerValueParsed => int.Parse(integerValue);
            public string integerValue { get; set; } = "0";
            public Integer(int integerValue) => this.integerValue = integerValue.ToString();
            public Integer() { }
        }
        
        public class Boolean : FirestoreValue
        {
            public bool booleanValue { get; set; } = false;
            public Boolean(bool booleanValue) => this.booleanValue = booleanValue;
            public Boolean() { } //!! NEEDED FOR INSTANTIATION
        }
        
        public class Double : FirestoreValue
        {
            public double doubleValue { get; set; }
            public Double(double doubleValue) => this.doubleValue = doubleValue;
            public Double() { } //!! NEEDED FOR INSTANTIATION
        }
        
        public class Bytes : FirestoreValue
        {
            public byte[] bytesValue { get; set; } = Array.Empty<byte>();
            public Bytes(byte[] bytesValue) => this.bytesValue = bytesValue;
            public Bytes() { }
        }
        
        public class TimeStamp : FirestoreValue
        {
            public DateTime timestampValue { get; set; } = new DateTime();
            public TimeStamp(DateTime timestampValue) => this.timestampValue = timestampValue;
            public TimeStamp() { }
        }
        //...
        #endregion

        
        [Obsolete("Use new Project.Backend")]
        public class Array<FVal> where FVal : FirestoreValue
        {
            public class XS
            {
                public FVal[] values = new FVal[0];
            }

            public XS arrayValue = new XS();

            public Array(){}

            public PrimVal[] GetArray<PrimVal>()
            {
                //if() check if PrimVal is OK type ???!
                if (arrayValue == null || arrayValue.values == null) return new PrimVal[0];
                return Enumerable.Range(0, arrayValue.values.Length)
                .Select(i => arrayValue.values[i].getValue<PrimVal>()).ToArray();
            }

            public Array<FVal> SetArray<PrimVal>(PrimVal[] update)
            {
                arrayValue = new XS();
                if (update == null) arrayValue.values = new FVal[0];
                else
                {
                    arrayValue.values = new FVal[update.Length];
                    for (int i = 0; i < update.Length; i++)
                    {
                        arrayValue.values[i] = RU.New<FVal>().setValue(update[i]) as FVal;
                    }
                }
                return this;
            }
        }

        /// <summary>
        /// DOESN'T WORK,  should wrap "MapValue" as Dictionary<string, DBVal>
        /// </summary>
        // public class Map<T> where T : FirestoreValue
        // {
        //     public Dictionary<string, T> fields;
        // }

        // public abstract class Fields
        // {
        //     /// use this to implement a class "PersonFields" and add your fields.
        //     /// ex.
        //     /// class PersonFields : Fields
        //     ///     public Val name;
        //     ///     public Val age;
        //     ///     public Array friends;
        //     ///
        //     /// 
        //     /// create "Person" class
        //     ///
        //     ///     PersonFields fields;
        //     ///     string name;
        //     ///     int age;
        //     ///     string[] friends;
        //     ///     
        //
        //     internal Dictionary<string, string> GetPropertiesJson()
        //     {
        //         return RU.GetPropertiesJson(this as object);
        //     }
        //
        //     public override string ToString() => JsonUtil.ToJson(this);
        //
        // }
        //
        // public abstract class Document
        // {
        //     [System.Runtime.Serialization.DataMember(Name = "name")]
        //     public string completePath = "";
        //     public string DocumentId => completePath.Split('/').Last();
        //     public override string ToString() => JsonUtil.ToJson(this);
        //
        //     internal Fields GetFieldsObj()
        //     {
        //
        //         Fields fields = null;
        //         int HighestBaseTypeCountOnField = 0;
        //         RU.ForAllAttributes(this, attr => {
        //             //Debugger.Print(member.Name);
        //             //if(member.Name == "fields")
        //             //{
        //             //    member.PrintBaseTypes();
        //             //}
        //             //Debugger.Print(member.HasBaseType(typeof(Fields)));
        //             if (attr.HasBaseType(typeof(Fields)))
        //             {
        //
        //                 if (attr.BaseTypeCount() > HighestBaseTypeCountOnField)
        //                 {
        //                     HighestBaseTypeCountOnField = attr.BaseTypeCount();
        //                     var v = attr.GetValue(this);
        //                     if (v is null) attr.SetValue(this, Activator.CreateInstance(attr.Type));
        //                     fields = attr.GetValue<Fields>(this);
        //                 }
        //             }
        //         });
        //         if (fields != null) return fields;
        //         else throw new Exception("Class inheriting Document doesn't implement custom Fields property");
        //         //Debugger.Print("userfield count:  " + string.Concat(RU.GetPropertiesJson(this).Keys));
        //     }
        //
        //     public void LoadFields()
        //     {
        //         Fields fieldsObj = GetFieldsObj();
        //
        //         Dictionary<string, RU.Member> docProps = RU.GetMembers(this);
        //         Dictionary<string, RU.Member> wrappedProps = RU.GetMembers(fieldsObj);
        //         Debugger.Print(string.Join(", ", docProps.Keys));
        //         Debugger.Print(string.Join(", ", wrappedProps.Keys));
        //         foreach (string k in wrappedProps.Keys)
        //         {
        //             var prop = docProps[k];
        //             var wrappedProp = wrappedProps[k];
        //             var value = wrappedProp.GetValue(fieldsObj);
        //             //Debugger.Print("LOADING FIELD:\nName: " + prop.Name + "\nType: " + prop.Type.FullName);
        //             if (value != null) {
        //                 #region translators
        //                 //Debugger.Print(prop.Type);
        //                 //VALUES
        //                 if (prop.Type == typeof(int))
        //                 {
        //                     prop.SetValue(this, (value as Integer).integerValueParsed);
        //                 }
        //                 else if (prop.Type == typeof(string))
        //                 {
        //                     prop.SetValue(this, (value as String).stringValue);
        //                 }
        //                 else if (prop.Type == typeof(bool))
        //                 {
        //                     prop.SetValue(this, (value as Boolean).booleanValue);
        //                 }
        //                 else if (prop.Type == typeof(byte[]))
        //                 {
        //                     prop.SetValue(this, (value as Bytes).bytesValue);
        //                 }
        //                 else if (prop.Type == typeof(double))
        //                 {
        //                     prop.SetValue(this, (value as Double).doubleValue);
        //                 }
        //                 else if (prop.Type == typeof(DateTime))
        //                 {
        //                     prop.SetValue(this, (value as TimeStamp).timestampValue);
        //                 }
        //                 //ARRAYS
        //                 else if (prop.Type.IsArray)
        //                 {
        //                     if (prop.Type == typeof(string[]))
        //                     {
        //                         prop.SetValue(this, (value as Array<String>).GetArray<string>());
        //                     }
        //                     else if (prop.Type == typeof(int[]))
        //                     {
        //                         prop.SetValue(this, (value as Array<Integer>).GetArray<int>());
        //                     }
        //                     else if (prop.Type == typeof(bool[]))
        //                     {
        //                         prop.SetValue(this, (value as Array<Boolean>).GetArray<bool>());
        //                     }
        //                     else if (prop.Type == typeof(double[]))
        //                     {
        //                         prop.SetValue(this, (value as Array<Double>).GetArray<double>());
        //                     }
        //                     //else if (prop.Type == typeof(byte[]))
        //                     //{
        //                     //    SetValue(new Array<Bytes>().SetArray(prop.GetValue<byte[]>(this)));
        //                     //}
        //                     else if (prop.Type == typeof(DateTime[]))
        //                     {
        //                         prop.SetValue(this, (value as Array<TimeStamp>).GetArray<DateTime>());
        //                     }
        //                 }
        //                 else {
        //                     Debugger.Print("TYPE NOT IMPLEMENTED WHEN LOADING FIELDS:\nName: " + prop.Name + "\nType: " + prop.Type.FullName);
        //                 }
        //                 #endregion translators
        //             }
        //         }
        //     }
        //
        //     private void UpdateFields()
        //     {
        //         Fields fieldsObj = GetFieldsObj();
        //
        //         Dictionary<string, RU.Member> docProps = RU.GetMembers(this);
        //         Dictionary<string, RU.Member> wrappedProps = RU.GetMembers(fieldsObj);
        //         //Debugger.Print(string.Join(", ", docProps.Keys));
        //         //Debugger.Print(string.Join(", ", wrappedProps.Keys));
        //         foreach (string k in wrappedProps.Keys)
        //         {
        //             var prop = docProps[k];
        //             var wrappedProp = wrappedProps[k];
        //             void SetValue(object val) => wrappedProp.SetValue(fieldsObj, val);
        //             //Debugger.Print(prop.Type + " : " + prop.Name);
        //             //VALUES
        //             if (prop.GetValue(this) != null)
        //             {
        //                 #region translators
        //                 //string,int,bool,double,byte,TimeStamp
        //                 // ADD ALL VALUES OR FIND A SMARTER (SLOWER) WAY, MABEY BEST TO JUST HARD CODE THIS
        //                 if (prop.Type == typeof(int))
        //                 {
        //                     SetValue(new Integer(prop.GetValue<int>(this)));
        //                 }
        //                 else if (prop.Type == typeof(string))
        //                 {
        //                     SetValue(new String(prop.GetValue<string>(this)));
        //                 }
        //                 else if (prop.Type == typeof(bool))
        //                 {
        //                     SetValue(new Boolean(prop.GetValue<bool>(this)));
        //                 }
        //                 else if (prop.Type == typeof(double))
        //                 {
        //                     SetValue(new Double(prop.GetValue<double>(this)));
        //                 }
        //                 else if (prop.Type == typeof(byte[]))
        //                 {
        //                     SetValue(new Bytes(prop.GetValue<byte[]>(this)));
        //                 }
        //                 else if (prop.Type == typeof(DateTime))
        //                 {
        //                     SetValue(new TimeStamp(prop.GetValue<DateTime>(this)));
        //                 }
        //                 else if (prop.Type.IsArray)
        //                 {
        //                     if (prop.Type == typeof(string[]))
        //                     {
        //                         SetValue(new Array<String>().SetArray(prop.GetValue<string[]>(this)));
        //                     }
        //                     else if (prop.Type == typeof(int[]))
        //                     {
        //                         SetValue(new Array<Integer>().SetArray(prop.GetValue<int[]>(this)));
        //                     }
        //                     else if (prop.Type == typeof(bool[]))
        //                     {
        //                         SetValue(new Array<Boolean>().SetArray(prop.GetValue<bool[]>(this)));
        //                     }
        //                     else if (prop.Type == typeof(double[]))
        //                     {
        //                         SetValue(new Array<Double>().SetArray(prop.GetValue<double[]>(this)));
        //                     }
        //                     //else if (prop.Type == typeof(byte[]))
        //                     //{
        //                     //    SetValue(new Array<Bytes>().SetArray(prop.GetValue<byte[]>(this)));
        //                     //}
        //                     else if (prop.Type == typeof(DateTime[]))
        //                     {
        //                         SetValue(new Array<TimeStamp>().SetArray(prop.GetValue<DateTime[]>(this)));
        //                     }
        //                 }
        //                 else
        //                 {
        //                     Debugger.Print("TYPE NOT IMPLEMENTED WHEN UPDATING FIELDS:\nName: " + prop.Name + "\nType: " + prop.Type.FullName);
        //                 }
        //                 #endregion translators
        //             }
        //         }
        //     }
        //
        //     private Dictionary<string, string> GetPropertiesJson()
        //     {
        //
        //         var fObj = GetFieldsObj();
        //         if (fObj is null) return new Dictionary<string, string>();
        //         return fObj.GetPropertiesJson();
        //     }
        //
        //     public string BuildCreateRequestJson()
        //     {
        //         UpdateFields();
        //         var props = GetPropertiesJson();
        //         string changeJSON = "";
        //         foreach (var prop in props)
        //         {
        //             changeJSON += "\"" + prop.Key + "\":" + prop.Value + ",";
        //         }
        //
        //         return "{\"fields\": {" + changeJSON.TrimEnd(',') + "}}";
        //     }
        //     public string BuildUpdateRequestJson(string docPath = "users/{uid}")
        //     {
        //         var oldProps = GetPropertiesJson();
        //         //Debugger.Print("old: "+ string.Join(", ", oldProps));
        //         UpdateFields();
        //         var newProps = GetPropertiesJson();
        //         //Debugger.Print("new: " + string.Join(", ", newProps));
        //         //same keys is guaranteed.    trodde jag.... tack på efterhand, löste där nere
        //
        //         string changeJSON = "";
        //         string propsToUpdate = "";
        //         for (int i = 0; i < newProps.Keys.Count; i++)
        //         {
        //             var k = newProps.Keys.ElementAt(i);
        //             //Debug.Log(oldProps[k] + " == " + newProps[k]);
        //             if (oldProps is null || oldProps[k] != newProps[k])
        //             {
        //                 changeJSON += "\"" + k + "\":" + newProps[k] + ",";
        //                 propsToUpdate += "\"" + k + "\",";
        //             }
        //         }
        //
        //         if (changeJSON == "") return null;
        //         propsToUpdate = propsToUpdate.TrimEnd(',');
        //         changeJSON = changeJSON.TrimEnd(',');
        //
        //         string updateMask = "\"updateMask\": {\"fieldPaths\": [" + propsToUpdate + "]}";
        //
        //         string fieldsJson = "\"fields\": {" + changeJSON + "}";
        //         string docRef = "\"name\":\"" + FirebaseCredentials.dbPath + docPath + "\"";
        //         string update = "\"update\":{" + docRef + "," + fieldsJson + "}";
        //         string payload = "{\"writes\": [{" + update + "," + updateMask + "}]}";
        //
        //         return payload;
        //     }
        // }
    }
}