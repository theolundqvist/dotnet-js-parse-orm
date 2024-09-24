using System.Runtime.Serialization;
using Newtonsoft.Json;

// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming

namespace Project.FirebaseORM.Database.Data;
[Obsolete("Use new Project.Backend")]
public class AuthUser
{ 
    [JsonProperty("nationality")]
    public string Nationality = "";
    
    [JsonProperty("dateOfBirth")]
    public string DateOfBirth = "";
    
    [JsonProperty("firstName")]
    public string FirstName = "";
    
    [JsonProperty("lastName")]
    public string LastName = "";
}