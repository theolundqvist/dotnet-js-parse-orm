using Bridgestars.Util;
using Bridgestars.Firebase;
using Bridgestars.Firebase.Authentication;
using Bridgestars.Firebase.Database;
using Bridgestars.Firebase.Database.Data;


var db = new BridgeDatabase(); 

string GetEmail(){
    System.Console.Write("email: ");
    return Console.ReadLine();
}

string GetPassword(){
    System.Console.Write("password: ");
    string password = null;
    while (true)
    {
        var key = System.Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
            break;
        password += key.KeyChar;
    }
    return password;
}

Credential SignIn(string email="", int failedAttempts = 1){

    if(string.IsNullOrEmpty(email)) email = GetEmail();
    var password = GetPassword();

    cred = Auth.SignInWithEmail(email, password).Catch(e => {
        Console.WriteLine("");
        if(e.Message.Contains("INVALID_PASSWORD")) {
            if(failedAttempts == 3) throw new Exception("Failed too many times. Run 'SignIn()' to try again.");
            Console.WriteLine("Invalid password, try again;");
            return SignIn(email, failedAttempts+1);
        }
        else {
            Console.WriteLine("ERROR: " + e.Message);
            Console.WriteLine("Do you want to try again? (y/n)");
            var resp = Console.ReadLine().ToLower();
            if(resp == "y") {
                return SignIn();
            }
            if(resp == "n"){
                Console.WriteLine("Run 'SignIn()' to try again.");
            }
        }

        throw new Exception("Failed to sign in.");
    }).Catch(e => {
        Console.WriteLine(e.Message);
        throw e;
        }).Await(999999999);

    return cred;
}


Console.WriteLine("\n\nEnter your credentials to sign in to Firebase;");
var cred = SignIn();
if(cred != null){
    db.SetCredential(cred);
    Auth.SetDatabaseReference(ref db);
    Console.WriteLine("var db = BridgeDatabase ref");
    Console.WriteLine("var cred = Your Credential obj.");
}






