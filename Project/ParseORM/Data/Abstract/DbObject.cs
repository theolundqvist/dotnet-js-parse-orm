using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Project.Util;
using Parse;
using Parse.Infrastructure.Control;
using RSG;

// ReSharper disable InvalidXmlDocComment

namespace Project.ParseORM.Data;



public abstract class DbObject
{

    protected int TimeToLive { get; set; } = 60 * 10; // 10 minutes
    private DateTime LastFetch { get; set; }
    public bool IsStale => DateTime.Now.Subtract(LastFetch).TotalSeconds > TimeToLive;
    public void NoteFetched() => LastFetch = DateTime.Now;
    public void NoteStale()
    {
        // Debugger.Print($"Uid {Uid} has been marked stale after {DateTime.Now.Subtract(LastFetch).TotalSeconds} seconds");
        LastFetch = DateTime.UnixEpoch;
    }


    private ParseObject _u;
    //ignore to serialize this field
    [IgnoreDataMember]
    public ParseObject Underlying
    {
        get => _u ?? throw new Exception("ParseObject not initialized");
        internal set => _u = value;
    }

    #region key mapping
    /// For inheriting classes to define the mapping of unusual named fields. See <see cref="DbUser"/>
    protected abstract Dictionary<string, string> KeyMaps { get; }
    private readonly Dictionary<string, string> _keyMapper = new(StringComparer.OrdinalIgnoreCase);
    internal bool RemoteKeyExists(string key) => _keyMapper.ContainsKey(key);
    internal string GetRemoteKey(string key) => _keyMapper.ContainsKey(key) ? _keyMapper[key] : key;
    internal static string GetRemoteKey<T>(string key) where T : DbObject => Instantiate<T>().GetRemoteKey(key);
    internal void AddRemoteKey(string key, string remoteKey)
    {

        if (_keyMapper.ContainsKey(key))
        {
            _keyMapper[key] = remoteKey;
        }
        else _keyMapper.Add(key, remoteKey);
    }

    public List<string> GetRemoteKeys() => _keyMapper.Values.ToList();
    public List<string> GetDirtyRemoteKeys() => GetRemoteKeys().Where(k => IsKeyDirty(k)).ToList();

    internal static Dictionary<string, string> GetKeyMapper<T>() where T : DbObject => Instantiate<T>()._keyMapper;
    #endregion


    #region SubType by field
    protected struct QueryLimiter
    {
        public string Key;
        public object Value;
    }

    protected virtual QueryLimiter? SubTypeLimiter => null;
    private static QueryLimiter? GetSubTypeLimiter<T>() where T : DbObject => Instantiate<T>().SubTypeLimiter;
    public static DbQuery<T> LimitQueryToSubtype<T>(DbQuery<T> q) where T : DbObject
    {
        var ql = GetSubTypeLimiter<T>();
        return ql == null ? q : q.WhereEqualTo(ql.Value.Key, ql.Value.Value);
    }

    #endregion




    #region dirty keys
    private Dictionary<string, bool> _dirtyKeys = new(StringComparer.OrdinalIgnoreCase);

    public bool IsKeyDirty(string key)
    {
        var k = GetRemoteKey(key);

        return _dirtyKeys.ContainsKey(k) && _dirtyKeys[k];
    }

    public void SetKeyDirty(string key)
    {
        var k = GetRemoteKey(key);
        if (_dirtyKeys.ContainsKey(k))
        {
            _dirtyKeys[k] = true;
        }
        else
        {
            _dirtyKeys.Add(k, true);
        }
    }

    public void SetKeysDirty(IEnumerable<string> key)
    {
        foreach (var k in key)
        {
            SetKeyDirty(k);
        }
    }

    public void SetKeyNotDirty(string key)
    {
        var k = GetRemoteKey(key);
        if (_dirtyKeys.ContainsKey(k))
        {
            _dirtyKeys[k] = false;
        }
        else
        {
            _dirtyKeys.Add(k, false);
        }
    }

    public List<string> GetDirtyKeys() =>
        _dirtyKeys.Where(x => x.Value).Select(x => x.Key).ToList();


    internal void ClearDirtyKeys() => _dirtyKeys.Clear();

    #endregion

    // #region operations
    // public enum OpType
    // {
    //     AddUnique,
    //     Add,
    //     Increment,
    //     Remove,
    // }
    // public class DbOp
    // {
    //     public OpType __op { get; set; }
    //     public string Key { get; set; }
    // }
    //
    // public class DbArrayOp : DbOp
    // {
    //     
    // }
    //
    // public class DbIncrementOp : DbOp
    // {
    //     
    // }
    // #endregion

    public abstract string ClassName { get; }
    public static string GetClassname<T>() where T : DbObject
        => Instantiate<T>().ClassName;


    /// Wraps a ParseObject in a DbObject, does not save to cache. 
    public static T Instantiate<T>(ParseObject po) where T : DbObject
    {
        var t = Instantiate<T>();
        if (t.ClassName != po.ClassName) throw new Exception("ParseObject class name does not match");
        t.Underlying = po;
        return t;
    }

    /// <inheritdoc cref="Database.EmptyReference{T}"/>
    public static T Instantiate<T>() where T : DbObject => (T)Activator.CreateInstance(typeof(T), true);

    private static T InstantiateAny<T>() => (T)Activator.CreateInstance(typeof(T), true);

    /// <summary>
    /// Returns true of the object has never been saved to the server
    /// </summary>
    public bool IsNew => string.IsNullOrEmpty(Underlying.ObjectId);

    #region Props

    /// <inheritdoc cref="Parse.ParseObject.ObjectId"/>
    public string Uid => Underlying.ObjectId;

    /// <inheritdoc cref="Parse.ParseObject.CreatedAt"/>
    public DateTime? CreatedAt => Underlying.CreatedAt;

    /// <inheritdoc cref="Parse.ParseObject.UpdatedAt"/>
    public DateTime? UpdatedAt => Underlying.UpdatedAt;

    /// <inheritdoc cref="Parse.ParseObject.IsDirty"/> 
    public bool HasUnsavedChanges => _dirtyKeys.Count > 0;

    /// <inheritdoc cref="Parse.ParseObject.IsKeyDirty"/>
    public bool IsPropChanged(string key) => Underlying.IsKeyDirty(GetRemoteKey(key));

    /// <inheritdoc cref="Parse.ParseObject.IsDataAvailable"/> 
    public bool HasData => Underlying.IsDataAvailable;

    #endregion

    #region init
    protected internal DbObject(ParseObject underlying)
    {
        Underlying = underlying;
        Init();
    }

    public DbObject()
    {
        SetDefaultValues();
        Init();
    }

    /// <summary>
    /// Use with care, wipes all data from this object. Need to run <see cref="DbObjectExtensions.SaveAsync{T}"/> to actually save the change.
    /// </summary>
    public void SetDefaultValues()
    {
        var uid = _u?.ObjectId;
        Underlying = ClassName switch
        {
            "_User" => new ParseUser().Bind(Database.Client),
            _ => new ParseObject(ClassName, Database.Client)
        };
        Underlying.ObjectId = uid;
        ClearDirtyKeys();
        if (SubTypeLimiter != null)
        {
            Set(SubTypeLimiter?.Value, SubTypeLimiter?.Key);
        }
    }

    private void Init()
    {
        // _u.FetchIfNeededAsync();
        if (KeyMaps != null)
        {
            foreach (var mapping in KeyMaps)
                _keyMapper.Add(mapping.Key, mapping.Value);
        }
        _keyMapper.Add("uid", "objectId");
        var names = GetType().GetProperties()
            .Select(x => x.Name)
            .Where(x => !_keyMapper.ContainsKey(x));
        foreach (var name in names)
        {
            var firstLower = name[0].ToString().ToLower() + name[1..];
            _keyMapper.Add(name, firstLower);
        }

    }
    #endregion

    #region Local Getters/Setters

    ///<inheritdoc cref="Parse.ParseObject.Revert()"/> 
    public void RevertLocalChanges()
    {
        Underlying.Revert();
        ClearDirtyKeys();
    }

    /// <summary>
    /// Perform Set Operation. When object is saved the value corresponding to the key will be set.
    /// </summary>
    /// <param name="key">key for the object.</param>
    /// <param name="value">the value for the key.</param>
    /// <returns>True if the key exists and the update could be performed, otherwise false.</returns>
    public bool SetValue<T>(string key, T value)
    {
        // if (!RemoteKeyExists(key)) return false;
        var k = GetRemoteKey(key);
        SetKeyDirty(k);
        Set(value, k);
        return true;
    }

    public bool SetIfDifferent<T>(string key, T value)
    {
        var k = GetRemoteKey(key);
        bool flag = TryGet(k, out T oldValue);
        if (flag && oldValue.Equals(value)) return false;
        // Set(value, k);
        Underlying.Set(k, value);
        return true;
    }

    /// <summary>
    /// Perform Set Operation. When object is saved the value corresponding to the key will be set.
    /// </summary>
    /// <param name="key">key for the object.</param>
    /// <param name="value">the value for the key.</param>
    /// <returns>True if the key exists and the update could be performed, otherwise false.</returns>
    public bool UnSet(string key)
    {
        // if (RemoteKeyExists(key)) return false;
        var k = GetRemoteKey(key);
        SetKeyDirty(k);
        Underlying.Remove(k);
        return true;
    }

    /// <inheritdoc cref="Parse.ParseObject.Set"/>
    protected void Set<T>(T value, [CallerMemberName] string key = null)
    {
        //print type
        var k = GetRemoteKey(key);

        if (value != null)
        {
            var type = value.GetType();
            if (type.IsArray)
            {
                if (type.GetElementType()?.IsSubclassOf(typeof(DbObject)) ?? false)
                {
                    throw new Exception("Can't set value of relation, instead get the relation and set it");
                }
            }
            else if (value is DbObject)
            {
                SetKeyDirty(k);
                Underlying.Set(k, ((DbObject)(object)value).Underlying);
                return;
            }
            else if (typeof(T).IsEnum)
            {
                SetKeyDirty(k);
                Underlying.Set(k, (int)(object)value);
                return;
            }
        }
        SetKeyDirty(k);
        Underlying.Set(k, value);
    }


    // protected DbRelation<T> GetRelation<T>(bool readOnly = false, [CallerMemberName] string key = null) where T : DbObject
    // {
    //     if (!RemoteKeyExists(key)) throw new Exception("This key does not exist ["+key+"]");
    //     // if(string.Equals(ClassName, "_User", StringComparison.OrdinalIgnoreCase))
    //         // return new Relation<T>(Underlying.GetRelation<ParseUser>(GetRemoteKey(key)));
    //     return typeof(T) == typeof(DbUser) 
    //         ?  new DbRelation<T>(Underlying.GetRelation<ParseUser>(GetRemoteKey(key)), readOnly) 
    //         :  new DbRelation<T>(Underlying.GetRelation<ParseObject>(GetRemoteKey(key)), readOnly);
    // }

    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="G">The desired type for the value.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>The value for the given key, converted to the
    /// requested type, or <see cref="otherwise"/> if unsuccessful.</returns>
    public G GetValue<G>(string key, G otherwise) => !TryGet(key, out G res) ? otherwise : res;



    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="G">The desired type for the value.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>The value for the given key, converted to the
    /// requested type, or <see cref="otherwise"/> if unsuccessful.</returns>
    protected G GetOrElse<G>(G otherwise, [CallerMemberName] string key = null) => !TryGet(key, out G res) ? otherwise : res;

    protected DbFile GetFile([CallerMemberName] string key = null)
    {
        var file = GetOrElse<ParseFile>(null, key);
        return file == null ? null : new DbFile(file);
    }
    protected void SetFile(DbFile file, [CallerMemberName] string key = null)
    {
        Set(file?.Underlying, key);
    }

    /// <summary>
    /// Populates result with the value for the key, if possible. Otherwise default value for the type is returned.
    /// </summary>
    /// <typeparam name="G">The desired type for the value.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>true if successful.</returns>
    internal bool TryGet<G>(string key, out G result)
    {
        bool flag = false;
        G res = default(G);
        if (typeof(G).IsEnum)
        {
            flag = Underlying.TryGetValue(GetRemoteKey(key), out dynamic obj);
            if (flag) res = (G)obj;
        }
        else if (typeof(G) == typeof(DbUser))
        {
            flag = Underlying.TryGetValue(GetRemoteKey(key), out ParseUser obj);
            if (flag) res = (G)(object)Instantiate<DbUser>(obj);
        }
        else if (typeof(G).IsSubclassOf(typeof(DbObject)))
        {
            flag = Underlying.TryGetValue(GetRemoteKey(key), out ParseObject obj);
            if (flag)
            {
                var o = (DbObject)(object)InstantiateAny<G>();
                o.Underlying = obj;
                res = (G)(object)o;
            }
        }
        else if (typeof(System.Collections.IDictionary).IsAssignableFrom(typeof(G)) && typeof(G) != typeof(Dictionary<string, object>))
        {
            //use GetDictionaryOrEmpty instead 
            throw new Exception("Use GetDictionaryOrEmpty instead.");
        }
        else
        {
            flag = Underlying.TryGetValue(GetRemoteKey(key), out G obj);
            if (flag) res = obj;
        }

        result = res;
        return flag;
    }

    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="G">The desired type for the array.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>The value for the given key, converted to the
    /// requested type, or an empty array if unsuccessful.</returns>
    protected G[] GetArrayOrEmpty<G>([CallerMemberName] string key = null)
    {
        // Debugger.Print("Trying to get value of ["+key+"]");
        var a = GetArrayOrElse(key, Array.Empty<G>());
        // Debugger.PrintJson("Got elements: " + a.Length);
        return a;
    }

    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="G">The desired type for the array.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>The value for the given key, converted to the
    /// requested type, or <see cref="otherwise"/> if unsuccessful.</returns>
    internal G[] GetArrayOrElse<G>(string key, G[] otherwise)
    {
        if (typeof(G).IsSubclassOf(typeof(DbObject)))
            throw new Exception("A one to many relation can't be retrieved as an array");
        return TryGet(GetRemoteKey(key), out IList<G> res) ? res.ToArray() : otherwise;
    }


    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns>The value for the given key, empty if unsucessfull</returns>
    internal Dictionary<string, object> GetDictionaryOrEmpty(string key)
    {
        return TryGet(GetRemoteKey(key), out Dictionary<string, object> res) ? res : new Dictionary<string, object>();
    }

    #endregion

    public static int GetHashCode(string className, string uid) => HashCode.Combine(className, uid);

    //override hashcode and equals to use the uid and classname
    public override int GetHashCode() => GetHashCode(ClassName, Uid);
    public override bool Equals(object obj) => obj is DbObject other && other.Uid.Equals(Uid) && other.ClassName.Equals(ClassName);
}

public static class DbObjectExtensions
{
    #region Remote

    /// <inheritdoc cref="Parse.ParseObject.SaveAsync"/>
    /// If any fields are updated server side as a side effect that is reflected in the local object.
    /// Returns self reference for convenience, mutates original. 
    public static IPromise<T> SaveAsync<T>(this T o) where T : DbObject
        => Database.SaveObjectsAsync(new[] { o }).Then(_ => o);
    // {
    //     // transfer all dirty prop values to new object and then save that object instead
    //     // this is for complete control of which keys to save
    //
    //     var keys = o.GetDirtyKeys();
    //     // if (keys == null || keys.Count == 0) throw new Exception("asdads");
    //     // Debugger.Print("Saving object with dirty keys: " + JsonUtil.ToJson(keys));
    //     // Debugger.Print("Saving underlying with dirty keys: " + JsonUtil.ToJson(o.Underlying.Keys.Where(k => o.Underlying.IsKeyDirty(k))));
    //     if (o.IsNew) return o.Underlying.SaveAsync().ToPromise().Then(() => o);
    //     
    //     if (keys.Count == 0) return Promise<T>.Resolved(o);
    //
    //     var temp = Database.EmptyReference<T>(o.Uid);
    //     if(temp == null) throw new Exception("Can't save object that doesn't exist");
    //     foreach (var key in keys)
    //     {
    //         TransferPropValue(o, key, temp);
    //     }
    //     
    //     //set all keys to not dirty so that if anyone updates a key while waiting that update will be saved next time
    //     o.ClearDirtyKeys();
    //
    //     return temp.Underlying.SaveAsync().ToPromise()
    //         .Then(() => o)
    //         // .ThenKeepVal(u =>
    //         // {
    //         //     Debugger.Print("Saved object with dirty keys: " + JsonUtil.ToJson(o.GetDirtyKeys()));
    //         //     Debugger.Print("Saved underlying with dirty keys: " + JsonUtil.ToJson(o.Underlying.Keys.Where(k => o.Underlying.IsKeyDirty(k))));
    //         // })
    //         .OnCatch(e => keys.ForEach(o.SetKeyDirty)) //if save fails, set all saved keys to dirty again
    //         .OnCatch(e =>
    //         {
    //             if (e.Message == "Object not found.")
    //                 throw new Exception("User does not have permission to save this object.", e);
    //         });
    // }


    // public static IPromise<T> Get<T>(this T o) where T : DbObject => o.Underlying.SaveAsync().ToPromise().Then(() => o);

    /// <inheritdoc cref="Parse.ParseObject.DeleteAsync"/>
    /// Returns self reference for convenience, mutates original.
    /// <remarks>Note that removing a signed in user will invalidate the current session and make it impossible to sign in another user </remarks>
    public static IPromise DeleteAsync<T>(this T o) where T : DbObject
    {
        return Database.DeleteObjectsAsync(o);
    }

    /// <inheritdoc cref="Parse.ParseExtensions.FetchAsync{T}(T)"/>
    /// <summary>Returns self reference for convenience, mutates original. Removes all local changes.</summary>
    /// <remarks>Fetch from server.</remarks>
    public static IPromise<T> FetchAsync<T>(this T o) where T : DbObject =>
        Database.FetchObjectsAsync(new[] { o }).Then(_ => o);
    // {
    //     var keys = o.GetDirtyKeys();
    //     o.RevertLocalChanges();
    //     return o.Underlying.FetchAsync().ToPromise()
    //         .Then(a => o)
    //         .ThenKeepVal(obj => Database.AddToCache(ref obj))
    //         .OnCatch(e => keys.ForEach(o.SetKeyDirty));
    // }

    public static string ToJson(this object a) => JsonUtil.ToJson(a);

    /// <inheritdoc cref="Parse.ParseExtensions.FetchAsync{T}(T)"/>
    /// <summary>Returns self reference for convenience, mutates original.</summary>
    public static IPromise<T> FetchFieldsAsync<T>(this T rx, params string[] keys) where T : DbObject =>
        Database.FetchObjectsAsync(new[] { rx }, keys).Then(_ => rx);
    // {
    //     if (typeof(T) == typeof(DbUser))
    //     {
    //         return Database.GetUserAsync(rx.Uid, keys).Then(user =>
    //         {
    //             foreach (var key in keys)
    //             {
    //                 if (user.TransferPropValue(key, rx as DbUser))
    //                 {
    //                     rx.SetKeyNotDirty(key);
    //                 }
    //             }
    //             return rx;
    //         });
    //     }
    //
    //     return Database.GetObjectAsync<T>(rx.Uid, keys).Then(obj =>
    //     {
    //         foreach (var k in keys)
    //         {
    //             obj.TransferPropValue(k, rx);
    //         }
    //         return rx;
    //     });
    //     // o.Underlying.FetchAsync().ToPromise().Then(a => o);   
    // }

    /// <summary>
    /// Populates result with the value for the key, if possible. The receiver will mark the key as dirty.
    /// </summary>
    /// <typeparam name="G">The desired type for the array.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <returns> True if successful.</returns>
    [Obsolete("This method will destroy atomicity, if not used very carefully")]
    internal static bool TransferPropValue<T>(this T tx, string key, T rx, bool markAsDirty = true) where T : DbObject
    {
        if (rx == null) throw new Exception("Can't transfer value to null object");
        if (tx == null) throw new Exception("Can't transfer value from null object");
        if (string.IsNullOrWhiteSpace(key)) throw new Exception("Can't transfer prop; null or empty key");

        if (string.Equals(tx.GetRemoteKey(key), "objectId")) return false;

        if (!tx.TryGet(key, out dynamic res)) return false;
        if (!rx.SetIfDifferent<dynamic>(key, res)) return false;
        if (!markAsDirty) rx.SetKeyNotDirty(key);
        return true;
    }

    /// <summary>
    /// Used to make savable copy without keys that aren't considered dirty by wrapper.
    /// </summary>
    [Obsolete("Not done yet.")]
    internal static T CreateDeepCopy<T>(this T tx) where T : DbObject
    {
        var rx = Database.EmptyReference<T>(tx.Uid);
        if (rx == null) throw new Exception("Can't transfer value to null object");
        if (tx == null) throw new Exception("Can't transfer value from null object");

        var keys = tx.GetDirtyKeys();
        foreach (var (k, op) in tx.Underlying.CurrentOperations)
        {
            if (!keys.Contains(k)) continue;
            var t = op.GetType();
            if (op is ParseSetOperation set) rx.Underlying.Set(k, set.Value);
            else if (op is ParseIncrementOperation inc) rx.Increment(k, (double)inc.Amount);
            else if (op is ParseAddOperation add) rx.ArrayAppend(k, add.Objects);
            else if (op is ParseRemoveOperation rem) rx.ArrayRemove(k, rem.Objects);
            else if (op is ParseAddUniqueOperation addU) rx.ArrayAppendUnique(k, addU.Objects);
            else if (op is ParseDeleteOperation del)
            {
                //Dont know what to do
            }
            else if (op is ParseRelationOperation rel)
            {
                //Dont know what to do
            }
        }
        return rx;
    }




    /// <inheritdoc cref="Parse.ParseExtensions.FetchIfNeededAsync{T}(T)"/>
    /// Returns self reference for convenience, mutates original.
    /// <remarks> Only fetches if none of the Object's fields are fetched. Use FetchFieldsAsync or FetchAsync to fetch the rest of the object.</remarks>
    public static IPromise<T> FetchIfNeededAsync<T>(this T o) where T : DbObject =>
        !o.IsStale || !o.HasData ? o.FetchAsync() : Promise<T>.Resolved(o);

    #endregion



    #region Atomic
    private static T RunReturn<T>(T o, string key, Action a) where T : DbObject
    {
        a();
        // Debugger.Print("Set " + Key(o, key) + " dirty");
        o.SetKeyDirty(Key(o, key));
        return o;
    }
    private static string Key<T>(T o, string key) where T : DbObject => o.GetRemoteKey(key);

    /// <inheritdoc cref="Parse.ParseObject.Increment(string, double)"/> 
    public static T Increment<T>(this T o, string key, double amount = 1) where T : DbObject =>
        RunReturn(o, key, () => o.Underlying.Increment(Key(o, key), amount));

    /// <inheritdoc cref="Parse.ParseObject.Increment(string, long)"/> 
    public static T Increment<T>(this T o, string key, long amount = 1) where T : DbObject =>
        RunReturn(o, key, () => o.Underlying.Increment(Key(o, key), amount));

    /// <inheritdoc cref="Parse.ParseObject.AddRangeToList(string,IEnumerable{T})"/> 
    public static T ArrayAppend<T>(this T o, string key, params object[] values) where T : DbObject =>
        RunReturn(o, key, () => o.Underlying.AddRangeToList(Key(o, key), values));

    /// <inheritdoc cref="Parse.ParseObject.AddRangeUniqueToList(string, IEnumerable{T})"/> 
    public static T ArrayAppendUnique<T>(this T o, string key, params object[] values) where T : DbObject =>
        RunReturn(o, key, () => o.Underlying.AddRangeUniqueToList(Key(o, key), values));

    /// <inheritdoc cref="Parse.ParseObject.RemoveAllFromList(string,IEnumerable{T})"/> 
    public static T ArrayRemove<T>(this T o, string key, params object[] values) where T : DbObject =>
        RunReturn(o, key, () => o.Underlying.RemoveAllFromList(Key(o, key), values));
    #endregion

    public static IPromise DeleteObjectsAsync<T>(this IEnumerable<T> objects) where T : DbObject =>
        Database.DeleteObjectsAsync(objects.ToArray());

    public static IPromise<List<T>> SaveObjectsAsync<T>(this IEnumerable<T> objects) where T : DbObject =>
        Database.SaveObjectsAsync(objects.ToArray());

}