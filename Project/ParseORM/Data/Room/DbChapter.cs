
namespace Project.ParseORM.Data.Room;

public class DbChapter : DbPost
{
  protected override Dictionary<string, string> KeyMaps =>
       new(){
            {"ChapterNumber", "subtype"},
            { "Description", "data2" },
            {"Course", "parent"}
        };

  protected override QueryLimiter? SubTypeLimiter => new() { Key = "type", Value = (int)PostType.Chapter };

  private DbChapter() : base("", "") { }

  public DbChapter(DbRoom room, DbCourse course, int chapterNumber, string title, string description, string data = null) : base(title, data)
  {
    Room = room;
    Course = course;
    Description = description;
    ChapterNumber = chapterNumber;
  }

  public int ChapterNumber
  {
    get => GetOrElse(-1);
    set => Set(value);
  }

  public DbCourse Course
  {
    get => Database.Reference<DbCourse>(GetOrElse(""));
    set => Set(value.Uid);
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

  private DbPagedQuery<DbPracticeSet> _PracticeSets;
  public DbPagedQuery<DbPracticeSet> PracticeSets => DbPagedQuery<DbPracticeSet>.LazyBuild(ref _PracticeSets,
      (b) => b
          .WhereEqualTo("chapter", this.Uid)
          .DefineOrdering("title", (ps) => ps.Title, _ascending: false) // godtycklig ordning
          .OnItemsFetched((ps) => { })
  );

}





