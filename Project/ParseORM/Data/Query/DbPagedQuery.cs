using System;
using System.ComponentModel;
using Project.ParseORM.Data;
using RSG;
using Project.Util;
namespace Project.ParseORM;



///<summary>Allows for queries to be paginated with coherent cache and runtime customizable filters and ordering. Should be used everywhere where collections are shown to the user, especially if they can be large. </summary>
public class DbPagedQuery<T> where T : DbObject
{
  private DbQuery<T> query;
  private DbQuery<T> orginialQuery;

  private string queryOrderKey;
  private Func<T, object> queryOrderValue;
  private bool ascending = true;
  private IComparer<object> innerLocalOrder;
  private IComparer<T> localOrder;

  private Action<T[]> onItemsFetched = (T[] items) => { };

  private SortedSet<T> _items;
  private bool reverseInner;

  private static DbPagedQueryBuilder<T> Builder() => new DbPagedQueryBuilder<T>();

  /// <summary>Builds a new query if one does not exist, otherwise returns the existing query <paramref name="queryRef"/>. </summary>
  public static DbPagedQuery<T> LazyBuild(ref DbPagedQuery<T> queryRef, Func<DbPagedQueryBuilder<T>, DbPagedQueryBuilder<T>> builderModifier)
  {
    if (queryRef == null)
    {
      queryRef = builderModifier(Builder()).Build();
    }
    return queryRef;
  }

  /// _clientLocalOrdering is set to Comparable.Default if not provided
  internal DbPagedQuery(DbQuery<T> _baseQuery, string _queryOrderKey, Func<T, object> _queryOrderValue, bool _ascending, IComparer<object> _clientLocalOrdering, Action<T[]> _onItemFetched, bool _reverseInner)
  {
    this.query = _baseQuery;
    this.orginialQuery = _baseQuery;

    this.queryOrderKey = DbObject.GetRemoteKey<T>(_queryOrderKey);
    this.queryOrderValue = _queryOrderValue;
    this.ascending = _ascending;
    this.reverseInner = _reverseInner; 
    
    if(_clientLocalOrdering == null) // SET TO DEFAULT IF NOT PROVIDED, should be able to handle all types implementing Comparable  
      _clientLocalOrdering = ascending ? Comparer<object>.Default : Comparer<object>.Create((a, b) => Comparer<object>.Default.Compare(b, a));

    innerLocalOrder = _clientLocalOrdering; //store so that we can create a new query later
    // wrap the ordering to compare Ts
    localOrder = Comparer<T>.Create((a, b) => _clientLocalOrdering.Compare(_queryOrderValue(a), _queryOrderValue(b)));
    if(reverseInner) 
      localOrder = Comparer<T>.Create((a, b) => _clientLocalOrdering.Compare(_queryOrderValue(b), _queryOrderValue(a)));
    this._items = new(localOrder);

    this.onItemsFetched = _onItemFetched;
  }


  /// <summary>Set a new query ordering.</summary>
  /// <returns>A new paginated query with the new ordering. Original is not modified. </returns>
  /// <param name="_queryOrderKey">The field name to order by.</param>
  /// <param name="_queryOrderValue">A function that returns the value of the field to order by. For automatic ordering of local cache.</param>
  /// <param name="_ascending">If true, the order will be ascending. If false, the order will be descending.</param>
  /// <param name="_clientLocalOrdering">The comparer to use for ordering locally. If null, the default comparer will be used.</param>
  public DbPagedQuery<T> WithQueryOrdering(string _queryOrderKey, Func<T, object> _queryOrderValue, IComparer<object> _clientLocalOrdering = null, bool _ascending = true)
  {
    return new DbPagedQuery<T>(query, _queryOrderKey, _queryOrderValue, _ascending, _clientLocalOrdering, onItemsFetched, reverseInner);
  }

  /// <summary>Add query constraints.</summary>
  /// <returns>A new paginated query with the new constraints. Original is not modified. </returns>
  public DbPagedQuery<T> WithQueryConstraints(Func<DbQuery<T>, DbQuery<T>> modifyQuery) =>
    // query.WhereEqualTo ex returns a new query and does not modify. Therefore modifyQuery should not modify the original query.
    // could be the same query if modifyQuery does nothing but return query. does not matter.
    new DbPagedQuery<T>(modifyQuery(query), queryOrderKey, queryOrderValue, ascending, innerLocalOrder, onItemsFetched, reverseInner);


  /// <summary> Clears local cache. </summary>
  public DbPagedQuery<T> ClearFetchedItemsCache()
  {
    _items.Clear();
    return this;
  }



  /// <returns> All locally cached objects. </returns>
  public List<T> GetAllFetchedItems() => _items.ToList();




  /// <summary>
  /// Fetches greater objects from the server and adds them to the local cache.
  /// </summary>
  /// <remarks> ex. DbMessage, Greater in this case means newer since messages are ordered by "createdAt". </remarks>
  /// <returns> All locally cached objects. Objects that aren't included here should not be displayed (if the collection is updated regurarly) on screen since that could break correctness of pagination. </returns>
  public IPromise<List<T>> FetchGreaterItemsAsync(int max_count = 10) =>
      WithOrdering(FetchDirection.Greater)
        .Limit(max_count)
        .FindAsync()
        .Then(items =>
        {
          // Debugger.Print(queryOrderKey);
          // Debugger.Print($"items: {items.Length}");
          // Debugger.PrintJson(items.Select(x => x.GetValue<string>("text", "")).ToArray());
          var filtered = FilterNewItems(items.Reverse());

          // Debugger.PrintJson(filtered);
          if (items.Length == max_count) //if there are max_count messages, there might be more
          {
            _items.Clear(); //remove old messages so that we can fetch the new ones that are between the old and the new                    
          }

          foreach (var item in items)
          {
            // Debugger.Print(item.GetValue("chat", ""));
            _items.Add(item);
            // item.NotifyReceived();
            item.NoteFetched(); //prob dont need this
          }
          onItemsFetched(items);


          return GetAllFetchedItems();
        });




  /// <summary>
  /// Fetches lesser objects from the server and adds them to the local cache. 
  /// </summary>
  /// <remarks> ex. DbMessage, Lesser in this case means older since messages are ordered by "createdAt". </remarks>
  /// <returns> All locally cached objects. Objects that aren't included here should not be displayed (if the collection is updated regurarly) on screen since that could break correctness of pagination. </returns>
  public IPromise<List<T>> FetchLesserItemsAsync(int max_count = 10) =>
      WithOrdering(FetchDirection.Lesser)
          .Limit(max_count)
          .FindAsync()
          .Then(items =>
          {
            var filtered = FilterNewItems(items.Reverse());
            foreach (var item in items)
            {
              _items.Add(item);
              item.NoteFetched(); //prob dont need this
              // message.NotifyReceived();
            }
            onItemsFetched(items);

            return GetAllFetchedItems();
          });


  /// <summary>
  /// Adds a object to the local cache.
  /// </summary>
  /// <remarks> Does not save the object to the server, this has to be done separately. </remarks>
  public void AddFetchedItem(T item) => _items.Add(item);
  public void AddFetchedItems(IEnumerable<T> items)
  {
    foreach (var item in items)
      AddFetchedItem(item);
  }











  //private methods

  private List<T> FilterNewItems(IEnumerable<T> m) =>
      m.Where(item => !_items.Contains(item)).ToList();

  enum FetchDirection
  {
    Greater,
    Lesser
  }
  private DbQuery<T> WithOrdering(FetchDirection dir)
  {
    DbQuery<T> q = query;
    if (_items.Count > 0)
    {
      // Debugger.Print("Adding query constraint based on existing items");
      // Debugger.PrintJson(_items.Select(x => x.GetValue<string>("text", "")).ToArray());
      // Debugger.Print(queryOrderKey);
      // Debugger.Print(dir);
      if (dir == FetchDirection.Greater)
        q = q.WhereGreaterThan(queryOrderKey, queryOrderValue(!reverseInner ? _items.Last() : _items.First()));
      else
        q = q.WhereLessThan(queryOrderKey, queryOrderValue(reverseInner ? _items.First() : _items.Last()));
    }

    if (ascending)
    {
      // Debugger.Print("Ascending");
      q = q.OrderBy(queryOrderKey);
    }
    else
    {
      // Debugger.Print("Descending");
      q = q.OrderByDescending(queryOrderKey);
    }

    return q;
  }
}



public class DbPagedQueryBuilder<T> where T : DbObject
{

  // FILTERING
  private DbQuery<T> query;

  // ORDERING
  private string queryOrderKey;
  private Func<T, object> queryOrderValue;
  private bool ascending = true;
  private IComparer<object> internalQueryOrder;

  // EVENT
  private Action<T[]> fetchedAction = (T[] item) => { };
  private bool reverseInner;

  public DbPagedQueryBuilder()
  {
    query = new DbQuery<T>();
  }

  ///<summary>Modify/Add constraints to the query.</summary>
  public DbPagedQueryBuilder<T> AddQueryConstraint(Func<DbQuery<T>, DbQuery<T>> modifyQuery)
  {
    query = modifyQuery(query);
    return this;
  }


  ///<summary>Add an equality constraint to the query.</summary>
  ///<param name="key">The key/(the name of the field) to be constrained.</param>
  ///<param name="value">The value that the key must equal.</param>
  public DbPagedQueryBuilder<T> WhereEqualTo(string key, object value)
  {
    query = query.WhereEqualTo(key, value);
    return this;
  }

  /// <summary>Define a remote and local cache ordering of the items.</summary>
  /// <param name="_orderingKey">The field name to order by remotely.</param>
  /// <param name="_orderingValue">A function to get the value of the field for automatic local ordering.</param>
  /// <param name="_ascending">Whether the ordering is ascending or descending.</param>
  /// <param name="_customInternalComparer">A custom comparer to use for local ordering if the automatic local ordering is not correct.</param>
  public DbPagedQueryBuilder<T> DefineOrdering(string _orderingKey, Func<T, object> _orderingValue, bool _ascending = true, IComparer<object> _customInternalComparer = null, bool _reverseInner = false)
  {
    queryOrderKey = _orderingKey;
    queryOrderValue = _orderingValue;
    ascending = _ascending;
    internalQueryOrder = _customInternalComparer;
    reverseInner = _reverseInner;

    return this;
  }

  ///<summary>Runs on each now batch of items that is fetched.</summary>
  public DbPagedQueryBuilder<T> OnItemsFetched(Action<T[]> action)
  {
    fetchedAction = action;
    return this;
  }


  public DbPagedQuery<T> Build()
  {
    if (queryOrderKey == null) throw new Exception("OrderBy must be set");
    if (queryOrderValue == null) throw new Exception("OrderingValue must be set");
    if (query == null) throw new Exception("Query must be set");
    return new DbPagedQuery<T>(query, queryOrderKey, queryOrderValue, ascending, internalQueryOrder, fetchedAction, reverseInner);
  }
}