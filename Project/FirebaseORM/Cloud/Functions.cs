// ReSharper disable InconsistentNaming

using Project.FirebaseORM.Authentication;
using Project.Util;
using Project.Util.WebUtil;
using RSG;

namespace Project.FirebaseORM.Cloud;
[Obsolete("Use new Project.Backend")]
public static class Functions
{
    
    
 
    // public const string CreateVersionData = "http://localhost:5001/bridge-fcee8/us-central1/createVersionData";
    // public const string GetLauncherVersionWindows = "http://localhost:5001/bridge-fcee8/us-central1/getLauncherVersionWindows";
    // public const string GetLauncherVersionMac = "http://localhost:5001/bridge-fcee8/us-central1/getLauncherVersionMac";
    // public const string GetVisibleProtectedFields = "http://localhost:5001/bridge-fcee8/us-central1/getVisibleProtectedFields";
    // public const string SetMyVisibleProtectedFields = "http://localhost:5001/bridge-fcee8/us-central1/setMyVisibleProtectedFields";
    // public const string GetMyProtectedFields = "http://localhost:5001/bridge-fcee8/us-central1/getMyProtectedFields";
    // public const string AuthDeleteAccount = "http://localhost:5001/bridge-fcee8/us-central1/authDeleteAccount";
    // public const string AuthUpdateUsername = "http://localhost:5001/bridge-fcee8/us-central1/authUpdateUsername";
    // public const string AuthMigrateAccount = "http://localhost:5001/bridge-fcee8/us-central1/authMigrateAccount";
    // public const string AuthUpdateEmail = "http://localhost:5001/bridge-fcee8/us-central1/authUpdateEmail";
    // public const string GetUsernameFromEmail = "http://localhost:5001/bridge-fcee8/us-central1/getUsernameFromEmail";
    // public const string SignUp = "http://localhost:5001/bridge-fcee8/us-central1/signUp";
    // public const string SendFriendRequest = "http://localhost:5001/bridge-fcee8/us-central1/sendFriendRequest";
    // public const string AcceptFriendRequest = "http://localhost:5001/bridge-fcee8/us-central1/acceptFriendRequest";
    // public const string RemoveFriend = "http://localhost:5001/bridge-fcee8/us-central1/removeFriend";

    //public const string CreateVersionData = "https://us-central1-bridge-fcee8.cloudfunctions.net/createVersionData";
    //public const string GetLauncherVersionWindows = "https://us-central1-bridge-fcee8.cloudfunctions.net/getLauncherVersionWindows";
    //public const string GetLauncherVersionMac = "https://us-central1-bridge-fcee8.cloudfunctions.net/getLauncherVersionMac";

    /// <summary>
    ///  <b>params</b> : uid <br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> A json all protected fields, if a field is private then the value of the field will be "private".  </returns>
    /// <throws> FirebaseException </throws> 
    public const string GetVisibleProtectedFields = "https://us-central1-bridge-fcee8.cloudfunctions.net/getVisibleProtectedFields";
    
    /// <summary>
    ///  Updates the protected fields on an users auth object, fields must be an serialized <see cref="ProtectedUserData"/> obj. <br/>
    ///  <b>params</b> : fields <br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string UpdateProtectedFields = "https://us-central1-bridge-fcee8.cloudfunctions.net/updateProtectedFields";
    
    /// <summary>
    ///  Makes sure that ALL documents and auth are updated. Checks if username is available.<br/>
    ///  <b>params</b> : new_username <br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException </throws>
    public const string AuthUpdateUsername = "https://us-central1-bridge-fcee8.cloudfunctions.net/authUpdateUsername";
    
    /// <summary>
    ///  Makes sure that ALL documents and account are moved to the new system.<br/>
    ///  <b>params</b> : email<br/>
    ///  <b>pass cred</b> : false    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException - may be in a wierd format  </throws>
    public const string AuthMigrateAccount = "https://us-central1-bridge-fcee8.cloudfunctions.net/authMigrateAccount";
    
    /// <summary>
    ///  Makes sure that ALL documents and account are updated. Checks new email for valid format.<br/>
    ///  <b>params</b> : new_email<br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException </throws>
    public const string AuthUpdateEmail = "https://us-central1-bridge-fcee8.cloudfunctions.net/authUpdateEmail";
    
    /// <summary>
    ///  Makes sure that ALL documents and account are deleted.<br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException </throws>
    public const string AuthDeleteAccount = "https://us-central1-bridge-fcee8.cloudfunctions.net/authDeleteAccount";
    
    /// <summary>
    ///  Used for sign in, gets the username corresponding to a specific email.<br/>
    ///  <b>params</b> : email<br/>
    ///  <b>pass cred</b> : false    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException </throws>
    public const string GetUsernameFromEmail = "https://us-central1-bridge-fcee8.cloudfunctions.net/getUsernameFromEmail";
    
    /// <summary>
    ///  Create an account for an user. Checks username, password and email for valid format etc.<br/>
    ///  <b>params</b> : email, username, password<br/>
    ///  <b>optional</b> : nationality, timeofbirth, firstname, lastname<br/>
    ///  <b>pass cred</b> : false    <br/>
    /// </summary>
    /// <returns> uid </returns>
    /// <throws> FirebaseException  </throws>
    public const string SignUp = "http://us-central1-bridge-fcee8.cloudfunctions.net/signUp";
    
    /// <summary>
    ///  Creates a friend request from the sender to the receiver. pass the uids of the sender and the receiver<br/>
    ///  <b>params</b> : sender, receiver<br/>
    ///  <b>pass cred</b> : true, must be the cred of the sender/admin    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string SendFriendRequest = "https://us-central1-bridge-fcee8.cloudfunctions.net/sendFriendRequest";
    
    /// <summary>
    ///  Accepts a friend request from the sender to the receiver. Pass the uids of the sender and the receiver<br/>
    ///  <b>params</b> : sender, receiver<br/>
    ///  <b>pass cred</b> : true, must be the cred of the receiver/admin    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string AcceptFriendRequest = "https://us-central1-bridge-fcee8.cloudfunctions.net/acceptFriendRequest";
    
    /// <summary>
    ///  Remove friend/Deny friend request. Pass the uids of the sender and the receiver<br/>
    ///  <b>params</b> : sender, receiver<br/>
    ///  <b>pass cred</b> : true, can be the cred of the receiver or the sender or admin  <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string RemoveFriend = "https://us-central1-bridge-fcee8.cloudfunctions.net/removeFriend";
    
    /// <summary>
    ///  Resolves if the username is not already taken by another user <br/>
    ///  <b>params</b> : username <br/>
    ///  <b>pass cred</b> : true    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string AuthIsUsernameAvailable = "https://us-central1-bridge-fcee8.cloudfunctions.net/authIsUsernameAvailable";

    /// <summary>
    ///  Resolves if the email exists. Send an email with an password reset link. <br/>
    ///  <b>params</b> : email <br/>
    ///  <b>pass cred</b> : false    <br/>
    /// </summary>
    /// <returns> "OK" </returns>
    /// <throws> FirebaseException  </throws>
    public const string
        SendPasswordResetEmail = /*"http://localhost:5001/bridge-fcee8/us-central1/sendPasswordResetEmail";*/"https://us-central1-bridge-fcee8.cloudfunctions.net/sendPasswordResetEmail";
    
    private static string API_KEY = FirebaseCredentials.API_KEY;

    internal static void SetApiKey(string apiKey) => API_KEY = apiKey;
    
    public static WebRequest AuthorizedRequest(string endpoint, Credential cred) =>
        AuthorizedRequest(endpoint).AddParam("idToken", cred.idToken);
    public static WebRequest AuthorizedRequest(string endpoint) =>
        new WebRequest().SetEndpoint(endpoint).AddParam("apiKey", API_KEY);

    /// <summary>
    ///  Relocates documents and fields to support the new system. Allows the user to now sign in with username/email securely. An account that hasn't been migrated since the old version will not be able to sign in. <br/>
    ///  <b>params</b> : email <br/>
    ///  <b>pass cred</b> : false <br/>
    /// </summary>
    /// <throws> FirebaseException - may be in a wierd format  </throws>
    public static IPromise MigrateAccount(string email) => AuthorizedRequest(AuthMigrateAccount).AddParam("email", email).GET_STRING().Catch(e => throw FirebaseException.Parse(e)).Empty();


}