using System.Data;
using Bridgestars.Networking;
using Project.Util;
using Parse;
using RSG;

using Project.ParseORM.Data;

namespace Project.ParseORM.Data.Room
{

  public class DbRoom : DbObject
  {
    protected override Dictionary<string, string> KeyMaps =>
        new() { { "description", "desc" } };
    public override string ClassName => "Room";

    // needed for reflection
    private DbRoom() { }

    public DbRoom(string name, string desc)
    {
      Name = name;
      Description = desc;
    }

    /// <summary>
    /// Course description
    /// </summary>
    public string Description
    {
      get => GetOrElse("");
      set => Set(value);
    }

    /// The room title
    public string Name
    {
      get => GetOrElse("");
      set => Set(value);
    }


    private DbPagedQuery<DbUser> _users;
    public DbPagedQuery<DbUser> Users => DbPagedQuery<DbUser>.LazyBuild(ref _users,
      (b) => b
        .WhereEqualTo("rooms", this.Uid) // rooms is array but its ok
        .DefineOrdering("dispName", (r) => r.DisplayUsername, _ascending: true)
        .OnItemsFetched((items) =>
        {
          // Debugger.Print($"Items fetched {items.Select(x => x.DisplayUsername).ToJson()}");
        })
      );

    private DbPagedQuery<DbCourse> _courses;
    public DbPagedQuery<DbCourse> Courses => DbPagedQuery<DbCourse>.LazyBuild(ref _courses,
      (b) => b
        .WhereEqualTo("room", this.Uid)
        .DefineOrdering("title", (r) => r.Title, _ascending: true)
        .OnItemsFetched((items) =>
        {
          // Debugger.Print($"Items fetched {items.Select(x => x.Title).ToJson()}");
        })
      );

    private DbPagedQuery<DbQuestion> _questions;
    public DbPagedQuery<DbQuestion> Questions => DbPagedQuery<DbQuestion>.LazyBuild(ref _questions,
      (b) => b
        .WhereEqualTo("room", this.Uid)
        .DefineOrdering("createdAt", (r) => r.CreatedAt.Value, _ascending: false)
        .OnItemsFetched((items) => { 
          // Debugger.Print($"Items fetched {items.Select(x => x.Name).ToJson()}");
        })
      );
  }

}
