using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM.Data;
public class DbMessage : DbReactionable
{
    protected override DbReaction.TargetType ReactionTargetType => DbReaction.TargetType.Message;
    protected override Dictionary<string, string> KeyMaps => new() {{"State", "status"} };
    public override string ClassName => "Message";

    private DbMessage(){}
    
    public DbMessage(DbUser sender, DbChat chat, string text) : base()
    {
        SetDefaultValues();
        Sender = sender;
        Chat = chat;
        Text = text;
    }
    
    public DbMessage(string sender, string chat, string text) : base()
    {
        SetDefaultValues();
        Sender = DbUser.Reference(sender);
        Chat = Database.Reference<DbChat>(chat);
        Text = text;
    }


    /// <summary>Use instead of SaveAsync, will set MessageState to Sent locally, not critical, SaveAsync can still be used </summary>
    public IPromise<DbMessage> Send() => this.SaveAsync().ThenKeepVal(c => State = MessageState.Sent);

    // public Relation<DbUser> Reactions {
    //     get => GetRelation<DbUser>();
    //     // private set => Set(value);
    // }

    public DbChat Chat {
        get => Database.Reference<DbChat>(GetOrElse(""));
        internal set => Set(value.Uid);
    }
    
    public DbUser Sender {
        get => DbUser.Reference(GetOrElse("")); 
        private set => Set(value.Uid);
    }
    
    public string Text {
        get => GetOrElse("");
        set => Set(value);
    }

    public bool IsEdited => CreatedAt != UpdatedAt;
    
    public MessageState State {
        get => GetOrElse(MessageState.None);
        set => Set(value);
    }


    public enum MessageState
    {
        None = 0,
        Sent = 1,
        Received = 2,
        Read = 3,
        // Edited,
        // Deleted
    }

    public IPromise<DbMessage> NotifyRead()
    {
        if (State != MessageState.Received && State != MessageState.Sent) return Promise<DbMessage>.Rejected(new Exception("Message is not sent or received"));
        State = MessageState.Read;
        return this.SaveAsync();
    }
    
    public IPromise<DbMessage> NotifyReceived()
    {
        if (this.IsNew) return Promise<DbMessage>.Rejected(new Exception("Message is not sent"));
        State = MessageState.Received;
        return this.SaveAsync();
    }
}