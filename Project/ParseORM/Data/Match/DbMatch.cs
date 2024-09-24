using Parse;
using RSG;
using Project.Util;

namespace Project.ParseORM.Data;
public class DbMatch : DbObject
{
    protected override Dictionary<string, string> KeyMaps =>
        new()
        {
            {"users", "players"},
            {"players", "players"},
            {"playerRefs", "players"},
            
            {"tables", "tables"},
            {"tableRefs", "tables"}
        };
    public override string ClassName => "Match";

    //måste finnas, kan vara private
    
    /// Start time will be set to DateTime.Now 
    public DbMatch()
    {
        StartTime = DateTime.Now;
    }
    
    ///You will have to set end time manually later, match can be created without an end time.
    public DbMatch(DateTime start)
    {
        StartTime = start;
    }
    
    /// Set start and end time. 
    public DbMatch(DateTime start, DateTime end)
    {
        StartTime = start;
        EndTime = end;
    }

    /// <summary>Player references, this field is set automatically server side when updating tables. </summary>
    public List<DbUser> PlayerRefs => GetArrayOrEmpty<string>().Select(DbUser.Reference).ToList();

    /// <summary>Table references</summary>
    public List<DbTable> TableRefs => GetArrayOrEmpty<string>().Select(Database.Reference<DbTable>).ToList();

    public IPromise<List<DbTable>> GetTablesAsync() => Database.GetObjectsAsync<DbTable>(TableRefs.Select(t => t.Uid)).Then(t => t.ToList());
    
    public IPromise<List<DbUser>> GetPlayersAsync() => Database.GetObjectsAsync<DbUser>(PlayerRefs.Select(t => t.Uid)).Then(t => t.ToList());

    //set => Set(value.Select(u => u.Uid).ToArray());
    /// <summary>
    /// Add an table to this match, save the match to save the change.
    /// </summary>
    /// <remarks>If the table is not saved, running this function will first save it, this is an requirement since we can't point to a non existing object. Calling this function should always be followed up by running SaveAsync on the object containing this relation so that the newly created objects aren't floating freely.</remarks>
    public IPromise AddTablesAsync(params DbTable[] tables)
    {
        List<DbTable> unsavedObjects = new();
        foreach (var table in tables)
        {
            if (table.IsNew || table.HasUnsavedChanges)
            {
                unsavedObjects.Add(table);
            }
            else this.ArrayAppendUnique("tables", table.Uid);
        }

        if (unsavedObjects.Count > 0)
        {
            return Database.SaveObjectsAsync(unsavedObjects.ToArray())
                .Then(_ => unsavedObjects.ForEach(x => this.ArrayAppendUnique("tables", x.Uid)));
        }
        return Promise.Resolved();
    }

    public IPromise<DbMatch> AddTablesAndSaveAsync(params DbTable[] tables) => AddTablesAsync(tables).Then(this.SaveAsync);


    // public List<DbTable> TablesList => GetArrayOrEmpty<string>().Select(Database.Reference<DbTable>).ToList();

    public DateTime? EndTime {
        get => GetOrElse<DateTime?>(null);
        set => Set(value);
    }
    
    public DateTime? StartTime {
        get => GetOrElse<DateTime?>(null);
        set => Set(value);
    }
    
    public string Name {
        get => GetOrElse("");
        set => Set(value);
    }
}

