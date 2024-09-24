

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Project.FirebaseORM.Database.Data;



// public class BridgeUsername : FirestoreDocument
// {
//     public BridgeUsername(string json=null) : base(json)
//     {
//         TryGetValue("email", ref email);
//                     
//     }
//     public string email;
// }
// public class BridgeEmail : FirestoreDocument
// {
//     public BridgeEmail(string json=null) : base(json)
//     {
//         TryGetValue("username", ref username);
//                     
//     }
//     public string username;
// }

/// <summary>
/// Used to represent a Project user on the firestore database.
/// </summary>
[Obsolete("use Project.Backend.Data.DbUser, obs breaking change")]
public class BridgeFirestoreUser : FirestoreUser
{
    /// <summary>
    /// Create User from json data string, will try to parse document details and make fields available. 
    /// </summary>
    /// <param name="json"> The json data string to parse. </param>
    /// <exception cref="ArgumentException"> Will throw if the specified json doesn't conform to firestore document rules.</exception>
    public BridgeFirestoreUser(string json=null) : base(json)
    {
        TryGetValue("balance", ref balance);
        TryGetValue("elo", ref elo);
        TryGetValue("xp", ref xp);
        TryGetArray("matchHistory", ref matchHistory);
    }
    
    public int elo;
    public int xp;
    public double balance;
    public string[] matchHistory;
}
[Obsolete("Use new Project.Backend")]
public class BridgeFirestoreMatchHistory : FirestoreDocument
{
    /// <summary>Create from serialized or local data</summary>
    public BridgeFirestoreMatchHistory(string json=null) : base(json)
    {
        TryGetObject("TableOne", ref TableOne);
        TryGetObject("TableTwo", ref TableTwo);
        TryGetValue("StartTime", ref StartTime);
        TryGetValue("EndTime", ref EndTime);
        //TryGetMap()
        MatchId = DocId;
    }

    
    public bool IsTeamGame => (TableOne != null && TableTwo != null) && (TableOne.HasId() && TableTwo.HasId()); //TableOne != null && TableTwo != null || (!TableOne.HasId() && !TableTwo.HasId());

    /// <summary>Will be used as DocId upon upload</summary>
    [NonSerialized]
    public string MatchId;

    public long StartTime;
    public long EndTime;

    public BridgeFirestoreTable TableOne;
    public BridgeFirestoreTable TableTwo;
}

/// <summary>
/// Used to represent a Bridge Table on the Firestore database
/// </summary>
[Obsolete("Use new Project.Backend")]
public class BridgeFirestoreTable : FirestoreObject
{
    public BridgeFirestoreTable(string json=null) : base(json)
    {
        TryGetValue("Data", ref Data);
        TryGetValue("Id", ref Id);
        TryGetValue("PlayerNorth", ref PlayerNorth);
        TryGetValue("PlayerSouth", ref PlayerSouth);
        TryGetValue("PlayerEast", ref PlayerEast);
        TryGetValue("PlayerWest", ref PlayerWest);
    }

    public bool HasId() => !string.IsNullOrEmpty(Id);

    public string Data;
    public string Id;
    public string PlayerNorth;
    public string PlayerSouth;
    public string PlayerEast;
    public string PlayerWest;
}