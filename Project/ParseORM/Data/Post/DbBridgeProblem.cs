namespace Project.ParseORM.Data;

public class DbBridgeProblem : DbPost
{
    protected override Dictionary<string, string> KeyMaps => new(){{"problemCategory", "subtype"}, {"Description", "data2"}};
    protected override QueryLimiter? SubTypeLimiter => new() { Key="type", Value = (int)PostType.Problem };

    private DbBridgeProblem():base("", ""){}

    public DbBridgeProblem(Category cat, string title, string data) : base(title, data)
    {
        ProblemCategory = cat;
    }

    public enum Category  
    {
        Unknown = 0,
        Bidding = 1,
        Play = 2,
        Suit = 3,
    }
    public Category ProblemCategory
    {
        get => GetOrElse(Category.Unknown);
        set => Set(value);  
    } 

    public string Description
    {
        get => GetOrElse("");
        set => Set(value);
    }
}





