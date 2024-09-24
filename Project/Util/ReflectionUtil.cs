using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace Project.Util
{
    public static class ReflectionUtil
    {

        public class Member
        {
            //A attribute (member) is property if it uses {get;set;} syntax and a field if it doesn't.
            //Could also be an event (EventInfo). We discard these.
            private PropertyInfo prop;
            private MemberInfo mem;
#pragma warning disable CS0169
            private MethodInfo method;
#pragma warning restore CS0169
            public string Name => mem.Name;
            public MemberTypes MemberType => mem.MemberType;

            public Member(MemberInfo info)
            {
                mem = info;
                if (info.MemberType == MemberTypes.Field) GetField = info as FieldInfo;
                else if (info.MemberType == MemberTypes.Property) prop = info as PropertyInfo;
            }

            public bool IsAttribute => GetField != null || prop != null;
            public FieldInfo GetField { get; }

            private Type GetInfoType() => GetField != null ? typeof(FieldInfo) : prop != null ? typeof(PropertyInfo) : null;
            private bool IsField => GetField != null;

            public Type Type => IsField ? GetField.FieldType : prop.PropertyType;
            public Type BaseType => IsField ? GetField.FieldType.BaseType : prop.PropertyType.BaseType;

            public bool HasBaseType(Type t)
            {
                var type = BaseType;
                for (int i = 0; i < 20; i++)
                {
                    if (type == null) return false;
                    if (type == t) return true;
                    else type = type.BaseType;
                }
                return false;
            }

            public int BaseTypeCount()
            {
                var type = Type;
                for (int i = 0; i < 20; i++)
                {
                    if (type == null) return i;
                    type = type.BaseType;
                }
                return 20;
            }

            public void PrintBaseTypes()
            {
                var type = Type;
                for (int i = 0; i < 20; i++)
                {
                    if (type == null) return;
                    Debugger.Print(type.Name);
                    type = type.BaseType;
                }
            }


            private static Type[] primitiveTypes = new Type[] { typeof(int), typeof(string), typeof(long), typeof(byte), typeof(float), typeof(double), typeof(char), typeof(bool), typeof(short) };
            public bool IsPrimitiveType => primitiveTypes.Contains(Type);

            public void SetValue(object obj, object val)
            {
                if (IsField) GetField.SetValue(obj, val);
                else prop.SetValue(obj, val);
            }
            public object GetValue(object obj)
            {
                if (IsField) return GetField.GetValue(obj);
                else return prop.GetValue(obj);
            }

            //TODO mabey dont need this
            public object GetValueNonNull(object obj)
            {
                var val = GetValue(obj);
                if(val == null)
                {
                    try { if (!IsPrimitiveType) return Activator.CreateInstance(Type); }
                    catch (Exception e) { Debugger.Print(e.Message); }
                }
                return val;
            }
            public T GetValue<T>(object obj)
            {
                return (T)GetValue(obj);
            }
            public T GetValue<T>(object obj, T t)
            {
                return (T)GetValue(obj);
            }
            public new int GetType()
            {
                return 0;
            }
        }


        private static object Create<T>()
        {
            if (typeof(T) == typeof(string)) return "";
            return Activator.CreateInstance<T>();
        }
        public static T New<T>()
        {
            return (T) Create<T>();
        }
        

        public static Dictionary<string, string> GetPropertiesJson(object obj)
        {
            var x = new Dictionary<string, string>();
            ForAllAttributes(obj, member =>
            {
                var val = member.GetValueNonNull(obj);
                //if (member.Name == "friends") Debugger.Print("Friends: " + JsonUtil.ToJson(val));
                if (member.IsPrimitiveType)
                    x.Add(member.Name, val?.ToString());
                else x.Add(member.Name, JsonUtil.ToJson(val));
            });
            return x;
        }

        public static void ForAllAttributes(object obj, Action<Member> a)
        {
            MemberInfo[] members = obj.GetType().GetMembers();
            //Debugger.Print(obj.GetType());
            //Debugger.Print(members.ToList().FindAll(x => !new Member(x).IsAttribute).Count);
            //Debugger.Print(members.ToList().FindAll(x => new Member(x).IsAttribute).Count);
            //members.ToList().ForEach(x => Debugger.Print(new Member(x).MemberType));
            foreach (MemberInfo mInfo in members)
            {
                var m = new Member(mInfo);
                //if (m.Name == "fields") Debugger.Print("fieldsType: " + m.MemberType + ", :" + m.IsAttribute);
                if(m.IsAttribute) a(m);
            }
        }
        
        public static FieldInfo[] GetAllFields(object _obj)
        {
            return _obj.GetType().GetFields();
        }

        public static string ToJsonNoNull(object obj)
        {
            var s = "{";
            ForAllAttributes(obj, member =>
            {
                var val = member.GetValue(obj);

                if (val != null)
                {
                    if (member.IsPrimitiveType)
                    {
                        s += "\"" + member.Name + "\":" + JsonUtil.ToJson(val) + ",";
                    }
                    else if (member.Type == typeof(string))
                    {
                        if (!new string[] { "", null, "{}" }.Contains(val))
                            s += "\"" + member.Name + "\":\"" + val + "\",";
                    }
                    else
                    {
                        s += "\"" + member.Name + "\":" + val + ",";
                    }
                }
            });
            return s.TrimEnd(',') + "}";
        }

        public static Dictionary<string, Member> GetMembers(object obj)
        {
            var x = new Dictionary<string, Member>();
            ForAllAttributes(obj, m => x.Add(m.Name, m));
            return x;
        }
        public static string[] GetPropertyNamesByType<T>(object obj, bool useBaseType = false)
        {
            var x = new List<string>();
            ForAllAttributes(obj, m => { 
                if ((useBaseType & m.BaseType == typeof(T)) ||
                        (!useBaseType & m.Type == typeof(T)))
                {
                    x.Add(m.Name);
                }
            });
            return x.ToArray();
        }
        public static string[] GetPropertyNamesByBaseType<T>(object obj)
        {
            return GetPropertyNamesByType<T>(obj, true);
        }
    }
}