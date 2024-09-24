using System.Runtime.Serialization;
using Project.FirebaseORM.Cloud;
using Project.FirebaseORM.Database;
using Project.FirebaseORM.Database.Data;
using Project.Util;
using Project.Util.WebUtil;
using Newtonsoft.Json;
using RSG;
using Debugger = Project.Util.Debugger;

namespace Project.FirebaseORM.Authentication
{
    [Obsolete("Use new Project.Backend")]
    public static class Auth
    {
        public const int SHORTEST_USERNAME_LENGTH = 4;
        public const int LONGEST_USERNAME_LENGTH = 16;
        public const int SHORTEST_PASSWORD_LENGTH = 8;
        private static string API_KEY = FirebaseCredentials.API_KEY;
        
        [Obsolete]
        public static void SetApiKey(string apiKey)
        {
            API_KEY = apiKey;
            Functions.SetApiKey(apiKey);
        }

        private static UserDatabase db = null;
        public static void SetDatabaseReference<T>(ref T _db) where T : UserDatabase
        { db = _db; Debugger.Print("Database ref set"); }


        /// <summary>
        /// Given an email and password, signs in to firebase and returns the user's credentials. <br/>
        /// This operation requires an additional call to cloud functions to get username corresponding to email.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public static IPromise<Credential> SignInWithEmail(string email, string password)
        {
            return Functions.AuthorizedRequest(Functions.GetUsernameFromEmail)
                .AddParam("email", email)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Then(username => SignInWithUsername(username, password).Catch(e =>
                {
                    if (e.InnerException?.Message == "USERNAME_NOT_FOUND")
                        throw new Exception("EMAIL_NOT_FOUND", e);
                    throw e;
                }));
        }
        
        /// <summary>
        /// Given an username and password, signs in to firebase and returns the user's credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static IPromise<Credential> SignInWithUsername(string username, string password)
        {
            Debugger.Print("SIGNING IN WITH USERNAME: \n" + username);
            Debugger.Print(username.ToLower() + ".account@bridgestars.net");
            

            return
                new WebRequest()
                    .AddParam("email", username.ToLower() + ".account@bridgestars.net")
                    .AddParam("password", password)
                    .AddParam("returnSecureToken", "true")
                    .SetContentType(WebRequest.ContentType.json)
                    .SetEndpoint("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + API_KEY)
                    .POST_STRING()
                    .Catch(e => throw FirebaseException.Parse(e))
                    .Catch(e =>
                    {
                        if (e.Message == "EMAIL_NOT_FOUND") throw new Exception("USERNAME_NOT_FOUND", e);
                        throw e;
                    })
                    .Then(JsonUtil.FromJson<Credential>)
                    .ThenKeepVal(cred => Debugger.Print("SIGNED IN WITH UID: \n" + cred.uid))
                    .CatchWrap("Sign in failed: ")
                    .PrintErrorMessage();
            //.OnFailure(err => { });
        }

        /// <summary>
        /// Creates an account with username and data documents, also checks if username, email and password are valid.
        /// Resolves with the user's UID.
        ///<br/>
        /// The user can now sign in and access data documents.
        /// </summary>
        /// <param name="password">The desired password, at least 6 characters</param>
        /// <param name="username">The desired username, 4-16 characters </param>
        /// <param name="email"> The desired email, has to upheld som standards</param>
        /// <param name="authUser"> Optional - protected user data will be stored in auth object</param>
        /// <returns> User's credentials </returns>
        public static IPromise<string> SignUpWithUsername(string username, string email,
            string password, AuthUser authUser = null)
        {
            var p = new Promise<string>();
            if (username.Length < SHORTEST_USERNAME_LENGTH)
                return p.Reject("Username too short, minimum length is: " + SHORTEST_USERNAME_LENGTH);
            if (username.Length > LONGEST_USERNAME_LENGTH)
                return p.Reject("Username too long, maximum length is: " + LONGEST_USERNAME_LENGTH);
            if (password.Length < SHORTEST_PASSWORD_LENGTH)
                return p.Reject("Password too short, minimum length is: " + SHORTEST_PASSWORD_LENGTH);
            if (!email.Contains("@"))
                return p.Reject("Invalid email");
            return Functions.AuthorizedRequest(Functions.SignUp)
                .AddParam("username", username)
                .AddParam("email", email)
                .AddParam("password", password)
                .AddParam("nationality", authUser?.Nationality ?? "")
                .AddParam("lastName", authUser?.LastName ?? "")
                .AddParam("firstName", authUser?.FirstName ?? "")
                .AddParam("dateOfBirth", authUser?.DateOfBirth ?? "")
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .ThenKeepVal(uid => Debugger.Print("SIGNED UP WITH UID: \n" + uid)); // only for illustration
        }
        

        
        /// <summary>
        /// Deletes account belonging to <b><paramref name="cred"/></b>, does not remove data belonging to user.
        /// </summary>
        [Obsolete("Does not remove user data")]
        internal static IPromise DeleteAccount(Credential cred)
        {
            Debugger.Print("DELETING ACCOUNT");
            return
                new WebRequest()
                    .SetContentType(WebRequest.ContentType.json)
                    .AddParam("idToken", cred.idToken)
                    .SetEndpoint("https://identitytoolkit.googleapis.com/v1/accounts:delete?key=" + API_KEY)
                    .POST_STRING()
                    .Then(content =>
                    {
                        Debugger.Print(content);
                    })
                    .Catch(e => throw FirebaseException.Parse(e))
                    .CatchWrap("ERROR WHEN DELETING ACCOUNT: \n");
        }
        
        /// <summary>
        /// Deletes account belonging to <b><paramref name="cred"/></b>, removes all data belonging to user.
        /// </summary>
        public static IPromise DeleteAccountAndData(Credential cred)
        {
            Debugger.Print("DELETING ACCOUNT");
            return Functions.AuthorizedRequest(Functions.AuthDeleteAccount, cred)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .CatchWrap("ERROR WHEN DELETING ACCOUNT: \n")
                .Empty();
        }


        /// <summary>
        /// Resolves with an <see cref="ProtectedUserData"/> object, all fields will be visible. <br/>
        /// </summary>
        /// <param name="cred"> The credential object of the user </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IPromise<ProtectedUserData> GetProtectedFields(Credential cred) =>
            GetProtectedFields(cred.uid, cred);
        
        /// <summary>
        /// Resolves with an <see cref="ProtectedUserData"/> object, fields which the user are not authorized to view have the value "private". <br/>
        /// </summary>
        /// <param name="uid"> The user id of the user which fields are queried. </param>
        /// <param name="cred"> The credential object of the user who performs the request </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IPromise<ProtectedUserData> GetProtectedFields(string uid, Credential cred) =>
            Functions.AuthorizedRequest(
                    Functions.GetVisibleProtectedFields, cred ?? db.GetCredential())
                .AddParam("uid", uid)
                .GET_STRING()
                .Then(JsonUtil.FromJson<ProtectedUserData>)
                .Catch(e => throw FirebaseException.Parse(e));
        
        /// <summary>
        /// Updates the protected fields on an user´s auth object. <br/>
        /// </summary>
        /// <param name="fields"> <see cref="ProtectedUserData"/> obj. </param>
        /// <param name="cred"> The credential object of the user who performs the request </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IPromise UpdateProtectedFields(ProtectedUserData fields, Credential cred) =>
            Functions.AuthorizedRequest(Functions.UpdateProtectedFields, cred)
                .AddParam("fields", JsonUtil.ToJson(fields))
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();


        /// <summary>
        /// Update an user's email, makes sure that all documents are updated correctly and that the new email is valid.
        /// </summary>
        /// <param name="newEmail"> The wanted email </param>
        /// <param name="cred"> The user's <see cref="Credential"/> obj. </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IPromise UpdateEmail(string newEmail, Credential cred) =>
            Functions.AuthorizedRequest(Functions.AuthUpdateEmail, cred)
                .AddParam("new_email", newEmail)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();

        /// <summary>
        /// Send password reset email to <b><paramref name="email"/></b>.
        /// </summary>
        /// <param name="email"></param>
        public static IPromise SendPasswordResetEmail(string email)
        {
            Debugger.Print("SENDING PASSWORD RESET EMAIL: \n" + email);
            return Functions.AuthorizedRequest(Functions.SendPasswordResetEmail)
                .AddParam("email", email)
                .GET_STRING()
                .Catch(e => throw FirebaseException.Parse(e))
                .Empty();
            // Debugger.Print("Can't send password reset email, method not implementeed");
            // return Rejected("Method not implemented, cant send password reset email.");
            // return
            //     new WebRequest()
            //         .AddParam("requestType", "PASSWORD_RESET")
            //         .AddParam("email", email)
            //         .AddParam("X-Firebase-Locale", "EN")
            //         .SetContentType(WebRequest.ContentType.json)
            //         .SetEndpoint("https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + API_KEY)
            //         .POST()
            //         .Catch(e => throw FirebaseException.Parse(e))
            //         .CatchWrap("ERROR WHEN SENDING PASSWORD RESET EMAIL: \n")
            //         //.Deserialize<Credential>()
            //         .Then(content =>
            //         {
            //             Debugger.Print("PASSWORD RESET EMAIL SENT: \n" + email + "\n");
            //         });

        }
        
        // private static IPromise<Credential> SignUpWithEmail(string email, string password)
        // {
        //     Debugger.Print("SIGNING UP WITH EMAIL: \n" + email);
        //     return
        //         new WebRequest()
        //             .AddParam("password", password)
        //             .AddParam("email", email)
        //             .AddParam("returnSecureToken", "true")
        //             .SetContentType(WebRequest.ContentType.json)
        //             .SetEndpoint("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + API_KEY)
        //             .POST()
        //             .Catch(e =>
        //                 throw FirebaseException.Parse(e)
        //             )
        //             .Then(JsonUtil.FromJson<Credential>)
        //             .ThenKeepVal(cred =>
        //             {
        //                 Debugger.Print("SIGNED UP WITH EMAIL, USER IS ASSIGNED UID: \n" + cred.uid);
        //             })
        //             .CatchWrap("ERROR WHEN SIGNING UP WITH EMAIL. \n");
        // }

        [Obsolete("Use new Project.Backend")]
        private class RefreshTokenResponse
        {
#pragma warning disable CS0649
            public string refresh_token;
            public string expires_in;
            public string id_token;
            public string user_id;
#pragma warning restore CS0649
        }

        /// <summary>
        /// Given a credential, returns a new credential with 60 more minutes of access time.
        /// </summary>
        public static IPromise<Credential> RefreshUserCredential(Credential cred)
        {
            return RefreshUserToken(cred.refreshToken);
        }

        /// <summary>
        /// Use RefreshUserCredential instead
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        [Obsolete]
        public static IPromise<Credential> RefreshAccessToken(string refreshToken)
        {
            return RefreshUserToken(refreshToken);
        } 
        
        private static IPromise<Credential> RefreshUserToken(string refreshToken)
        {
            Debugger.Print("TRYING TO REFRESH ACCESSTOKEN");
            string endpoint = "https://securetoken.googleapis.com/v1/token?key="+ API_KEY;
            //string payload = "grant_type=refresh_token&refresh_token=" + refreshToken;

            return new WebRequest()
                .AddHeader("grant_type", "refresh_token")
                .AddHeader("refresh_token", refreshToken)
                .SetEndpoint(endpoint)
                .SetContentType("application/x-www-form-urlencoded")
                .POST_STRING()
                .ThenKeepVal(s => Debugger.Print("TOKEN HAS BEEN REFRESHED\n"))
                .Catch(e => throw FirebaseException.Parse(e))
                .Then(JsonUtil.FromJson<RefreshTokenResponse>)
                .Then(RTR =>
                {
                    var cred = new Credential();
                    cred.localId = RTR.user_id;
                    cred.idToken = RTR.id_token;
                    cred.expiresIn = RTR.expires_in;
                    cred.refreshToken = RTR.refresh_token;
                    return cred;
                });
        }

        private static IPromise<G> Rejected<G>(string s)
        {
            Debugger.Print(s);
            return new Promise<G>().Reject(s);
        }

        private static IPromise Rejected(string s)
        {
            Debugger.Print(s);
            return new Promise().Reject(s);
        }

    }
    [Obsolete("Use new Project.Backend")]
    public class Credential
    {

        public string idToken;
        public string email;
        public string refreshToken;
        public string expiresIn;
        public string localId;
        public bool registered;


        public bool HasExpired() => validTo.Subtract(DateTime.Now).TotalSeconds < 10;

        private DateTime _creationDate = DateTime.Now;
        public DateTime validTo => _creationDate.AddSeconds(int.Parse(expiresIn));
        
        public string uid { get => localId; set => localId = value; }
        public string accessToken => idToken;
    }
    [Obsolete("Use new Project.Backend")]
    public class ProtectedUserData
    {
        [JsonProperty("nationality")]
        public string Nationality = "";
        [JsonProperty("timeOfBirth")]
        public string TimeOfBirth = "";
        [JsonProperty("firstName")]
        public string FirstName = "";
        [JsonProperty("lastName")]
        public string LastName = "";
        [JsonProperty("publicFields")]
        public string[] PublicFields;

        public override string ToString() => JsonUtil.ToJson(this);
    }
}