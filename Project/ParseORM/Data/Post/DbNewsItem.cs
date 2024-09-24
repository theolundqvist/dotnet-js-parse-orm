namespace Project.ParseORM.Data;

public class DbNewsItem : DbPost
{
    protected override Dictionary<string, string> KeyMaps => new(){{"NewsCategory", "subtype"}, {"Description", "data2"}};
    
    protected override QueryLimiter? SubTypeLimiter => new() { Key="type", Value = (int)PostType.News };
    
    private DbNewsItem():base("",""){}
    
    public DbNewsItem(string title, string description, string data, Category newsCategory) : base(title, data)
    {
        Description = description;
        NewsCategory = newsCategory;
    }

    public enum Category
    {
        Unknown=0,
        Event = 1,
        News = 2,
        Announcement = 3,
    }
    public Category NewsCategory
    {
        get => GetOrElse(Category.Unknown);
        set => Set(value);  
    } 

    ///<summary> A short description that can be displayed in game. </summary>
    public string Description
    {
        get => GetOrElse("");
        set => Set(value);
    }
}





