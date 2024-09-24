namespace Project.FirebaseORM.Database.Data
{
    
    /// <summary>
    /// A generic representation of a user on the Firestore database. Inherit from this and create your own customized user.
    /// </summary>
    [Obsolete("Use new Project.Backend")]
    public class FirestoreUser : FirestoreDocument
    {
        /// <summary>
        /// Tries to parse all standard user fields, such as username and friends.
        /// </summary>
        /// <param name="json"></param>
        /// <exception cref="ArgumentException"> Will throw if the specified json doesn't conform to firestore document rules.</exception>
        public FirestoreUser(string json=null) : base(json)
        {
            TryGetValue("username", ref username);
            TryGetValue("usernameLower", ref usernameLower);
            TryGetValue("img", ref img);
            TryGetArray("friends", ref friends);
            TryGetArray("chats", ref chats);

            
            TryGetArray("friendRequests", ref incomingFriendRequests); 
            TryGetArray("incomingFriendRequests", ref incomingFriendRequests);
            
            TryGetArray("outgoingFriendRequests", ref outgoingFriendRequests);
        }

        /// <summary>
        /// Used when creating document without existing data. Pass json string to build from database value.
        /// </summary>
        /// 

        public string username;
        public string usernameLower;
        public string img;
        public string[] friends;
        public string[] chats;
        
        /// <summary>
        /// Renamed.
        /// Is now a reference to incomingFriendRequests
        /// </summary>
        [Obsolete]
        public string[] friendRequests
        {
            get => incomingFriendRequests;
            set => incomingFriendRequests = value;
        }
        public string[] incomingFriendRequests;
        public string[] outgoingFriendRequests;
    }


}