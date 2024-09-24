using System.Diagnostics;
using Project.Util;
using RSG;

namespace Project.ParseORM.Data;

//TODO ADD OPTIONS FOR SORTING AND FILTERING
[Obsolete("Use user.Matches instead, this allows for reordering and filtering by using DbPagedQuery.")]
public class DbMatchHistory
{
    private DbUser _userRef;
    public DbMatchHistory(DbUser user)
    {
        _userRef = user;
    }
    
    private readonly SortedSet<DbMatch> _matches = new(Comparer<DbMatch>.Create((a1, a2) =>
    {
        Debug.Assert(a1.CreatedAt != null, "a1.CreatedAt != null");
        Debug.Assert(a2.CreatedAt != null, "a2.CreatedAt != null");
        return a1.CreatedAt.Value.CompareTo(a2.CreatedAt.Value);
    }));

    private List<DbMatch> FilterNewMessages(IEnumerable<DbMatch> m) => 
        m.Where(match => !_matches.Contains(match)).ToList();

    
    /// <returns> All locally cached matches. </returns>
    public List<DbMatch> GetAllDownloaded() => _matches.ToList();
    
    /// <summary>
    /// Clear local cache. 
    /// </summary>
    public DbMatchHistory Clear()
    {
        _matches.Clear();
        return this;
    }
    
    /// <summary>
    /// Fetches new matches from the server and adds them to the local cache.
    /// </summary>
    /// <returns> All locally cached Matches. Matches that aren't included here should not be displayed. </returns>
    public IPromise<List<DbMatch>> FetchNew() =>
        new DbQuery<DbMatch>()
            .WhereEqualTo("players", _userRef.Uid)
            .WhereGreaterThan("createdAt", _matches.Count > 0 ?  _matches.First().CreatedAt : DateTime.UnixEpoch) //fetches all when list is empty, not ok
            .OrderByDescending("createdAt")
            .Limit(10)
            .FindAsync()
            .Then(m =>
            {
                var filtered = FilterNewMessages(m.Reverse());
                if (m.Length == 10) //if there are 10 messages, there might be more
                    _matches.Clear(); //remove old messages so that we can fetch the new ones that are between the old and the new                    

                foreach (var message in m) _matches.Add(message);
                return GetAllDownloaded();
            });

    /// <summary>
    /// Fetches older matches from the server and adds them to the local cache. 
    /// </summary>
    /// <returns> Matches that hasn't been downloaded before. </returns>
    public IPromise<List<DbMatch>> FetchOlder() => 
        new DbQuery<DbMatch>()
            .WhereEqualTo("players", _userRef.Uid)
            .WhereLessThan("createdAt", _matches.Count > 0 ?  _matches.First().CreatedAt : DateTime.Now)
            .OrderByDescending("createdAt")
            .Limit(10)
            .FindAsync()
            .Then(m =>
            {
                var filtered = FilterNewMessages(m.Reverse());
                foreach (var message in m) _matches.Add(message);
                return filtered;
            });
}