using System.Data;
using Bridgestars.Networking;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM.Data;



/// <summary>
/// This class is used to create Post subclasses, it is not meant to be used directly.
/// </summary>
public class DbPost : DbReactionable
{
  protected override DbReaction.TargetType ReactionTargetType => DbReaction.TargetType.Post;

  protected override Dictionary<string, string> KeyMaps { get; }
  public override string ClassName => "Post";

  // needed for reflection
  internal DbPost() { }

  /// <summary> WARNING: This constructor is only used when all post types are wanted, this is most often not the case. </summary>
  public DbPost(string title, string data)
  {
    Title = title;
    if(data != null) Data = data;
  }


  /// <summary> The user that created the post. <see cref="Database.Reference{T}"/> </summary>
  public DbUser Author
  {
    get
    {
      //TODO TEST AND EXTRACT TO GETPOINTER
      var user = GetOrElse<ParseUser>(null);
      if (user == null) return null;
      var dbuser = DbObject.Instantiate<DbUser>(user);

      if (dbuser.HasData || !Database.IsCached<DbUser>(dbuser.Uid)) Database.AddToCache(ref dbuser); //this pointer has been downloaded with data, update cache to reflect this.
      else //this pointer has not been downloaded with data, use the cached object instead.
      {
        var tx = Database.Reference<DbUser>(dbuser.Uid);
        return tx;
      }

      return dbuser;
    }
    set
    {
      if (value == null) return;
      Set(value.Underlying);
    }
  }


  /// <summary> The post data, can be any string encoded data. "data2" is available as well, this is commonly used for description data. </summary>
  public string Data
  {
    get => GetOrElse("");
    set => Set(value);
  }

  /// <summary> The post title/decription </summary>
  public string Title
  {
    get => GetOrElse("");
    set => Set(value);
  }


  public enum PostType
  {
    Unknown = 0,
    Request = 1,
    Problem = 2,
    News = 3,
    Question = 4,
    PracticeSet = 5,
    Chapter = 6
  }

  /// <summary> The type of the post. We only ever want one type at a time when querying. </summary>
  public PostType Type
  {
    get => GetOrElse(PostType.Unknown);
    set => Set(value);
  }

  /// <summary> The number of comments in the chat. </summary>
  public int NumComments => GetOrElse(0);

  /// <summary> Public Chat is automatically created on SaveAsync(). Chat will need fetching first time if any members are needed since this is only a reference. <see cref="DbChat.Messages"/> are still ok to use since those are queried for based on the uid of the chat.
  /// <br></br>
  /// <see cref="Database.Reference{T}"/> below:
  /// </summary>
  /// <inheritdoc cref="Database.Reference{T}"/>
  public DbChat Chat => Database.Reference<DbChat>(GetOrElse(""));
}





