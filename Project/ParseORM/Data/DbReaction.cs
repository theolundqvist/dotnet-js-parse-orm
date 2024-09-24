using Project.ParseORM;
using Project.Util;

namespace Project.ParseORM.Data;

/// <summary>
/// A reaction can be added or modified by saving a new reaction object.
/// Use <see cref="DbPost.AddReactionAsync"/> and <see cref="DbPost.RemoveReactionAsync"/> to add or remove votes.
/// </summary>
public class DbReaction : DbObject
{
  protected override Dictionary<string, string> KeyMaps => null;
  public override string ClassName => "Reaction";

  private DbReaction() { }
  public static DbReaction Create<T>(T target, ReactionType data) where T : DbObject
  {
    var r = new DbReaction();
    r.Target = target.Uid;
    r.Data = data;
    if (typeof(T) == typeof(DbMessage))
      r.Type = TargetType.Message;
    else if (typeof(T) == typeof(DbPost))
      r.Type = TargetType.Post;
    else
      throw new Exception("Invalid Target type");
    return r;
  }

  public static DbReaction Create(string targetUid, TargetType targetType, ReactionType data)
  {
    var r = new DbReaction();
    r.Target = targetUid;
    r.Data = data;
    r.Type = targetType;
    // Debugger.PrintJson(r.GetDirtyRemoteKeys());
    // Debugger.PrintJson(r.GetRemoteKeys().Where(k => r.Underlying.IsKeyDirty(k)));
    // Debugger.PrintJson(r.Underlying.CurrentOperations.Values.Select(v => v.Encode(Database.Client)));
    return r;
  }
  /// Automatically set on upload
  public DbUser User
  {
    get => DbUser.Reference(GetOrElse(""));
    set => Set(value.Uid);
  }

  public ReactionType Data
  {
    get => GetOrElse(ReactionType.Unknown);
    set => Set(value);
  }

  public TargetType Type
  {
    get => GetOrElse(TargetType.Unknown);
    private set => Set(value);
  }

  public string Target
  {
    get => GetOrElse("");
    set => Set(value);
  }

  public DbReaction SetReactionData(ReactionType data)
  {
    Data = data;
    return this;
  }

  public enum TargetType
  {
    Unknown = 0,
    Post = 1,
    Message = 2
  }
  public enum ReactionType
  {
    Unknown = 0,
    Like = 1,
    Dislike = 2,
    Love = 3,
    Haha = 4,
    Wow = 5,
    Sad = 6,
    Angry = 7
  }
}