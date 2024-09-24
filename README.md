## .NET js-parse (+firebase) ORM

This is my try at building my own object-relational mapper (ORM) in .NET.

I built it since I wanted to automatically detects changes to .NET class instances and run the proper atomic actions on the database.

Key features that I wanted to implement:
* Object mapping (mapping named str keys to class fields)
* Field serialization
* Atomic operation calculation from series of assignments
* Caching
* Intellisense + type checking
* Key spelling checking
* Typed queries

```c#
var chat = new DbChat()
chat.Name = "Best chat"
chat.AddUser(me)
chat.Save() // save and fetch

chat.IntField++ // atomic increment
chat.Increment("IntField", 2) // atomic increment
chat.SaveAsync() // sends atomic increment(+3)

chat.IntField++ // atomic increment
chat.Increment("IntField", 2) // atomic increment
chat.IntField = 5
chat.SaveAsync() // sends IntField=5
```

In the first iteration I tried to build it towards the Firebase API but had a hard time.

In the second iteration I used the Firebase .NET SDK, which has operations like .Increment("field", 2), so I added a bunch of reflection to solve object mapping and field assignment operations like =, ++, etc.

In the third and last iteration I rebuilt everything for Parse JS since I switched to that backend. I built the ORM on top of the Parse .NET SDK but also added caching and handling of new field types such as DbObjectPointers, Relations, file pointers and object inheritance. I also wrapped all possible queries with a bunch of generics magic to provide type intellisense.

### DbObject example
note, 
```c#
public class DbMessage : DbObject
{
    protected override Dictionary<string, string> KeyMaps => new() {{"State", "status"} }; // another name is used server side
    public override string ClassName => "Message";
    
    public DbMessage(DbUser sender, DbChat chat, string text) : base()
    {
        SetDefaultValues();
        Sender = sender;
        Chat = chat;
        Text = text;
    }

    /// <summary>Use instead of SaveAsync, will set MessageState to Sent locally, not critical, SaveAsync can still be used </summary>
    public IPromise<DbMessage> Send() => this.SaveAsync().ThenKeepVal(c => State = MessageState.Sent);

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
}
```

