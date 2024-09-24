using Parse;

namespace Project.ParseORM.Data;

public class DbTable : DbObject
{
    protected override Dictionary<string, string> KeyMaps =>
        new()
        {   //left hand side is case insensitive
            
        };
    public override string ClassName => "Table";

    private DbTable(){}
    
    /// <param name="deal"> deal-data </param>
    /// <param name="deals"> more deals </param>
    public DbTable(string deal, params string[] deals)
    {
        Data = deals.ToList().Prepend(deal).ToArray();
    }

    /// <param name="data"> encoded deals </param>
    public DbTable(string[] data)
    {
        Data = data;
    }

    /// <summary> Contains encoded Deal data. </summary>
    public string[] Data
    {
      get => GetArrayOrEmpty<string>();
      private set => Set(value);
    } 
    
    
    /// <summary>
    /// Overwrites deals saved on the server in the <see cref="Data"/> array. Don't forget to call <see cref="DbObjectExtensions.SaveAsync{DbTable}"/> to save the changes.
    /// </summary>
    /// <param name="data"> string encoded deal data </param>
    /// <returns> Self reference </returns>
    public DbTable SetAllDeals(string[] deals)
    {
        Data = deals;
        return this;
    }

    public DbTable SetAllDeals(string deal, params string[] deals) => SetAllDeals(deals.ToList().Prepend(deal).ToArray());

    /// <summary>
    /// Adds a deal to the end of the <see cref="Data"/> array. Don't forget to call <see cref="DbObjectExtensions.SaveAsync{DbTable}"/> to save the changes.
    /// </summary>
    /// <param name="data"> string encoded deal data </param>
    /// <returns> Self reference </returns>
    public DbTable AddDeal(params string[] data)
    {
        data.ToList().ForEach(d => this.ArrayAppend("data", d));
        return this;
    }

    /// <summary> A DbUser reference to the user that played as South on this table. The user will only contain uid until fetched, see <see cref="DbObjectExtensions.FetchAsync{T}"/>. </summary>
    public DbUser South {
        get => DbUser.Reference(GetOrElse<string>(null));
        set => Set(value.Uid);
    }
    /// <summary> A DbUser reference to the user that played as North on this table. The user will only contain uid until fetched, see <see cref="DbObjectExtensions.FetchAsync{T}"/>. </summary>
    public DbUser North {
        get => DbUser.Reference(GetOrElse<string>(null));
        set => Set(value.Uid);
    }
    /// <summary> A DbUser reference to the user that played as East on this table. The user will only contain uid until fetched, see <see cref="DbObjectExtensions.FetchAsync{T}"/>. </summary>
    public DbUser East {
        get => DbUser.Reference(GetOrElse<string>(null));
        set => Set(value.Uid);
    }
    /// <summary> A DbUser reference to the user that played as West on this table. The user will only contain uid until fetched, see <see cref="DbObjectExtensions.FetchAsync{T}"/>. </summary>
    public DbUser West {
        get => DbUser.Reference(GetOrElse<string>(null));
        set => Set(value.Uid);
    }
}
