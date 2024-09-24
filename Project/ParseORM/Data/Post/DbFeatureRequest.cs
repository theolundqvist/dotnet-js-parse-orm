namespace Project.ParseORM.Data;

public class DbFeatureRequest : DbPost
{
    protected override Dictionary<string, string> KeyMaps => new(){{"RequestStatus", "subtype"}, { "Description", "data" }, {"Data", "data2"} };
    
    protected override QueryLimiter? SubTypeLimiter => new() { Key="type", Value = (int)PostType.Request };

    private DbFeatureRequest():base("",""){}

    public DbFeatureRequest(string title, string description) : base(title, null)
    {
        Description = description;
        RequestStatus = Status.New;
    }

    public enum Status
    {
        Unknown = 0,
        New = 1,
        Reviewed = 2,
        Planned = 3,
        InProgress = 4,
        InBeta = 5,
        Done = 6,
        AlreadyExists = 7,
        Live = 8 
    }
    public Status RequestStatus
    {
        get => GetOrElse(Status.Unknown);
        set => Set(value);  
    } 
    
    public string Description
    {
        get => GetOrElse("");
        set => Set(value);
    }
}





