using Project.ParseORM.Data;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM;
sealed public class FriendRequest
{
    public string Sender;
    public string Receiver;
    private DbUser _owner;

    public FriendRequest(string sender, string receiver, DbUser owner) {
        Sender = sender;
        Receiver = receiver;
        _owner = owner;
    }

    /// <summary>
    /// Accept an incoming friend request. Throws when trying to accept an outgoing friend request. 
    /// </summary>
    public IPromise<DbUser> AcceptAsync() =>
        _owner.Uid.Equals(Sender) ? Promise<DbUser>.Rejected(new ("Can't accept outgoing friend request")) :
            Database.Client.CallCloudCodeFunctionAsync<string>("acceptFriendRequest",
                new Dictionary<string, object> { { "uid", Sender.ToString() } }).ToPromise().Then(s =>
                _owner.FetchFieldsAsync("friends", "ifr"));

    /// <summary>
    /// Deny an incoming friend request or revoke an outgoing friend request. 
    /// </summary>
    public IPromise DenyAsync() =>
        Database.Client.CallCloudCodeFunctionAsync<string>("removeFriend",
                new Dictionary<string, object> { { "uid", Sender } })
            .ToPromise().Then(s => _owner.FetchFieldsAsync("ofr", "ifr")).Empty();
}