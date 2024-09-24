

namespace Project.ParseORM.Data.Room;

public class DbPracticeSet : DbPost
{
  protected override Dictionary<string, string> KeyMaps =>
     new() { { "PracticeType", "subtype" }, {"Description", "data2"}, {"Chapter","parent"} };

  protected override QueryLimiter? SubTypeLimiter => new() { Key = "type", Value = (int)PostType.PracticeSet };

  private DbPracticeSet() : base("", "") { }

  public DbPracticeSet(DbRoom room, DbChapter chapter, string title, string description, string data) : base(title, data)
  {
    Room = room;
    Chapter = chapter;
    Description = description;
    PracticeType = _PracticeType.Unknown;
  }

  public enum _PracticeType
  {
    Unknown,
    Bidding,
    Lead,
    Play
  }

  public _PracticeType PracticeType
  {
    get => GetOrElse(_PracticeType.Unknown);
    set => Set(value);
  }

    public string Description
    {
        get => GetOrElse("");
        set => Set(value);
    }

    public DbChapter Chapter {
        get => Database.Reference<DbChapter>(GetOrElse(""));
        set => Set(value.Uid);
    }
    
    public DbRoom Room {
        get => Database.Reference<DbRoom>(GetOrElse(""));
        set => Set(value.Uid);
    }
}





