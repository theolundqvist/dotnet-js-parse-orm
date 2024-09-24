using Bridgestars.Networking;
using Project.FirebaseORM.Authentication;
using Project.FirebaseORM.Cloud;
using Project.FirebaseORM.Database.Data;
using Project.FirebaseORM.Database.Transform;
using Project.Util;
using Project.Util.WebUtil;
using RSG;

namespace Project.FirebaseORM.Database
{[Obsolete("Use new Project.Backend")]
    public class SimpleUserDatabase : UserDatabaseWrapper<FirestoreUser>
    {
        protected override string API_KEY => FirebaseCredentials.API_KEY;
        protected override string API_URL => FirebaseCredentials.API_URL;
        protected override string dbPath => FirebaseCredentials.dbPath;

        //public AddFriendRequest
    }

    /// <summary>
    /// Use this to inherit, only created to avoid having to pass type arguments to every method
    /// </summary>
    /// <typeparam name="U"> FirestoreUser </typeparam>
    [Obsolete("Use new Project.Backend")]
    public abstract class UserDatabaseWrapper<U> : UserDatabase where U : FirestoreUser
    {
        /// <summary>
        /// Will query the alphabetically sorted list of usernames, starting on and returning the document that matches 'username'
        /// followed by the next n-1 documents. Amount of documents and good matches is therefore not guaranteed.
        /// </summary>
        /// <param name="username"> username to search for </param>
        /// <param name="n"> Nbr of users to return, default = 5 </param>
        /// <returns>Array of U:FirestoreUser</returns>
        public IPromise<U[]> FindUsersByUsername(string username, int n = 5)
        {
            return FindUsersByUsername<U>(username, n);
        }

        /// <summary>
        /// Retrieves an user from the Firebase Database, given their id.
        /// </summary>
        /// <param name="uid"> Id of the user that we are looking for </param>
        /// <typeparam name="U">FirestoreUser</typeparam>
        /// <returns>U:FirestoreUser</returns>
        public IPromise<U> GetUserData(string uid)
        {
            return GetUserData<U>(uid);
        }

        /// <summary>
        /// Retrieves multiple users from the Firebase Database, given their id. The requests will be run in parallel but will result in multiple reads. 
        /// </summary>
        /// <param name="uids"> List of ids of the users that we are looking for </param>
        /// <typeparam name="U">FirestoreUser</typeparam>
        /// <returns>List of U</returns>
        public IPromise<U[]> GetUserData(IEnumerable<string> uids)
        {
            return GetUserData<U>(uids);
        }

        /// <summary>
        /// Add chatId to uid.chats if not already added
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <returns></returns>
        [Obsolete("Replaced with `JoinChat`")]
        public IPromise<U> AddChat(string uid, string chatId, U userdata = null)
        {
            return base.AddChat(uid, chatId, userdata);
        }

        /// <summary>
        /// Add chatId to uid.chats if not already added
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <returns></returns>
        public IPromise<U> JoinChat(string uid, string chatId, U userdata = null) =>
            base.JoinChat(uid, chatId, userdata);

        /// <summary>
        /// Remove ChatId from uid.chats
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <returns></returns>
        [Obsolete("Replaced with `LeaveChat`")]
        public IPromise<U> RemoveChat(string uid, string chatId, U userdata = null)
        {
            return base.RemoveChat(uid, chatId, userdata);
        }

        /// <summary>
        /// Remove ChatId from uid.chats
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <returns></returns>
        public IPromise<U> LeaveChat(string uid, string chatId, U userdata = null) =>
            base.LeaveChat(uid, chatId, userdata);

        /// <summary>
        /// Add sender to receiver.friendRequests, will not add if already existing.
        /// </summary>
        /// <param name="receiver">user to update</param>
        /// <param name="sender">friend to add</param>
        /// <param name="userdata">receiver´s userdata if available</param>
        /// <returns></returns>
        [Obsolete]
        public IPromise<U> AddFriendRequest(string receiver, string sender, U userdata = null)
        {
            return base.AddFriendRequest(receiver, sender, userdata);
        }

        /// <summary>
        /// Add friend and remove friendRequest if pending, will not add friend if already befriended
        /// </summary>
        /// <param name="userId">user to update</param>
        /// <param name="friendId">friend to add</param>
        /// <param name="userdata">user´s userdata if available</param>
        /// <returns></returns>
        [Obsolete]
        public IPromise<U> AddFriend(string userId, string friendId, U userdata = null)
        {
            return base.AddFriend(userId, friendId, userdata);
        }
    }

    /// <summary>
    /// A database that supports users with username etc.
    /// An implementation of this class should wrap all protected methods specifying the generic type as (or as subtype) of Userdata.
    /// </summary>
    [Obsolete("Use new Project.Backend")]
    public abstract class UserDatabase : Database
    {
        /// <summary>
        /// Resolves if there is no user on the firestore database with username: <b><paramref name="username"/></b>.<br/>
        /// Not case sensitive. 
        /// </summary>
        /// <param name="username"></param>
        public IPromise IsUsernameAvailable(string username)
        {
            Debugger.Print("CHECKING IF USERNAME IS FREE: " + username.ToLower());
            return Functions.AuthorizedRequest(Functions.AuthIsUsernameAvailable, CREDENTIAL)
                .AddParam("username", username)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();
        }

        // /// <summary>
        // /// If available, resolves with email corresponding to this username
        // /// </summary>
        // /// <param name="username"></param>
        // /// <returns> email </returns>
        // public IPromise<string> GetEmailFromUsername(string username)
        // {
        //     Debugger.Print("TRYING TO GET EMAIL FROM USERNAME: \n" + username.ToLower());
        //     return GetDocument<UsernameDoc>("usernames/" + username.ToLower())
        //         .Then(user =>
        //             user.email
        //         );
        // }

        /// <summary>
        /// Update data on existing user document.
        /// Will not resolve if user object contains updated username.
        /// </summary>
        /// <param name="user"> User object that will be uploaded </param>
        /// <param name="uid"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        internal IPromise UpdateUserData<U>(U user, string uid) where U : FirestoreUser
        {
            return user.IsFieldUpdated("username")
                ? Rejected("Can't update username without credential")
                : UpdateUserData(user,
                    new Credential
                    {
                        uid = uid,
                    }
                );
        }

        /// <summary>
        /// Update data on existing user document.
        /// </summary>
        /// <param name="user"> User object that will be uploaded </param>
        /// <param name="cred"> This credential must belong to the user which is being updated </param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        public IPromise UpdateUserData<U>(U user, Credential cred) where U : FirestoreUser
        {
            if (!UID.Validate(cred.uid)) return Rejected("'" + cred.uid + "' is not a valid UID");
            if (!HasValidAccessToken) return Rejected("DATABASE ACCESS_TOKEN NOT DEFINED OR EXPIRED");

            user.usernameLower = user.username.ToLower();
            return (user.IsFieldUpdated("username") ? TryUpdateUsername(user, cred) : Promise.Resolved())
                .Then(() =>
                {
                    user.DocPath = FirebaseCredentials.dbPath + "users/" + cred.uid;
                    var payload = user.GetUpdateJson();
                    if (payload == null)
                    {
                        Debugger.Print("PAYLOAD == NULL\n" + JsonUtil.ToJson(user));
                        throw new Exception("NO DATA TO POST! DATA IS IDENTICAL TO SERVER VERSION.");
                    }

                    Debugger.Print("POSTING USER DATA: \n" + payload);
                    return PostData(payload);
                });
        }

        /// <summary>
        /// PostUserData helper
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cred"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        private IPromise TryUpdateUsername<U>(U user, Credential cred) where U : FirestoreUser
        {
            Debugger.Print("UPDATING USERNAME");

            return user.username.Length switch
            {
                0 => Rejected("Username is not defined"),
                < Auth.SHORTEST_USERNAME_LENGTH => Rejected("Username must be at least 4 characters long."),
                > Auth.LONGEST_USERNAME_LENGTH => Rejected("Username must be at most 16 characters long."),
                _ => Functions.AuthorizedRequest(Functions.AuthUpdateUsername, cred)
                    .AddParam("new_username", user.username)
                    .GET_STRING()
                    .Catch(e => throw FirebaseException.Parse(e))
                    .Empty()
                // _ => GetEmailFromUsername(user.username)
                //     .Then(email =>
                //     {
                //         if(email != cred.email)
                //             throw new Exception("NOT OK");
                //     })
                //     .Catch(e =>
                //     {
                //         Debugger.Print(e.Message);
                //         if(!string.IsNullOrEmpty(e.Message) && e.Message.Equals("NOT OK"))
                //             throw new Exception("Username is unavailable: " + user.username, e);
                //     })
                //     .Then(() =>
                //     {
                //         var usernameOld = "";
                //         user.TryGetOldValue("username", ref usernameOld);
                //         return usernameOld;
                //     })
                //     .ThenIf(s => !string.IsNullOrEmpty(s), 
                //         s => DeleteDocument("usernames/" + s.ToLower()).Catch(e => { })
                //         )
                //     .Then(() => PostDocument(UsernameDoc.WithEmail(cred.email), "usernames/" + user.username.ToLower())
                //         .CatchWrap("Failed to upload username document"))
            };
        }

        #region methods that should be wrapped by subtype

        /// <summary>
        /// Add chatId to uid.chats if not already added
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        [Obsolete("Replaced with `JoinChat`")]
        public IPromise<U> AddChat<U>(string uid, string chatId, U userdata = null) where U : FirestoreUser
        {
            return MapServerSideUserdata(userdata, uid, u =>
            {
                Debugger.Print(u);
                Debugger.Print(u.ToFirestoreJson());
                if (!u.chats.Contains(chatId)) u.chats = u.chats.Prepend(chatId);
                return u;
            });
        }

        /// <summary>
        /// Add chatId to uid.chats if not already added
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        public IPromise<U> JoinChat<U>(string uid, string chatId, U userdata = null) where U : FirestoreUser
        {
            return MapServerSideUserdata(userdata, uid, u =>
            {
                if (!u.chats.Contains(chatId)) u.chats = u.chats.Prepend(chatId);
                return u;
            });
        }

        /// <summary>
        /// Remove ChatId from uid.chats
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        [Obsolete("replaced with `LeaveChat`")]
        public IPromise<U> RemoveChat<U>(string uid, string chatId, U userdata = null) where U : FirestoreUser
        {
            return MapServerSideUserdata(userdata, uid, u =>
            {
                var tempList = u.chats.ToList();
                tempList.Remove(chatId);
                u.chats = tempList.ToArray();
                return u;
            });
        }

        /// <summary>
        /// Remove ChatId from uid.chats
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="chatId"></param>
        /// <param name="userdata"></param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        public IPromise<U> LeaveChat<U>(string uid, string chatId, U userdata = null) where U : FirestoreUser
        {
            return MapServerSideUserdata(userdata, uid, u =>
            {
                var tempList = u.chats.ToList();
                tempList.Remove(chatId);
                u.chats = tempList.ToArray();
                return u;
            });
        }

        /// <summary>
        /// Add sender to receiver.friendRequests, will not add if already existing.
        ///<br/>
        /// <b>USE SendFriendRequest</b>
        /// </summary>
        /// <param name="receiver">user to update</param>
        /// <param name="sender">friend to add</param>
        /// <param name="userdata">receiver´s userdata if available</param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        [Obsolete]
        public IPromise<U> AddFriendRequest<U>(string receiver, string sender, U userdata = null)
            where U : FirestoreUser
            => SendFriendRequest(sender, receiver).Then(() => userdata);

        // return MapServerSideUserdata(userdata, receiver, u =>
        // {
        //     if(!u.friendRequests.Contains(sender)) u.friendRequests = u.friendRequests.Prepend(sender);
        //     return u;
        // });

        /// <summary>
        /// Add sender to receiver.incomingFriendRequests, will not add if already existing.
        /// Add receiver to sender.outgoingFriendRequests
        /// </summary>
        /// <param name="receiver"> This user will receive a friend request </param>
        /// <param name="sender"> This user will send a friend request </param>
        /// <returns></returns>
        public IPromise SendFriendRequest(string sender, string receiver)
            => Functions.AuthorizedRequest(Functions.SendFriendRequest, CREDENTIAL) //using admin credential is okay
                .AddParam("receiver", receiver)
                .AddParam("sender", sender)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();


        /// <summary>
        /// Add friend and remove friendRequest if pending, will not add friend if already befriended
        /// </summary>
        /// <param name="userId">user to update</param>
        /// <param name="friendId">friend to add</param>
        /// <param name="userdata">user´s userdata if available</param>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <returns></returns>
        [Obsolete]
        public IPromise<U> AddFriend<U>(string userId, string friendId, U userdata = null) where U : FirestoreUser
            => AcceptFriendRequest(userId, friendId).Then(() => userdata);

        /// <summary>
        /// Remove incoming and outgoing requests and add friends uid to `friends`
        /// </summary>
        /// <param name="receiver"> Receiver of friend request, this is the person accepting the request </param>
        /// <param name="sender"> The sender of the friend request </param>
        /// <returns></returns>
        public IPromise AcceptFriendRequest(string sender, string receiver)
            => Functions.AuthorizedRequest(Functions.AcceptFriendRequest, CREDENTIAL)
                .AddParam("receiver", receiver)
                .AddParam("sender", sender)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();


        /// <summary>
        /// Remove friendId from userId.friends and from userId.friendRequests
        /// </summary>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <param name="userId">user to update</param>
        /// <param name="friendId">friend to remove</param>
        /// <param name="userdata">user´s data if available</param>
        /// <returns></returns>
        [Obsolete]
        public IPromise<U> RemoveFriend<U>(string userId, string friendId, U userdata = null) where U : FirestoreUser
            => RemoveFriend(userId, friendId).Then(() => userdata);
        // {
        //     return MapServerSideUserdata(userdata, userId, u =>
        //     {
        //         //REMOVE FRIEND
        //         var tempList = u.friends.ToList();
        //         tempList.Remove(friendId);
        //         u.friends = tempList.ToArray();
        //
        //         //REMOVE FRIENDREQUEST
        //         tempList = u.friendRequests.ToList();
        //         tempList.Remove(friendId);
        //         u.friendRequests = tempList.ToArray();
        //         return u;
        //     });
        // }


        /// <summary>
        /// Updates `friends`, `incomingFriendRequests` and `outgoingFriendRequests` on userId and friendId.<br/>
        /// Both users are updated.
        /// </summary>
        /// <param name="uid1"> user to update </param>
        /// <param name="uid2"> user to update </param>
        /// <returns></returns>
        /// 
        public IPromise RemoveFriend(string uid1, string uid2)
            => Functions.AuthorizedRequest(Functions.RemoveFriend, CREDENTIAL)
                .AddParam("receiver", uid1)
                .AddParam("sender", uid2)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();

        /// <summary>
        /// Will query the alphabetically sorted list of usernames, starting on and returning the document that matches 'username'
        /// followed by the next n-1 documents. Amount of documents and good matches is therefore not guaranteed.
        /// </summary>
        /// <param name="username"> username to search for </param>
        /// <param name="n"> Number of users to return, default = 5, max = 15 </param>
        /// <returns>Array of U:FirestoreUser</returns>
        /// <returns></returns>
        public IPromise<U[]> FindUsersByUsername<U>(string username, int n = 5) where U : FirestoreUser
        {
            if (n > 15) return Rejected<U[]>("Value of n may not be larger than 15");
            return RunQuery<U>(new Query()
                .SelectCollection("users")
                .OrderBy("usernameLower")
                .StartAt(new OldData.String(username.ToLower()))
                .EndAt(new OldData.String(username.ToLower() + '~'))
                .SetReadLimit(n)
            );
        }

        /// <summary>
        /// Retrieves an user from the Firebase Database, given their id.
        /// </summary>
        /// <param name="uid"> Id of the user that we are looking for </param>
        /// <typeparam name="U">FirestoreUser</typeparam>
        /// <returns>U:FirestoreUser</returns>
        public IPromise<U> GetUserData<U>(string uid) where U : FirestoreUser
        {
            if (!UID.Validate(uid)) return Rejected<U>("'" + uid + "' is not a valid UID");
            Debugger.Print("GETTING USER DATA....");
            //Debugger.Print(this.GetType().Name);
            //Debugger.Print("VALID ACCESS TOKEN? " + HasValidAccessToken);
            //Debugger.Print("ACCESS_TOKEN: " + ACCESS_TOKEN );
            //Debugger.Print("CRED: " + JsonUtil.ToJson(CREDENTIAL));
            if (!HasValidAccessToken) return Rejected<U>("ACCESS_TOKEN NOT DEFINED");

            return new WebRequest()
                .AddHeader("Accept", "application/json")
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .SetContentType(WebRequest.ContentType.json)
                .SetEndpoint(API_URL + dbPath + "users/" + uid)
                .GET_STRING()
                .ThenKeepVal(s => Debugger.Print(s))
                .Catch(e => throw FirebaseException.Parse(e))
                .Then(FirestoreObject.BuildFromJson<U>)
                .CatchWrap("COULD NOT DOWNLOAD USERDATA: ")
                .ThenKeepVal(data => { Debugger.Print("RECEIVED USER DATA:\n" + JsonUtil.ToJson(data)); });
            //.PrintSerialized();
        }

        /// <summary>
        /// Retrieves multiple users from the Firebase Database, given their id. The requests will be run in parallel but will result in multiple reads. 
        /// </summary>
        /// <param name="uids"> List of ids of the users that we are looking for </param>
        /// <typeparam name="U">FirestoreUser</typeparam>
        /// <returns>List of U</returns>
        [Obsolete("Very rarely does not resolve nor reject")]
        public IPromise<U[]> GetUserData<U>(IEnumerable<string> uids) where U : FirestoreUser
        {
            
            var enumerable = uids.ToList();
            Debugger.Print(uids.ToString());
            Debugger.Print(JsonUtil.ToJson(enumerable)); 
            return Promise<U>.All(enumerable.Select(GetUserData<U>).ToArray()).ThenKeepVal(us => Debugger.Print("DONE")).Then(us => us.ToArray()).ThenKeepVal(us => Debugger.Print("DONE"));
        }

        #endregion


        #region private methods

        /// <summary>
        /// Updates <b><paramref name="user"/></b> and post to server, if <b><paramref name="user"/></b> is null then first download userdata
        /// Will not resolve when trying to change username field
        /// </summary>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <param name="user">UserData object</param>
        /// <param name="uid">string, user id</param>
        /// <param name="f">function from UserData->UserData</param>
        /// <returns></returns>
        internal IPromise<U> MapServerSideUserdata<U>(U user, string uid, Func<U, U> f) where U : FirestoreUser
        {
            if (user != null)
            {
                var u = f(user);
                return UpdateUserData(u, uid)
                    .Then(() => u);
            }

            return GetUserData<U>(uid)
                .Then(f)
                .ThenKeepVal(u => UpdateUserData(u, uid));
        }
        
        /// <summary>
        /// Updates <b><paramref name="user"/></b> and post to server, if <b><paramref name="user"/></b> is null then first download userdata
        /// Will not resolve when trying to change username field
        /// </summary>
        /// <typeparam name="U"> FirestoreUser </typeparam>
        /// <param name="user">UserData object</param>
        /// <param name="uid">string, user id</param>
        /// <param name="f">function from UserData->UserData</param>
        /// <returns></returns>
        internal IPromise<U> MapServerSideUserdata<U>(string uid, Func<U, U> f, U user = null) where U : FirestoreUser
        {
            if (user != null)
            {
                var u = f(user);
                return UpdateUserData(u, uid)
                    .Then(() => u);
            }

            return GetUserData<U>(uid)
                .Then(f)
                .ThenKeepVal(u => UpdateUserData(u, uid));
        }

        #endregion
    }

    [Obsolete("Use new Project.Backend")]
    public abstract class Database
    {
        // ReSharper disable once InconsistentNaming
        protected abstract string API_KEY { get; }

        // ReSharper disable once InconsistentNaming
        protected abstract string API_URL { get; }

        // ReSharper disable once InconsistentNaming
        protected abstract string dbPath { get; }

        // ReSharper disable once InconsistentNaming
        protected string ACCESS_TOKEN => CREDENTIAL?.accessToken;

        // ReSharper disable once InconsistentNaming
        protected Credential CREDENTIAL;

        /// <summary>
        /// Sets the credential that is used for database calls.
        /// </summary>
        public void SetCredential(Credential cred) => CREDENTIAL = cred;

        internal Credential GetCredential() => CREDENTIAL;

        /// <summary>
        /// Try to use credentials from <b>FirebaseCredentials</b> to be used for database calls.
        /// </summary>
        public IPromise TryUseAdminCredential() =>
            Auth.SignInWithEmail(FirebaseCredentials.ADMIN_EMAIL, FirebaseCredentials.ADMIN_PASSWORD)
                .Then(c => CREDENTIAL = c).Empty();

        /// <summary>
        /// <b>OBSOLETE</b> use <b>SetCredential</b> instead. <br/>
        ///
        /// Sets an user's credential to be used for database calls.
        /// </summary>
        [Obsolete]
        public void UseUserCredential(Credential cred) => CREDENTIAL = cred;

        /// <summary>
        /// Returns true if database credential is defined and hasn't expired
        /// </summary>
        public bool HasValidAccessToken => CREDENTIAL != null && !CREDENTIAL.HasExpired();

        /// <summary>
        /// Credential is only valid for 60 minutes, this method will update the validity of the credential for an additional 60 minutes 
        /// </summary>
        public IPromise RefreshCredential() => Auth.RefreshUserCredential(CREDENTIAL).Then(c => CREDENTIAL = c).Empty();

        protected Credential GetActiveCredential()
        {
            return CREDENTIAL;
        }

        #region generic methods

        // [Obsolete] USE NEW PARSER
        /// <summary>
        /// Runs a Firebase Query defined by <b><paramref name="query"/></b> and returns an array of FirestoreDocuments of type <b><typeparamref name="D"/></b>
        /// </summary>
        /// <param name="query"> Query object </param>
        /// <typeparam name="D"> FirestoreDocument </typeparam>
        public IPromise<D[]> RunQuery<D>(Query query) where D : FirestoreDocument
        {
            string payload = query.GetPayload();
            if (!HasValidAccessToken) return Rejected<D[]>("ACCESS_TOKEN NOT DEFINED OR EXPIRED");

            Debugger.Print(payload);

            return new WebRequest()
                .AddHeader("Accept", "application/json")
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .SetContentType(WebRequest.ContentType.json)
                .AddParam("key", API_KEY)
                .SetEndpoint(API_URL + dbPath + ":runQuery")
                .AddJsonBody(payload)
                .POST_STRING()
                .ThenKeepVal(s => Debugger.Print(s))
                .Catch(e => throw FirebaseException.Parse(e))
                //.MapSuccessContent(json => (G) FirestoreDocument.FromJson(json))
                .Then(FirestoreDocument.BuildCollectionFromJson<D>)
                .ThenKeepVal(d => Debugger.Print("Query success, received " + d.Length + " documents"));
        }

        /// <summary>
        /// Retrieves a document from the Firebase Database
        /// </summary>
        /// <param name="path"> The path to the document. ex "users/uid" </param>
        /// <typeparam name="D"> FirestoreDocument </typeparam>
        public IPromise<D> GetDocument<D>(string path) where D : FirestoreDocument
        {
            if (string.IsNullOrWhiteSpace(path)) return Rejected<D>("'" + path + "' is not a valid path");

            return GetRaw(path).Then(FirestoreObject.BuildFromJson<D>);
        }

        /// <summary>
        /// Retrieves a document from the Firebase Database, will not try to parse the document. Instead returns "as is".
        /// </summary>
        /// <param name="path"> The path to the document. ex "users/uid" </param>
        public IPromise<string> GetRaw(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Rejected<string>("'" + path + "' is not a valid path");

            Debugger.Print("READING DOCUMENT...");

            var wr = new WebRequest()
                .AddHeader("Accept", "application/json")
                .SetContentType(WebRequest.ContentType.json)
                .SetEndpoint(API_URL + dbPath + path);

            if (HasValidAccessToken) wr.AddHeader("Authorization", "Bearer " + ACCESS_TOKEN);

            return wr
                .GET_STRING()
                .ThenKeepVal(s => Debugger.Print("RECEIVED DATA:\n" + s))
                .Catch(e => throw FirebaseException.Parse(e));
        }

        /// <summary>
        /// Updates a document on the Firebase Database
        /// </summary>
        /// <param name="doc">Obj inheriting from FirestoreDocument</param>
        /// <param name="path"> The path to the document. ex "users/uid" </param>
        public IPromise PostDocument(FirestoreDocument doc, string path = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return Rejected("'" + path + "' is not a valid path");

            if (!HasValidAccessToken) return Rejected("ACCESS_TOKEN NOT DEFINED");

            if (path != null) doc.DocPath = FirebaseCredentials.dbPath + path;
            var payload = doc.GetUpdateJson();
            if (payload == null)
                return Rejected("NO DATA TO POST! DATA IS IDENTICAL TO SERVER VERSION.");

            return PostData(payload);
        }

        /// <summary>
        /// Creates a new document on the firestore database with an unique id
        /// </summary>
        /// <param name="doc">Firestore document</param>
        /// <param name="collection">ex 'users/'</param>
        /// <typeparam name="D"> FirestoreDocument </typeparam>
        /// <returns></returns>
        public IPromise<D> CreateDocumentWithAutomaticId<D>(D doc, string collection) where D : FirestoreDocument
        {
            if (string.IsNullOrWhiteSpace(collection))
                return Rejected<D>("'" + collection + "' is not a valid collection");

            if (!HasValidAccessToken) return Rejected<D>("ACCESS_TOKEN NOT DEFINED");
            //string payload = data.BuildUpdateRequestJson(path);
            //if (payload == null)
            //{
            //    var e = new System.Exception("NO DATA TO POST! DATA IS IDENTICAL TO SERVER VERSION.");
            //    Print(e.Message);
            //    var p = new Promise<string>(); p.Reject(e);
            //    return p;
            //}

            //TRIES TO CREATE DOCUMENT WITH NEW ID
            string payload = "";
            try
            {
                payload = doc.GetCreateJson();
            }
            catch (Exception e)
            {
                return Rejected<D>("COULD NOT CREATE REQUEST JSON:\n" + e.Message);
            }

            Debugger.Print("payload: \n" + payload);

            if (payload == "") return Rejected<D>("COULD NOT CREATE REQUEST JSON");

            return CreateData(payload, collection)
                //.Deserialize<G>()
                .Then(FirestoreDocument.BuildFromJson<D>)
                .ThenKeepVal(t => Debugger.Print("CREATED DOCUMENT WITH ID: \n" + t.DocId));
        }

        /// <summary>
        /// Delete document on path, must be document owner or admin
        /// </summary>
        /// <param name="path">ex "users/uid"</param>
        public IPromise DeleteDocument(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Rejected("'" + path + "' is not a valid path");
            Debugger.Print("DELETING DOCUMENT: " + path);
            if (!HasValidAccessToken) return Rejected("ACCESS_TOKEN NOT DEFINED");

            return new WebRequest()
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .AddHeader("Accept", "application/json")
                .SetContentType(WebRequest.ContentType.json) //.AddHeader("Content-Type", "application/json")
                .SetEndpoint(API_URL + dbPath + path + "?key=" + API_KEY)
                .DELETE_STRING()
                .ThenKeepVal(s => Debugger.Print("SUCCESS DELETING DOCUMENT\n" + s))
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();
        }

        public IPromise PostTransform(DocumentTransform t)
        {
            var payload = t.GetTransformPayload();
            if (string.IsNullOrWhiteSpace(payload)) return Rejected("'" + payload + "' is not a valid payload");
            Debugger.Print("POSTING DOCUMENT: \n" + payload);
            if (!HasValidAccessToken) return Rejected("ACCESS_TOKEN NOT DEFINED");

            return new WebRequest()
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .AddHeader("Accept", "application/json")
                .SetContentType(WebRequest.ContentType.json) //.AddHeader("Content-Type", "application/json")
                .AddJsonBody(payload)
                .SetEndpoint(API_URL + dbPath + ":commit?key=" + API_KEY)
                .POST_STRING()
                .ThenKeepVal(s => Debugger.Print("SUCCESS POSTING DOCUMENT\n" + s))
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();
        }

        protected IPromise PostData(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return Rejected("'" + payload + "' is not a valid payload");
            Debugger.Print("POSTING DOCUMENT: \n" + payload);
            if (!HasValidAccessToken) return Rejected("ACCESS_TOKEN NOT DEFINED");

            return new WebRequest()
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .AddHeader("Accept", "application/json")
                .SetContentType(WebRequest.ContentType.json) //.AddHeader("Content-Type", "application/json")
                .AddJsonBody(payload)
                .SetEndpoint(API_URL + dbPath + ":commit?key=" + API_KEY)
                .POST_STRING()
                .ThenKeepVal(s => Debugger.Print("SUCCESS POSTING DOCUMENT\n" + s))
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();
        }

        /// <param name="payload"></param>
        /// <param name="path"> path to document ex "users/uid" </param>
        /// <returns>promise with the created document as json string</returns>
        private IPromise<string> CreateData(string payload, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Rejected<string>("'" + path + "' is not a valid path");
            Debugger.Print("CREATING DOCUMENT: \n" + payload);
            if (!HasValidAccessToken) return Rejected<string>("ACCESS_TOKEN NOT DEFINED");

            return new WebRequest()
                .AddHeader("Authorization", "Bearer " + ACCESS_TOKEN)
                .AddHeader("Accept", "application/json")
                .SetContentType(WebRequest.ContentType.json)
                .AddJsonBody(payload)
                .SetEndpoint(API_URL + dbPath + path + "?key=" + API_KEY)
                .POST_STRING()
                .ThenKeepVal(s => Debugger.Print("SUCCESS CREATING DOCUMENT\n" + s))
                .Catch(e => throw FirebaseException.Parse(e));
        }


        protected static IPromise<D> Rejected<D>(string s)
        {
            Debugger.Print(s);
            return new Promise<D>().Reject(s);
        }

        protected static IPromise Rejected(string s)
        {
            Debugger.Print(s);
            return new Promise().Reject(s);
        }

        #endregion generic methods
    }
}