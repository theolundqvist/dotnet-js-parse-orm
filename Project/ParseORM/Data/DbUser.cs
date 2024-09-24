using Project.ParseORM.Data.Room;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM.Data;

public class DbUser : DbObject
{
    protected override Dictionary<string, string> KeyMaps =>
        new()
        {   //left hand side is case insensitive
            { "displayUsername", "dispName" },
            { "balance", "bal" },
            { "incomingFriendRequests", "ifr" },
            { "outgoingFriendRequests", "ofr" },
            { "profileAccess", "profileAccess" },
            { "firstName", "first" },
            { "lastName", "last" },
            { "dateOfBirth", "birth" },
            { "LastGameSignIn", "gameSignIn" },
        };
    public override string ClassName => "_User";

    private DbUser() { }

    public IPromise<DbUser> GetUserInfoAsync() =>
        Database.Client.CallCloudCodeFunctionAsync<ParseUser>(
                "getUserInfo",
                new Dictionary<string, object>
                {
                    { "uid", Uid }
                }).ToPromise()
            // .Then(uo => uo.FetchAsync().ToPromise())
            // .ThenKeepVal(o => Debugger.PrintJson(o))
            // .ThenKeepVal(o => Debugger.PrintJson(o.ServerDataToJSONObjectForSerialization()))

            .Then(Instantiate<DbUser>)
            .Then(u =>
            {
#pragma warning disable CS0618
                u.TransferPropValue("profileAccess", this, false);
                u.TransferPropValue("firstName", this, false);
                u.TransferPropValue("lastName", this, false);
                u.TransferPropValue("nationality", this, false);
                u.TransferPropValue("dateOfBirth", this, false);
#pragma warning restore CS0618
                return this;
            })
            .Catch(e => this); //User does not have access to this profile // not my problem


    [Obsolete]
    private DbMatchHistory _matchHistory;
    [Obsolete("Use user.Matches instead, this allows for reordering and filtering.")]
    public DbMatchHistory MatchHistory => _matchHistory ??= new DbMatchHistory(this);

    private DbPagedQuery<DbMatch> _matches;
    ///<summary>A paginated list of matches that is sortable and filterable.</summary>
    public DbPagedQuery<DbMatch> Matches => DbPagedQuery<DbMatch>.LazyBuild(ref _matches,
        (b) =>
            b.WhereEqualTo("players", Uid)
            .DefineOrdering("createdAt", (m) => m.CreatedAt, false)
            .OnItemsFetched((matches) => { })
        );


    #region Protected Fields
    public string Email
    {
        get => GetOrElse("");
        // set => Set(value);
    }
    //do the same for the field first and the field last
    public string FirstName
    {
        get => GetOrElse("");
        set => Set(value);
    }
    public string LastName
    {
        get => GetOrElse("");
        set => Set(value);
    }

    public DateTime? DateOfBirth
    {
        get => GetOrElse<DateTime?>(null);
        set => Set(value);
    }

    public string Nationality
    {
        get => GetOrElse("");
        set => Set(value);
    }

    public ProfileAccessType ProfileAccess
    {
        get => (ProfileAccessType)GetOrElse(0);
        set => Set((int)value);
    }

    public enum ProfileAccessType
    {
        NoOne,
        Friends,
        Public
    }

    public DateTime? LastGameSignIn
    {
        get => GetOrElse<DateTime?>(null);
        set => Set(value);
    }
    #endregion

    #region Friends

    public List<DbUser> Friends => GetArrayOrEmpty<string>().Select(Reference).ToList();


    [Obsolete("Not recommended, SendFriendRequest should be used instead, this is just for migration")]
    public void SetFriends(IEnumerable<string> uids) => Set(uids.ToArray(), "friends");

    // set => Set(value);
    public List<FriendRequest> IncomingFriendRequests
    {
        get => GetArrayOrEmpty<string>().Select(s => new FriendRequest(s, Uid, this)).ToList();
        // set => Set(value);
    }

    public List<FriendRequest> OutgoingFriendRequests
    {
        get => GetArrayOrEmpty<string>().Select(s => new FriendRequest(Uid, s, this)).ToList();
        // set => Set(value);
    }



    public void SignOutAsync()
    {
        if (Database.IsSignedIn(this))
            Database.SignOutAsync();
        else throw new Exception("This user is not signed in.");
    }


    /// <summary>
    /// Send a friendRequest to <see cref="receiver"/> 
    /// </summary>
    public IPromise<DbUser> SendFriendRequestAsync(DbUser receiver) => SendFriendRequestAsync(receiver.Uid);

    /// <summary>
    /// Send a friendRequest to <see cref="receiverUid"/> 
    /// </summary>
    public IPromise<DbUser> SendFriendRequestAsync(string receiverUid) =>
        Uid.Equals(receiverUid)
            ? Promise<DbUser>.Rejected(new Exception("Can't add yourself as a friend"))
            : Database.Client.CallCloudCodeFunctionAsync<string>("sendFriendRequest",
                    new Dictionary<string, object> { { "receiver", receiverUid.ToString() } }).ToPromise()
                .Then(s => this.FetchFieldsAsync("ofr"));

    /// <inheritdoc cref="RemoveFriendAsync(string)"/> 
    public IPromise<DbUser> RemoveFriendAsync(DbUser receiver) => RemoveFriendAsync(receiver.Uid);

    /// <summary>
    /// Remove friend from your friends list, if there is a pending friend request in either direction, remove that too.
    /// </summary>
    public IPromise<DbUser> RemoveFriendAsync(string friendUid) =>
        Database.Client
            .CallCloudCodeFunctionAsync<string>("removeFriend",
                new Dictionary<string, object> { { "uid", friendUid.ToString() } })
            .ToPromise()
            // .Then(s => this.FetchAsync());
            .Then(s => this.FetchFieldsAsync("friends", "ifr", "ofr"));


    #endregion Friends

    #region Fields

    public string Username
    {
        get => GetOrElse("");
        set => Set(value);
    }

    public string DisplayUsername => GetOrElse("");

    // public string Password {
    //     set => Set(value);
    // }

    public string SessionToken => GetOrElse("");

    public double Balance
    {
        get => GetOrElse(0.0);
        set => Set(value);
    }

    public double Elo
    {
        get => GetOrElse(0.0);
        set => Set(value);
    }


    public double Xp
    {
        get => GetOrElse(0.0);
        set => Set(value);
    }

    public string Img {
        get => GetOrElse("");
        set => Set(value);
    }


    public enum Subscription
    {
       Education,
       Unknown 
    }

    public bool HasSubscription(Subscription s) => Subscriptions.Contains(s);

    public List<Subscription> Subscriptions => 
        GetArrayOrElse("subscriptions", new string[0])
        .Select(x =>
        {
            return x switch
            {
                "education" => Subscription.Education,
                _ => Subscription.Unknown
            };
        })
        .ToList();


    public List<DbRoom> Rooms =>
        GetArrayOrEmpty<string>().Select(Database.Reference<DbRoom>).ToList();
    public List<DbCourse> Courses =>
        GetArrayOrEmpty<string>().Select(Database.Reference<DbCourse>).ToList();


    #endregion

    #region avatar

    public DbFile Avatar
    {
        get => GetFile();
        set => SetFile(value);
    } 
    #endregion avatar
    public IPromise<DbChat[]> GetAllChatsAsync() => new DbQuery<DbChat>()
        .WhereEqualTo<bool>("public", false).WhereEqualTo("users", this.Uid).FindAsync();

    public static DbUser Reference(string uid) => Database.Reference<DbUser>(uid);
}
