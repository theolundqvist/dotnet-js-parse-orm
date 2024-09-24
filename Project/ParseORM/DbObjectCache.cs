using Project.ParseORM.Data;
using Project.Util;
using RSG;

namespace Project.ParseORM;

public class DbObjectCache
{
  public DbObjectCache(bool enabled)
  {
    Enabled = enabled;
  }

  /// <summary> Defined whether the cache is currently activated or not. </summary>
  public bool Enabled = false;

  private readonly Dictionary<int, DbObject> _cache = new();


  private static int GetKey<T>(T obj) where T : DbObject => obj.GetHashCode();

  private static int GetKey<T>(string uid) where T : DbObject => DbObject.GetHashCode(DbObject.GetClassname<T>(), uid);

  public int ObjectCount() => _cache.Count;
  public int StaleCount() => _cache.Count(x => x.Value.IsStale);

  /// <summary> Remove all object references from cache. </summary>
  public void Clear()
  {
    //iterate all objectes in cache

    _cache.Clear();
  }

  /// <summary> Clear the data of all the object in cache. Keep only ClassName and Uid so that object can be fetched. </summary>
  public void DisposeAll()
  {
    foreach (var pair in _cache)
    {
      if (pair.Value is DbChat chat)
      {
        // Debugger.Print("Chat messages cleared: " + chat.GetAllDownloadedMessages().Count);
        chat.Clear();
      }
      pair.Value.SetDefaultValues();
      pair.Value.NoteStale();
    }
    // _cache.Clear(); //don't clear cache, just mark all objects as stale and wipe their content.
  }

  /// <summary>
  /// Returns true if the object was found in the cache, stale or not. see <see cref="DbObject.IsStale"/>
  /// </summary>
  public bool Contains<T>(string uid) where T : DbObject
  {
    var k = GetKey<T>(uid);
    if (!_cache.ContainsKey(k)) return false;
    // if (_cache[k].IsStale) {
    //     // Debugger.Print("Cache hit but stale: " + id);
    //     return true;
    // }
    // Debugger.Print("Cache hit: " + uid);
    return true;
  }

  /// <summary>
  /// Returns true if the object was found in the cache and was not stale. see <see cref="DbObject.IsStale"/>
  /// </summary>
  public bool ContainsAndFresh<T>(string uid) where T : DbObject
  {
    var k = GetKey<T>(uid);
    if (!_cache.ContainsKey(k)) return false;
    if (_cache[k].IsStale)
    {
      // Debugger.Print("Cache hit but stale: " + id);
      return false;
    }
    // Debugger.Print("Cache hit: " + uid);
    return true;
  }

  /// <summary> Returns true if the object could be found in the local cache. </summary>
  public bool TryGet<T>(string uid, out T obj) where T : DbObject
  {
    if (Enabled && Contains<T>(uid))
    {
      obj = (T)_cache[GetKey<T>(uid)];
      return true;
    }
    obj = null;
    return false;
  }

  /// <exception cref="Exception">Object not found in cache.</exception>
  public T Get<T>(string uid) where T : DbObject => (T)_cache[GetKey<T>(uid)];

  /// <summary> Add object to cache or update the already existing data. </summary>
  public void AddToCache<T>(ref T obj) where T : DbObject
  {
    if (!Enabled) return;
    if (obj.IsNew) throw new Exception("Can't add unsaved object to cache.");
    var key = GetKey(obj);
    if (_cache.ContainsKey(key))
    {
      //transfer all contained props to the cached object
      var rx = _cache[key];
      var o = obj;
      rx.Underlying = obj.Underlying;
      rx.ClearDirtyKeys();
      // obj.GetRemoteKeys().ForEach(k => o.TransferPropValue(k, rx));
      rx.NoteFetched(); //mark as fetched, so it won't be fetched again for TimeToLive
      obj = (T)_cache[key];
    }
    else
    {
      //Debugger.Print("adding " + key + " to cache: " + JsonUtil.ToJson(_cache.Keys));
      _cache.Add(key, obj);
      obj.NoteFetched();
    }
  }

  /// <summary> Remove object from cache if it exists. </summary>
  public void RemoveFromCache<T>(ref T obj) where T : DbObject
  {
    if (!Enabled) return;
    if (obj.IsNew) throw new Exception("Can't remove unsaved object from cache.");
    var key = GetKey(obj);
    if (!_cache.ContainsKey(key)) return;
    _cache.Remove(key);
    obj = null;
  }

  /// <summary> Remove objects from cache if they exists. </summary>
  public void RemoveRangeFromCache<T>(IEnumerable<T> objs) where T : DbObject =>
      objs.ToList().ForEach(a => RemoveFromCache(ref a));

  /// <summary> Add objects to cache. <see cref="DbObjectCache.AddToCache{T}"/> </summary>
  public List<T> AddRangeToCache<T>(IEnumerable<T> objs) where T : DbObject =>
      objs.Select(a =>
      {
        AddToCache(ref a);
        return a;
      }).ToList();
}
