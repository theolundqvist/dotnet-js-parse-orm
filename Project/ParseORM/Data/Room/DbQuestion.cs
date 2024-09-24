
namespace Project.ParseORM.Data.Room;

public class DbQuestion : DbPost
{
  protected override Dictionary<string, string> KeyMaps => 
    new() { { "questionType", "subtype" }, { "Description", "data2" }};

  protected override QueryLimiter? SubTypeLimiter => new() { Key = "type", Value = (int) PostType.Question };

  private DbQuestion() : base("", "") { }

  public DbQuestion(DbRoom room, string title, string description, _QuestionType type = _QuestionType.Unknown, string data = null) : base(title, data)
  {
    this.Room = room;
    Description = description;
    QuestionType = type;
  }

  public enum _QuestionType
  {
    Unknown = 0,
    Bidding = 1,
    Lead = 2,
    Play = 3,
    Other = 4
  } 

  public _QuestionType QuestionType
  {
    get => GetOrElse(_QuestionType.Unknown);
    set => Set(value);
  }

  public string Description
  {
    get => GetOrElse("");
    set => Set(value);
  }

  public DbRoom Room
  {
    get => Database.Reference<DbRoom>(GetOrElse(""));
    set => Set(value.Uid);
  }


  /// <summary> If this question is to be displayed in or with a reference to ex a chapter and not in a room, <see cref="Parent"/> could be set to something other than null or <see cref="Room"/>. </summary>
  public DbObject Parent {
    get => Database.Reference<DbObject>(GetOrElse(""));
    set => Set(value.Uid);
  }
}





