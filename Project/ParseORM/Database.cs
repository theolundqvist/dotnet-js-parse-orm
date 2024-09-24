using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using Project.ParseORM.Data;
using Project.ParseORM.Data.Commerce;
using Project.ParseORM.Data.Room;
using Bridgestars.BridgeUtilities;
using Project.Util;
using Newtonsoft.Json;
using Parse;
using Parse.Infrastructure;
using RSG;
using Debugger = Project.Util.Debugger;

// ReSharper disable InvalidXmlDocComment

namespace Project.ParseORM;


/// <summary>
/// 1. Database.Connect()
/// </summary>
public static class Database
{
    // PATH MUST END WITH "/"
    // private const string ServerUrl = "http://localhost:1337/rest/";
    private static bool Dev { get; set; }
    private static string ServerUrl => $"https://aws.lb{(Dev ? ".dev" : "")}.bridgestars.net/rest/";
    private const string AppId = "app-id";
    private static void UseDevEndpoint(bool dev) => Dev = dev;
    internal enum Endpoint
    {
        [EnumMember(Value = "login")] Login,
        [EnumMember(Value = "users")] Users,
    }

    private static ParseClient client;

    public static ParseClient Client
    {
        get
        {
            ThrowIfNotConnected();
            return client;
        }
        set => client = value;
    }

    /// The local cache, contains some convenience methods. 
    public static readonly DbObjectCache Cache = new(true);

    /// Enable or disable the local cache.
    public static void EnableLocalCache(bool enabled) => Cache.Enabled = enabled;

    /// <inheritdoc cref="DbObjectCache.DisposeAll"/> 
    public static void ClearLocalCache() => Cache.DisposeAll();

    /// Whether the specified user is currently logged in.
    public static bool IsSignedIn(DbUser user) => Client != null && Client.GetCurrentUser().ObjectId == user.Uid;

    /// Get the currently signed in user.  
    public static DbUser GetCurrentUser() => ThrowIfNotSignedIn(() => DbUser.Reference(Client.GetCurrentUser().ObjectId));

    #region Account


    /// <summary>
    /// Will first call a cloud function to migrate the user from firebase if needed, then login
    /// Will also set <see cref="DbUser.LastGameSignIn"/> to DateTime.Now
    /// </summary>
    /// <param name="usernameOrEmail"> user's username or email</param>
    /// <param name="password">user's password</param>
    /// <returns>User's data as <see cref="DbUser"/></returns>
    /// <remarks>Side-effects: Clears cache</remarks>
    public static IPromise<DbUser> SignInAsync(string usernameOrEmail, string password)
    {
        ThrowIfNotConnected();
        Cache.DisposeAll();

        return SignOutAsync().Then(() => Client.CallCloudCodeFunctionAsync<string>("signIn", new Dictionary<string, object>
            {
                { "username", usernameOrEmail.ToLower() },
                { "password", password }
            }).ToPromise())
            .Then(s => Client.LogInAsync(usernameOrEmail.ToLower(), password).ToPromise())
            .Then(user =>
            {
                var u = DbObject.Instantiate<DbUser>(user);
                Cache.AddToCache(ref u);
                if (u.Username != "admin")
                {
                    u.LastGameSignIn = DateTime.Now;
                    u.SaveAsync(); //should run async, since we use thenkeepval
                }
                return u;
            });
    }
    // Client.LogInAsync(usernameOrEmail.ToLower(), password).ToPromise<DbUser>();

    /// <summary>
    /// Creates an account with the specified username, email and password.
    /// Promise will fail if the username or email is already taken, or if any of the fields does not match the requirements.
    /// </summary>
    /// <remarks>Side-effects: Clears cache</remarks>
    public static IPromise<DbUser> SignUpAsync(string username, string email, string password)
    {
        ThrowIfNotConnected();
        Cache.DisposeAll();
        var user = new ParseUser
        {
            Username = username,
            Password = password,
            Email = email
        };
        return user.SignUpAsync().ToPromise()
            .Then(() =>
            {
                var u = DbObject.Instantiate<DbUser>(user);
                Cache.AddToCache(ref u);
                u.LastGameSignIn = DateTime.Now;
                u.SaveAsync(); //should run async, since we use thenkeepval
                return u;
            });
    }

    /// Sign out the currently signed in user.
    /// <remarks>Side-effects: Clears cache</remarks>
    public static IPromise SignOutAsync() => Client.LogOutAsync().ToPromise().Catch(e =>
    {
        if (e.Message == "Invalid session token")
        {
            //session is already disabled, so we can just clear the cache
        }
        else throw e;
    }).Then(() => Cache.DisposeAll());

    /// Sign out the currently signed in user from all devices including this one.
    /// <remarks>Side-effects: Clears cache</remarks>
    public static IPromise SignOutFromAllDevicesAsync() =>
        Client.CallCloudCodeFunctionAsync<string>("signOutFromAllDevices", null)
            .ToPromise().Empty().Then(() => Cache.DisposeAll())
            .Then(SignOutAsync);

    /// Send a password reset email to the specified email address. 
    public static IPromise RequestPasswordReset(string email) => Client.RequestPasswordResetAsync(email).ToPromise();


    #endregion Account

    #region SearchUsers
    public class UserSearchResult
    {
        [JsonProperty("id")]
        public string Uid;
        [JsonProperty("disp")]
        public string DisplayUsername;
        [JsonProperty("img")]
        public string Img;
    }

    // [Obsolete("Use Database.SearchUsersAsync(username, previous_username, per_page) instead")]
    // public static IPromise<UserSearchResult[]> SearchUsersAsync(string username, int page, int per_page = 10) =>
    //     ThrowIfNotSignedIn(() => Client.CallCloudCodeFunctionAsync<string>("searchUsers",
    //             new Dictionary<string, object> { { "username", username }, { "page", page }, { "per_page", per_page } })
    //         .ToPromise()
    //         .Then(JsonUtil.FromJson<UserSearchResult[]>));

    /// <summary>
    /// Returns users (<see cref="UserSearchResult"/>) that match the specified username. 
    /// </summary>
    /// <param name="username"> The username to search for. </param>
    /// <param name="previous_username"> The last username in the previous search to fetch to next page. </param>
    /// <param name="per_page"> How many result to yield. </param>
    public static IPromise<UserSearchResult[]> SearchUsersAsync(string username, string previous_username, int per_page = 10, string room = null) =>
        ThrowIfNotSignedIn(() => Client.CallCloudCodeFunctionAsync<string>("searchUsers",
                new Dictionary<string, object> { { "username", username }, { "prev_username", previous_username }, { "per_page", per_page }, { "room", room } })
            .ToPromise()
            .Then(JsonUtil.FromJson<UserSearchResult[]>));

    /// <summary>
    /// Returns users (<see cref="UserSearchResult"/>) that match the specified username. 
    /// </summary>
    /// <param name="username"> The username to search for. </param>
    /// <param name="per_page"> How many result to yield. </param>
    public static IPromise<UserSearchResult[]> SearchUsersAsync(string username, int per_page = 10, string room = null) =>
        ThrowIfNotSignedIn(() => Client.CallCloudCodeFunctionAsync<string>("searchUsers",
                new Dictionary<string, object> { { "username", username }, { "per_page", per_page }, { "room", room } }).ToPromise()
                .Then(JsonUtil.FromJson<UserSearchResult[]>));



    /// <summary>
    ///  Check if the email is already in use.
    /// </summary>
    public static IPromise EmailExistsAsync(string email) =>
        GetUserQuery().WhereEqualTo("email", email.ToLower().Trim()).FirstAsync().Empty();

    /// <summary>
    ///  Check if the username is already in use.
    /// </summary>
    public static IPromise UsernameExistsAsync(string username) =>
        GetUserQuery().WhereEqualTo("username", username.ToLower().Trim()).FirstAsync().Empty();



    #endregion SearchUsers

    #region Objects



    /// <inheritdoc cref="Database.GetObjectAsync{T}"/>
    public static IPromise<DbUser> GetUserAsync(string uid, params string[] fieldsToInclude) =>
        GetUserQuery().SelectMultiple(fieldsToInclude).GetObjectByIdAsync(uid);

    /// <inheritdoc cref="Database.GetObjectsAsync{T}"/>
    public static IPromise<DbUser[]> GetUsersAsync(IEnumerable<string> uids, params string[] fieldsToInclude) =>
        GetUserQuery().SelectMultiple(fieldsToInclude).WhereContainedIn("objectId", uids).FindAsync();

    /// <inheritdoc cref="GetQuery{T}"/> 
    public static DbQuery<DbUser> GetUserQuery() => ThrowIfNotSignedIn(() => new DbQuery<DbUser>());


    /// <summary>
    /// Creates a reference to a specified Database object.
    /// </summary>
    /// <returns> Cached object with data that may be outdated, empty reference if object is not cached, null if uid has an invalid format. </returns>
    public static T Reference<T>(string uid) where T : DbObject
    {
        if (string.IsNullOrEmpty(uid))
            return null;
        if (Cache.Contains<T>(uid))
            return Cache.Get<T>(uid);
        T obj = DbObject.Instantiate<T>(
            typeof(T) == typeof(DbUser)
            ? Client.CreateObjectWithoutData<ParseUser>(uid)
            : Client.CreateObjectWithoutData(DbObject.GetClassname<T>(), uid));
        AddToCache(ref obj);
        return obj;
    }

    /// <summary>
    /// Creates an empty reference to a specified Database object. Ignores cached objects.
    /// </summary>
    /// <remarks>This method will create an additional copy of the Database object and does therefore not comply with the "one object one reference" guideline. </remarks>
    internal static T EmptyReference<T>(string uid) where T : DbObject
    {
        if (string.IsNullOrEmpty(uid))
            return null;
        return DbObject.Instantiate<T>(
            typeof(T) == typeof(DbUser)
                ? Client.CreateObjectWithoutData<ParseUser>(uid)
                : Client.CreateObjectWithoutData(DbObject.GetClassname<T>(), uid));
    }

    /// Adds the object to the cache. Obj may be mutated. 
    public static void AddToCache<T>(ref T obj) where T : DbObject => Cache.AddToCache(ref obj);

    /// Add an entire list of objects to the cache. Only the returned objects should be used afterwards.
    public static List<T> AddRangeToCache<T>(IEnumerable<T> objs) where T : DbObject => Cache.AddRangeToCache(objs);

    /// <returns>true if the object is cached. TimeToLive may be expired. </returns>
    public static bool IsCached<T>(string uid) where T : DbObject => Cache.Contains<T>(uid);

    /// <summary>
    /// Creates a reference to a specified Database object.
    /// <br/><br/>
    /// The cache policy is defined as below:
    /// <br/><br/>
    /// i. Each object has a TimeToLive (TTL) value. The TTL is the time in seconds that the object is allowed to be cached.
    /// <br/><br/>
    /// 1. If the object is not cached, it is fetched from the server, cached and returned.
    /// <br/><br/>
    /// 2. If the object is cached, and the TTL has not expired, the cached object is returned.
    /// <br/><br/>
    /// 3. If the object is cached, and the TTL has expired, a request is sent to the server containing the object's last update time.
    /// <br/>
    /// 3.1 If the remote object's last update time is the same as the cached object last update time, the cached object is returned.
    /// <br/>
    /// 3.2 Otherwise the remote object is returned from the server, cached and returned.
    /// </summary>
    /// <param name="print"> Whether debug information should be printed (cache miss/hits etc) </param>
    public static IPromise<T> ReferenceAsync<T>(string uid, bool print = false) where T : DbObject
    {
        if (Cache.TryGet<T>(uid, out var obj))
        {
            if (obj.IsStale)
            {
                if (print) Debugger.Print($"[Db] {typeof(T).Name} {uid} is stale, fetching data...");
                return CheckAndUpdate(obj, print);
            }
            else
            {
                if (print) Debugger.Print($"[Db] {typeof(T).Name} {uid} is fresh, returning.");
                return Promise<T>.Resolved(obj);
            }
        }
        if (print) Debugger.Print($"[Db] {typeof(T).Name} {uid} is not cached, fetching...");
        return GetObjectAsyncNoCache<T>(uid).Then(o =>
        { //if not in cache, get from server
            Cache.AddToCache(ref o);
            return o;
        });
    }

    private static IPromise<T> CheckAndUpdate<T>(T obj, bool print = false) where T : DbObject
    {
        var className = DbObject.GetClassname<T>();
        // if (obj.UpdatedAt == null) throw new Exception("Object has no updatedAt");
        return CallCloudFunctionAsync<ParseObject>("fetchIfUpdated", new Dictionary<string, object>
        {
            { "className", className },
            { "uid", obj.Uid },
            { "updatedAt", obj.UpdatedAt }
        }).Then(updatedObj =>
        {
            if (updatedObj == null) //object has same updatedAt serverside
            {
                if (print) Debugger.Print($"[Db] {typeof(T).Name} {obj.Uid} is still fresh, returning.");
                obj.NoteFetched();
                return obj;
            }

            if (print) Debugger.Print($"[Db] {typeof(T).Name} {obj.Uid} was outdated, new data has been fetched.");
            var db = UnderlyingTo<T>(updatedObj);

            obj.NoteFetched();
            Cache.AddToCache(ref db); //updates obj
            return obj;
        });
    }

    // #endregion


    /// <summary>
    /// Will fetch the specified object from local cache or from the server according to the rules defined by <see cref="ReferenceAsync{T}"/> 
    /// </summary>
    /// <param name="fieldsToInclude"> Currently fetches entire object either way. </param>
    public static IPromise<T> GetObjectAsync<T>(string uid, params string[] fieldsToInclude) where T : DbObject =>
        GetQuery<T>().SelectMultiple(fieldsToInclude).GetObjectByIdAsync(uid);

    /// <summary>
    /// Will fetch the specified object from server even if it is cached.
    /// </summary>
    /// <param name="fieldsToInclude"> Currently fetches entire object either way. </param>
    private static IPromise<T> GetObjectAsyncNoCache<T>(string uid, params string[] fieldsToInclude) where T : DbObject =>
        GetQuery<T>().SelectMultiple(fieldsToInclude).BypassCache().GetObjectByIdAsync(uid);

    /// <summary>
    /// Will fetch the specified object from the server.
    /// </summary>
    /// <TODO> Add possibility to exclude cached objects from search. </TODO>
    /// <param name="fieldsToInclude"> Currently fetches entire object either way. </param>
    public static IPromise<T[]> GetObjectsAsync<T>(IEnumerable<string> uids, params string[] fieldsToInclude) where T : DbObject =>
        GetQuery<T>().SelectMultiple(fieldsToInclude).WhereContainedIn("uid", uids).FindAsync();


    /// <inheritdoc cref="Parse.ParseExtensions.FetchAsync{T}(T)"/>
    /// <remarks> Returns self reference for convenience, mutates original.
    /// Not specifying keys will return all keys as default.</remarks>
    /// <TODO> Currently objects are not fetched from cache, all objects are fetched from server. </TODO>
    /// <TODO> All keys are fetched, not just the ones specified. </TODO>
    public static IPromise<List<T>> FetchObjectsAsync<T>(IEnumerable<T> objects, params string[] keys) where T : DbObject
    {
        var obj = objects.ToList();
        if (typeof(T) == typeof(DbUser))
        {
            //select uid of user which is authenticated
            var authedUser = obj.Where(o => ((ParseUser)o.Underlying).IsAuthenticated).ToList();
            // Debugger.Print($"fetching {obj.Except(authedUser).Count()} unauthed objects, {authedUser.Count} authed objects");
            //"FetchObjectsAsync wont keep auth status."
            return Client.FetchObjectsAsync(obj.Except(authedUser).Select(x => x.Underlying)).ToPromise()
                .Then(_ => authedUser.Count > 0 ?
                    authedUser.First().Underlying.FetchAsync().ToPromise().Empty() :
                    Promise.Resolved())
                .Then(() =>
                {
                    // really wierd, compiler does not let me use this with an lambda
                     return Cache.AddRangeToCache(obj);
                });
        }
        // Debugger.Print($"fetching {obj.Count} objects");
        return Client.FetchObjectsAsync(obj.Select(x => x.Underlying)).ToPromise()
            .Then(_ => Cache.AddRangeToCache(obj));

        // var objs = objects.ToArray();
        // if (objs.Length == 0) throw new Exception("List of objects to fetch is empty.");
        // if (keys.Length == 0) keys = objs[0].GetRemoteKeys().ToArray();
        // if (typeof(T) == typeof(DbUser))
        // {
        //     return GetUsersAsync(objs.Select(o => o.Uid), keys).Then(users =>
        //     {
        //         foreach (var t in users)
        //         {
        //             foreach (var key in keys)
        //             {
        //                 var rx = objs.FirstOrDefault(o => o.Uid == t.Uid);
        //                 if (rx != null && t.TransferPropValue(key, rx as DbUser))
        //                 {
        //                     rx.SetKeyNotDirty(key);
        //                 }
        //             }
        //         }
        //
        //         var res = objs.ToList();
        //         Cache.AddToCache(res);
        //         return res;
        //     });
        // }
        //
        // return GetObjectsAsync<T>(objs.Select(o => o.Uid), keys).Then(txs =>
        // {
        //     foreach (var t in txs)
        //     {
        //         foreach (var key in keys)
        //         {
        //             var rx = objs.FirstOrDefault(o => o.Uid == t.Uid);
        //             if (t.TransferPropValue(key, rx))
        //             {
        //                 rx.SetKeyNotDirty(key);
        //             }
        //         }
        //     }
        //     var res = objs.ToList();
        //     Cache.AddToCache(res);
        //     return res;
        // });
        // o.Underlying.FetchAsync().ToPromise().Then(a => o);   
    }


    /// <returns> An editable query directed on the specified DbClass. </returns>
    public static DbQuery<T> GetQuery<T>() where T : DbObject => ThrowIfNotSignedIn(() => new DbQuery<T>());


    /// <summary>
    /// Creates or updates the specified objects on the server.
    /// </summary>
    /// <param name="objects"> List of objects to save. Objects will be modified to include CreatedAt, Uid, UpdatedAt </param>
    /// <returns> Modified objects for convenience. </returns>
    public static IPromise<List<T>> SaveObjectsAsync<T>(IEnumerable<T> objects) where T : DbObject =>
        ThrowIfNotSignedIn(() =>
        {
            var obj = objects.ToList();
            if (Dev)
            {
                foreach (var o in obj)
                {
                    if (o.IsNew && !o.IsKeyDirty("test"))
                    {
                        if (o.GetValue<bool?>("test", null) != null)
                        {
                            Debugger.PrintJson(o.Underlying.Keys);
                            Debugger.Print("Adding [test]");
                            throw new Exception("Test should always be undefined here!!");
                        }
                        o.SetValue("test", true);
                    }
                }
            }
            return Client.SaveObjectsAsync(obj.Select(x => x.Underlying)).ToPromise().Then(() => obj)
                .Then(objs => Cache.AddRangeToCache(objs));
            // //SAVE ALL DIRTY KEYS
            // var objs = objects.Distinct().ToList();
            // if(objs.Count == 0) return Promise<List<T>>.Resolved(new List<T>());
            //
            // List<SaveRef<T>> toSave = new();
            // List<SaveRef<T>> toCreate = new();
            //
            // void RestoreDirtyKeys()
            // {
            //     foreach (var o in toSave)
            //         foreach (var k in o.keys)
            //             o.o.SetKeyDirty(k);
            // }
            // // Debugger.Print($"Saving {objs.Count} objects");
            // objs.ForEach(o =>
            // {
            //     var k = o.GetDirtyKeys();
            //     // Debugger.Print($"Saving {o.ClassName}, {o.Uid} with keys: {string.Join(", ", k)}");
            //     if (k.Count == 0) return;
            //     o.ClearDirtyKeys();
            //     if (o.IsNew)
            //     {
            //         toCreate.Add(new SaveRef<T> {o = o, keys = k, temp = o});
            //     }
            //     else
            //     {
            //         var temp = EmptyReference<T>(o.Uid);
            //         foreach (var key in k)
            //         {
            //             o.TransferPropValue(key, temp);
            //         }
            //         toSave.Add(new SaveRef<T> {o = o, keys = k, temp = temp});
            //     }
            // });
            // IPromise SaveAll() => Client.SaveObjectsAsync(toSave.Select(a => a.temp.Underlying)).ToPromise()
            //     .Then(() =>
            //     {
            //         //only updated not created so nothing changed in the object.
            //         // Cache.AddToCache(objs);
            //     })
            //     .OnCatch(e => RestoreDirtyKeys())
            //     .CatchWrap("Failed to save objects. ");
            //
            // IPromise CreateAll()
            // {
            //        Debugger.PrintJson(toCreate[0].o);
            //        Debugger.PrintJson(toCreate[0].keys);
            //        Debugger.PrintJson(toCreate[0].o.Underlying);
            //     return toCreate.Select(a => a.o.Underlying.SaveAsync().ToPromise().Then(() =>
            //         {
            //             // Debugger.Print("Created object: " + a.o.Uid);
            //             Cache.AddToCache(ref a.o);
            //         })
            //         .OnCatch(e => a.o.SetKeysDirty(a.keys)))
            //     .Aggregate((prev, next) =>
            //         prev.Then(() => next));
            // }
            //
            // // Debugger.Print($"toCreateLength: {toCreate.Count} toSaveLength: {toSave.Count}, objsLength: {objs.Count}");
            // if(toSave.Count > 0 && toCreate.Count > 0)
            //     return SaveAll().Then(CreateAll).Then(() => objs);
            // if(toSave.Count > 0)
            //     return SaveAll().Then(() => objs);;
            // if(toCreate.Count > 0)
            //     return CreateAll().Then(() => objs);
            //
            //
            //
            //
            // //This could happen if no objects has dirty keys.
            // return Promise<List<T>>.Resolved(objs);
            // // throw new Exception("This should not happen, something is wrong.");


        });

    /// <summary>
    /// Deletes the specified objects on the server.
    /// </summary>
    /// <exception cref="Exception"> If any object has an invalid Uid </exception>
    public static IPromise DeleteObjectsAsync<T>(params T[] objects) where T : DbObject =>
        ThrowIfNotSignedIn(() =>
        {
            if (objects.Any(o => o.Uid.Length != 10)) throw new Exception("Can't delete non-existing object.");

            if (objects.Length == 0) return Promise.Resolved();
            if (objects.Length == 1) return objects[0].Underlying.DeleteAsync().ToPromise().Then(() => Cache.RemoveRangeFromCache(objects));
            Debugger.Print($"Deleting {objects.Length} objects");
            return Client.DeleteObjectsAsync(objects.Select(x => x.Underlying)).ToPromise().Then(() =>
                    {
                        Cache.RemoveRangeFromCache(objects);
                    });
        });


    /// <summary>
    /// Get the raw json object from the server.
    /// </summary>
    public static IPromise<ParseObject> GetObjectJsonAsync(string className, string uid) =>
        Client.GetQuery(className).GetAsync(uid).ToPromise();


    private static T UnderlyingTo<T>(object x) where T : DbObject
    {
        return x switch
        {
            ParseUser user => (T)(object)DbObject.Instantiate<DbUser>(user),
            ParseObject po => (T)(object)DbObject.Instantiate<T>(po),
            _ => null
        };
    }
    #endregion Objects

    #region Connect

    /// <summary>
    ///     Use the following code to initialize the Database.
    ///     <code>
    ///     using System;
    ///     using UnityEngine;
    ///     using Parse.Infrastructure;
    ///     //params
    ///     new LateInitializedMutableServiceHub { },
    ///     new MetadataMutator
    ///     {
    ///         EnvironmentData = new EnvironmentData { OSVersion = SystemInfo.operatingSystem, Platform = $"Unity {Application.unityVersion} on {SystemInfo.operatingSystemFamily}", TimeZone = TimeZoneInfo.Local.StandardName },
    ///         HostManifestData = new HostManifestData { Name = Application.productName, Identifier = Application.productName, ShortVersion = Application.version, Version = Application.version }
    ///     },
    ///     new AbsoluteCacheLocationMutator
    ///     {
    ///         CustomAbsoluteCacheFilePath = $"{Application.persistentDataPath.Replace('/', Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}Parse.cache"
    ///     }</code>
    /// </summary>
    public static void ConnectUnity(LateInitializedMutableServiceHub serviceHub, MetadataMutator mutator,
        AbsoluteCacheLocationMutator absoluteCacheLocationMutator)
    {
        Client = new ParseClient(AppId, ServerUrl, "", serviceHub, mutator, absoluteCacheLocationMutator);
        Client.Publicize();
        Client.GetCurrentUser();
    }

    /// <summary>
    /// Initialize the database with the default settings, needed to be able to connect with the server.
    /// </summary>
    public static void Connect(bool dev = false)
    {
        UseDevEndpoint(dev);
        Debugger.Print(ServerUrl);
        Client = new ParseClient(new ServerConnectionData
        {
            ApplicationID = AppId,
            ServerURI = ServerUrl,
            Key = "", // This is unnecessary if a value for MasterKey is specified.
                      // MasterKey = "Your Master Key",
                      // Headers = new Dictionary<string, string>
                      // {
                      //     ["X-Extra-Header"] = "Some Value"
                      // }
        });
        Client.Publicize();
    }


    #endregion Connect

    #region Commerce


    public class VoucherResult
    {
        /// <summary>The voucher description.</summary>
        [JsonProperty("desc")]
        public string Description;

        /// <summary>The voucher type.</summary>
        [JsonProperty("type")]
        public DbVoucher._VoucherType Type;

        /// <summary>The link to an checkout session if necessary.</summary>
        [JsonProperty("url")]
        public string PaymentLink;

        /// <summary>True if a payment is still needed to unlock this content.</summary>
        public bool IsPaymentRequired => !string.IsNullOrWhiteSpace(PaymentLink);

        /// <summary>Holds the room/voucher id that has been unlocked.</summary>
        [JsonProperty("data")]
        public string Data;
    }

    /// <summary>
    /// Reedem a voucher code. If cache is enabled: Current user will automatically be fetched from the server on success so that the user object is up to date.
    /// </summary>
    /// <param name="voucherCode"> The voucher <see cref="DbVoucher.RedemptionCode"/> </param>
    /// <returns><see cref="VoucherResult"/></returns>
    public static IPromise<VoucherResult> RedeemVoucherCodeAsync(string voucherCode) =>
        CallCloudFunctionAsync<Dictionary<string, object>>("redeemVoucher", new Dictionary<string, object> {
        {"voucherCode", voucherCode}
        }).Then(x => JsonUtil.FromJson<VoucherResult>(JsonUtil.ToJson(x)))
            .ThenKeepVal(x => GetCurrentUser().FetchAsync());

    public static IPromise<string> GenerateSubscriptionDashboardLinkAsync() =>
        CallCloudFunctionAsync<string>("generateSubscriptionDashboardLink");

    #endregion Commerce

    #region Tournament

    // TODO: Theo
    public static byte[][] GetTournamentFiles()
    {
        throw new NotImplementedException();
    }

    // TODO: Theo
    public static void PostTournament(Tournament t)
    {
        throw new NotImplementedException();
    }

    public static List<Tournament> GetTournaments()
    {
        var files = GetTournamentFiles();
        List<Tournament> lst = new();

        foreach (var file in files)
        {
            try
            {
                var text = Encoding.UTF8.GetString(file);
                Tournament t = Tournament.FromPBNText(text);
                lst.Add(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in reading file: " + ex.ToString());
            }
        }
        return lst;
    }

    #endregion

    #region Roles

    /// <summary> Remove role from user. </summary>
    /// <remarks> Only admin can do this. </remarks>
    public static IPromise AssignRoleAsync(DbUser user, string roleName) =>
        CallCloudFunctionAsync<string>("assignRole", new Dictionary<string, object> {
        {"uid", user.Uid},
        {"role", roleName}
        }).Empty();

    /// <summary> Remove role from user. </summary>
    /// <remarks> Only admin can do this. </remarks>
    public static IPromise StripRoleAsync(DbUser user, string roleName) =>
        CallCloudFunctionAsync<string>("stripRole", new Dictionary<string, object> {
        {"uid", user.Uid},
        {"role", roleName}
        }).Empty();

    /// <summary> Assigns the user to the room moderator role and adds user to the room. </summary>
    /// <remarks> Only admin can do this. </remarks>
    public static IPromise AssignRoomModAsync(DbUser user, DbRoom room) =>
        CallCloudFunctionAsync<string>("assignRoomMod", new Dictionary<string, object> {
        {"uid", user.Uid},
        {"room", room.Uid}
        }).Empty();

    /// <summary> Remove moderator role from user for this room. </summary>
    /// <remarks> Only admin can do this. </remarks>
    public static IPromise DemoteRoomModAsync(DbUser user, DbRoom room) => 
        StripRoleAsync(user, $"roomMod-{room.Uid}");

    #endregion Roles

    // TODO: Simplify CloudFunction workflow
    // public class CloudFunction
    // {
    //     
    // }
    // public class CloudFunctions
    // {
    //     static const CloudFunction RemoveFriend = new CloudFunction("removeFriend");
    // }
    public static IPromise<T> CallCloudFunctionAsync<T>(string name, Dictionary<string, object> parameters = null) =>
        Client.CallCloudCodeFunctionAsync<T>(name, parameters).ToPromise();



    #region AuthCheck

    /// Whether the Database Client has an currently signed in user. 
    public static bool IsAuthenticated => client?.GetCurrentUser()?.IsAuthenticated ?? false;

    /// Whether the Database Client has been Initialized.
    public static bool IsConnected => client != null;

    internal static void ThrowIfNotConnected()
    {
        if (!IsConnected) throw new Exception("Client is not connected");
    }

    internal static T ThrowIfNotSignedIn<T>(Func<T> f)
    {
        if (IsAuthenticated) return f();
        throw new Exception("Client is not authenticated");
    }

    internal static void ThrowIfNotSignedIn(Action a)
    {
        if (IsAuthenticated) a();
        else throw new Exception("Client is not authenticated");
    }

    internal static void ThrowIfNotSignedIn()
    {
        if (!IsAuthenticated) throw new Exception("Client is not authenticated");
    }

    #endregion


}
