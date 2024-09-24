using System.Diagnostics;
using Project.Util;
using Parse;
using RSG;
using Debugger = Project.Util.Debugger;

namespace Project.ParseORM.Data;

public class DbChat : DbObject
{
  protected override Dictionary<string, string> KeyMaps =>
      new()
      {

      };

  public override string ClassName => "Chat";

  // CHAT

  private DbChat() { }

  public DbChat(string uid1, string uid2, params string[] uids)
  {
    AddUsers(uid1);
    AddUsers(uid2);
    AddUsers(uids);
  }

  public List<DbUser> Users =>
      GetArrayOrEmpty<string>()
          .Select(Database.Reference<DbUser>).ToList();

  public string Name
  {
    get => GetOrElse("");
    set => Set(value);
  }


  /// <summary>You need to save the object after using this function.</summary>
  public DbChat AddUsers(params string[] users)
  {
    users.ToList().ForEach(u => this.ArrayAppendUnique("users", u));
    return this;
  }
  public DbChat AddUsers(params DbUser[] users) => AddUsers(users.Select(u => u.Uid).ToArray());



  //CHILDREN QUERY

  private DbPagedQuery<DbMessage> _messages;
  public DbPagedQuery<DbMessage> Messages => DbPagedQuery<DbMessage>.LazyBuild(ref _messages, (b) =>
  {
    Debugger.Print("Building messages query for chat " + this.Uid);
    return b
      .WhereEqualTo("chat", this.IsNew ? throw new Exception("Chat is not saved yet") : this.Uid)
      .DefineOrdering("createdAt", (c) => c.CreatedAt.Value, _ascending: false, _reverseInner: true)
      .OnItemsFetched((messages) =>
      {
        foreach (var msg in messages)
        {
          msg.NotifyReceived(); //for the sender to know that the message is received
        }
      });
});

  /// <inheritdoc cref="DbChildrenQueryable{T}.GetAllFetchedChildren"/> 
  public DbChat Clear()
  {
    // ClearChildrenQueryCache();
    Messages.ClearFetchedItemsCache();
    return this;
  }



  /// <inheritdoc cref="DbChildrenQueryable{T}.GetAllFetchedChildren"/> 
  public List<DbMessage> GetAllDownloadedMessages() => Messages.GetAllFetchedItems();//GetAllFetchedChildren();

  /// <inheritdoc cref="DbChildrenQueryable{T}.FetchGreaterChildrenAsync"/> 
  public IPromise<List<DbMessage>> FetchNewMessagesAsync(int limit = 10) => Messages.FetchGreaterItemsAsync(limit);
  // FetchGreaterChildrenAsync(limit);

  /// <inheritdoc cref="DbChildrenQueryable{T}.FetchLesserChildrenAsync"/> 
  public IPromise<List<DbMessage>> FetchOlderMessagesAsync(int limit = 10) => Messages.FetchLesserItemsAsync(limit);
  //  FetchLesserChildrenAsync(limit);

  /// <inheritdoc cref="DbChildrenQueryable{T}.AddFetchedChild"/> 
  public void AddDownloadedMessage(DbMessage dbMessage) => Messages.AddFetchedItem(dbMessage);
  //  AddFetchedChild(dbMessage);

  /// <inheritdoc cref="DbChildrenQueryable{T}.AddFetchedChildren"/> 
  public void AddDownloadedMessages(IEnumerable<DbMessage> msgs) => Messages.AddFetchedItems(msgs);//l AddFetchedChildren(msgs);


  /// Same as creating a new message, saving it and adding it to the local cache.
  public IPromise<DbMessage> AddMessageAsync(string text, DbUser sender) =>
      new DbMessage(sender, this, text).Send().ThenKeepVal(m => AddDownloadedMessage(m));

}