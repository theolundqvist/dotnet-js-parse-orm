using System;
using System.Collections;
using Project.ParseORM.Data;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM;

public class DbQuery<T> where T : DbObject
{
  private readonly string _className = DbObject.GetClassname<T>();
  private bool IsUserQ => typeof(T) == typeof(DbUser);
  private readonly Dictionary<string, string> _keyMapper = DbObject.GetKeyMapper<T>();
  private ParseQuery<ParseObject> _q;
  private ParseQuery<ParseUser> _uq;
  private bool CacheEnabled = true;
  private string GetRemoteKey(string key) => _keyMapper.ContainsKey(key) ? _keyMapper[key] : key;

  private static dynamic GetValue<G>(G obj)
  {
    // Debugger.Print($"type: {obj.GetType()}, basetype: {obj.GetType().BaseType}");
    if (obj is DbObject dbObject)
      return dbObject.Underlying;
    if (typeof(G).IsEnum)
    {
      // Debugger.Print($"enum: {(int)(object)obj}"); 
      return (int)(object)obj;
    }
    return obj;
  }

  private static dynamic GetArray<G>(IEnumerable<G> obj) =>
      obj.GetType().GetElementType()?.IsSubclassOf(typeof(DbObject)) == true ? obj.Select(x => (x as DbObject)?.Underlying) : obj;

  internal DbQuery(ParseQuery<ParseUser> puq)
  {
    if (IsUserQ)
    {
      _uq = puq;
      _uq = DbObject.LimitQueryToSubtype(this)._uq;
    }
    else throw new Exception("Query on ParseUser must have internal classname '_User'");
  }

  internal DbQuery(ParseQuery<ParseObject> puq)
  {
    if (!IsUserQ) _q = puq;
    else throw new Exception("Query on ParseObject may not have internal classname '_User'");
  }
  public DbQuery()
  {
    if (!IsUserQ)
    {
      _q = Database.Client.GetQuery(_className); //TODO make DbObject have a static method to get a query of the object, DbSubtypes would have to override the method and add a where Type == "BridgeProblem"
      _q = DbObject.LimitQueryToSubtype(this)._q;
    }
    else _uq = Database.Client.GetUserQuery();
  }

  private DbQuery<T> WithConstraint(Func<Parse.ParseQuery<Parse.ParseObject>> modifiedQuery, Func<Parse.ParseQuery<Parse.ParseUser>> modifiedUserQuery)
  {
    if (IsUserQ) return new DbQuery<T>(modifiedUserQuery());
    else return new DbQuery<T>(modifiedQuery());
  }

  public DbQuery<T> Or(params DbQuery<T>[] queries) => WithConstraint(
      () => Database.Client.ConstructOrQuery(_q, queries.Select(x => x._q).ToArray()),
      () => Database.Client.ConstructOrQuery(_uq, queries.Select(x => x._uq).ToArray()));

  #region Run Query

  /// <inheritdoc cref="ParseQuery{T}.FindAsync()"/>
  public IPromise<T[]> FindAsync()
      => Database.ThrowIfNotSignedIn(() => IsUserQ ? _uq.FindAsync().ToPromise<T>() : _q.FindAsync().ToPromise<T>())
          .Then(t => Database.AddRangeToCache(t).ToArray());

  /// <inheritdoc cref="ParseQuery{T}.FirstAsync()"/>
  public IPromise<T> FirstAsync()
      => Database.ThrowIfNotSignedIn(() => IsUserQ ? _uq.FirstAsync().ToPromise<T>() : _q.FirstAsync().ToPromise<T>())
          .Then(t =>
          {
            Database.AddToCache(ref t);
            return t;
          });

  /// <inheritdoc cref="ParseQuery{T}.CountAsync()"/>
  public IPromise<int> CountAsync()
      => Database.ThrowIfNotSignedIn(() => IsUserQ ? _uq.CountAsync().ToPromise() : _q.CountAsync().ToPromise());

  /// <inheritdoc cref="ParseQuery{T}.GetAsync(string)"/>
  public IPromise<T> GetObjectByIdAsync(string uid)
      => Database.ThrowIfNotSignedIn(() =>
      {
        if (CacheEnabled && Database.Cache.ContainsAndFresh<T>(uid)) return Database.ReferenceAsync<T>(uid);
        
        return IsUserQ ? _uq.GetAsync(uid).ToPromise<T>() : _q.GetAsync(uid).ToPromise<T>();
      }).Then(t =>
      {
        Database.AddToCache(ref t);
        return t;
      });

  #endregion

  #region Equality

  /// <inheritdoc cref="ParseQuery{T}.WhereEqualTo"/> 
  public DbQuery<T> WhereEqualTo<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereEqualTo(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereEqualTo(GetRemoteKey(key), GetValue(value)));

  /// <inheritdoc cref="ParseQuery{T}.WhereNotEqualTo"/> 
  public DbQuery<T> WhereNotEqualTo<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereNotEqualTo(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereNotEqualTo(GetRemoteKey(key), GetValue(value)));

  /// <inheritdoc cref="ParseQuery{T}.WhereLessThan"/> 
  public DbQuery<T> WhereLessThan<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereLessThan(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereLessThan(GetRemoteKey(key), GetValue(value)));

  /// <inheritdoc cref="ParseQuery{T}.WhereGreaterThan"/> 
  public DbQuery<T> WhereGreaterThan<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereGreaterThan(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereGreaterThan(GetRemoteKey(key), GetValue(value)));

  /// <inheritdoc cref="ParseQuery{T}.WhereGreaterThanOrEqualTo"/> 
  public DbQuery<T> WhereGreaterThanOrEqualTo<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereGreaterThanOrEqualTo(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereGreaterThanOrEqualTo(GetRemoteKey(key), GetValue(value)));


  /// <inheritdoc cref="ParseQuery{T}.WhereLessThanOrEqualTo"/> 
  public DbQuery<T> WhereLessThanOrEqualTo<G>(string key, G value)
      => WithConstraint(
          () => _q.WhereLessThanOrEqualTo(GetRemoteKey(key), GetValue(value)),
          () => _uq.WhereLessThanOrEqualTo(GetRemoteKey(key), GetValue(value)));



  #endregion Equality

  #region Convenience

  /// <inheritdoc cref="ParseQuery{T}.Limit"/> 
  public DbQuery<T> Limit(int n)
      => WithConstraint(
          () => _q.Limit(n),
          () => _uq.Limit(n));

  /// <inheritdoc cref="ParseQuery{T}.Select"/> 
  public DbQuery<T> Select(string key)
      => WithConstraint(
          () => _q.Select(GetRemoteKey(key)),
          () => _uq.Select(GetRemoteKey(key)));


  /// <summary>
  /// Restrict the fields of returned ParseObjects to only include the provided keys. If this is called multiple times, then all of the keys specified in each of the calls will be included.
  /// </summary>
  /// <param name="keys"> The keys that should be included. If Empty, all keys will be fetched. </param>
  /// <returns> A new query with the additional constraints. </returns>
  public DbQuery<T> SelectMultiple(params string[] keys)
  {
    var _q = this._q;
    var _uq = this._uq;
    return WithConstraint(
        () =>
            {keys.ToList().ForEach(k =>
            {

                _q = _q.Select(GetRemoteKey(k));
            });
            return _q;
            },
        () =>
            {keys.ToList().ForEach(k =>
            {

                _uq = _uq.Select(GetRemoteKey(k));
            });
            return _uq;
            });

  }
  /// <inheritdoc cref="ParseQuery{T}.Include"/> 
  public DbQuery<T> Include(string key)
      => WithConstraint(
          () => _q.Include(GetRemoteKey(key)),
          () => _uq.Include(GetRemoteKey(key)));

  /// <inheritdoc cref="ParseQuery{T}.Skip"/> 
  public DbQuery<T> Skip(int n)
      => WithConstraint(
          () => _q.Skip(n),
          () => _uq.Skip(n));

  #endregion

  #region Order

  /// <inheritdoc cref="ParseQuery{T}.OrderBy"/> 
  public DbQuery<T> OrderBy(string key)
      => WithConstraint(
          () => _q.OrderBy(GetRemoteKey(key)),
          () => _uq.OrderBy(GetRemoteKey(key)));

  /// <inheritdoc cref="ParseQuery{T}.OrderByDescending"/> 
  public DbQuery<T> OrderByDescending(string key)
      => WithConstraint(
          () => _q.OrderByDescending(GetRemoteKey(key)),
          () => _uq.OrderByDescending(GetRemoteKey(key)));

  /// <inheritdoc cref="ParseQuery{T}.ThenBy"/> 
  public DbQuery<T> ThenBy(string key)
      => WithConstraint(
          () => _q.ThenBy(GetRemoteKey(key)),
          () => _uq.ThenBy(GetRemoteKey(key)));

  /// <inheritdoc cref="ParseQuery{T}.ThenByDescending"/> 
  public DbQuery<T> ThenByDescending(string key)
      => WithConstraint(
          () => _q.ThenByDescending(GetRemoteKey(key)),
          () => _uq.ThenByDescending(GetRemoteKey(key)));


  #endregion

  #region String


  /// <inheritdoc cref="ParseQuery{T}.WhereContains"/> 
  public DbQuery<T> WhereContains(string key, string value)
      => WithConstraint(
          () => _q.WhereContains(GetRemoteKey(key), value),
          () => _uq.WhereContains(GetRemoteKey(key), value));

  /// <inheritdoc cref="ParseQuery{T}.WhereMatches(string, string, string)"/> 
  public DbQuery<T> WhereMatches(string key, string value, string modifiers = null)
      => WithConstraint(
          () => _q.WhereMatches(GetRemoteKey(key), value, modifiers),
          () => _uq.WhereMatches(GetRemoteKey(key), value, modifiers));


  /// <inheritdoc cref="ParseQuery{T}.WhereEndsWith"/> 
  public DbQuery<T> WhereEndsWith(string key, string value)
      => WithConstraint(
          () => _q.WhereEndsWith(GetRemoteKey(key), value),
          () => _uq.WhereEndsWith(GetRemoteKey(key), value));

  /// <inheritdoc cref="ParseQuery{T}.WhereStartsWith"/> 
  public DbQuery<T> WhereStartsWith(string key, string value)
      => WithConstraint(
          () => _q.WhereStartsWith(GetRemoteKey(key), value),
          () => _uq.WhereStartsWith(GetRemoteKey(key), value));


  #endregion

  #region List

  /// <inheritdoc cref="ParseQuery{T}.WhereContainedIn{T}"/> 
  public DbQuery<T> WhereContainedIn<G>(string key, IEnumerable<G> values) =>
      WhereContainedIn(key, values.ToArray());

  /// <inheritdoc cref="ParseQuery{T}.WhereContainedIn{T}"/> 
  public DbQuery<T> WhereContainedIn<G>(string key, params G[] values)
      => WithConstraint(
          () => _q.WhereContainedIn(GetRemoteKey(key), GetArray(values)),
          () => _uq.WhereContainedIn(GetRemoteKey(key), GetArray(values)));


  /// <inheritdoc cref="ParseQuery{T}.WhereContainsAll{T}"/> 
  public DbQuery<T> WhereContainsAll<G>(string key, IEnumerable<G> values) =>
      WhereContainsAll(key, values.ToArray());

  /// <inheritdoc cref="ParseQuery{T}.WhereContainsAll{T}"/> 
  public DbQuery<T> WhereContainsAll<G>(string key, params G[] values)
      => WithConstraint(
          () => _q.WhereContainsAll(GetRemoteKey(key), GetArray(values)),
          () => _uq.WhereContainsAll(GetRemoteKey(key), GetArray(values)));



  /// <inheritdoc cref="ParseQuery{T}.WhereNotContainedIn{T}"/> 
  public DbQuery<T> WhereNotContainedIn<G>(string key, IEnumerable<G> values) =>
      WhereNotContainedIn(key, values.ToArray());

  /// <inheritdoc cref="ParseQuery{T}.WhereNotContainedIn{T}"/> 
  public DbQuery<T> WhereNotContainedIn<G>(string key, params G[] values)
      => WithConstraint(
          () => _q.WhereNotContainedIn(GetRemoteKey(key), GetArray(values)),
          () => _uq.WhereNotContainedIn(GetRemoteKey(key), GetArray(values)));


  #endregion

  #region Cache

  public DbQuery<T> BypassCache()
  {
    CacheEnabled = false;
    return this;
  }

  #endregion



  // /// <inheritdoc cref="ParseQuery{T}.WhereExists"/> 
  // public Query<T> WhereExists(string key, string value)
  //     => Self(
  //         () => _q.WhereExists(GetRemoteKey(key)),
  //         () => _uq.WhereExists(GetRemoteKey(key))); 

}