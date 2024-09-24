using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RSG;
using Project.Util;
using Project.Util.WebUtil;
using System.Threading;
using Trace = System.Diagnostics.Trace;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using Project.FirebaseORM.Authentication;
using Project.FirebaseORM.Database;
using Project.FirebaseORM.Database.Data;
using Project.FirebaseORM.Database.Transform;
using Debugger = Project.Util.Debugger;

#pragma warning disable CS0618
//[assembly: Parallelize(Workers = 16, Scope = ExecutionScope.MethodLevel)]

namespace Test
{
    public class Mock
    {
        public static BridgeFirestoreUser FirestoreUser => new()
        {
            img = "imgurl",
            friends = new string[2] {"uid1", "uid2"},
            chats = new string[2] {"chatid", "chatid2"},
            elo = 100,
            xp = 110,
            balance = 1000.50,
            matchHistory = new string[2] {"asdasd", "asdasd"},
        };

        public static AuthUser AuthUser => new()
        {
            Nationality = "sweden",
            FirstName = "theo",
            LastName = "lundqvist",
            DateOfBirth = "today"
        };

        public struct userInfo
        {
            public const string username = "TEST_username";
            public static string nextUsername = "TEST_username2";

            public const string email = "test_username@gmail.com";
            public static string nextEmail = "test_username2@gmail.com";

            public static string password = "12345678";
        }
    }

    [TestClass]
    public class FirebaseTester
    {
        public BridgeDatabase DB = new BridgeDatabase();

        public static void EnableLogging(string msg = "")
        {
            //Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            Debugger.SetPrintMethod(s =>
                Console.WriteLine(
                    s)); //System.Diagnostics.Debug.WriteLine(s));//System.Diagnostics.Debug.WriteLine(str));
            if (msg != "") Debugger.Print(msg);
        }

        public static void DisableLogging() => Debugger.SetPrintMethod(str => { });

        private Credential UseAdminCredentialSync()
        {
            var cred = Auth.SignInWithUsername("admin_test_acc", "").Await();
            //var cred = Auth.SignInWithUsername("Castor", "12345678").Await();
            DB.SetCredential(cred);
            return cred;
        }

        [TestInitialize]
        public void Setup()
        {
            ;
            Debugger.Print("____BEGIN SETUP____\n");
            DisableLogging();
            //Debugger.AddWordFilter("POST");
            //UserDatabase db = new BridgeDatabase();
            Auth.SetDatabaseReference(ref DB);
            UseAdminCredentialSync();


            EnableLogging();

            Debugger.Print("____SETUP DONE____\n");
        }

        [TestCleanup]
        public void CleanUp()
        {
            Debugger.Print("____CLEANING UP____\n");
            DisableLogging();

            if (DB.HasValidAccessToken)
            {
                RemoveUser();
            }

            EnableLogging();
        }

        [TestMethod]
        public void TestMatchHistory()
        {
            var time = DateTime.Now;
            var local = new BridgeFirestoreMatchHistory
            {
                MatchId = "testId",
                StartTime = time.ToFileTime(),
                EndTime = time.AddHours(1).ToFileTime(),
                TableOne = new BridgeFirestoreTable
                {
                    Data = "how the match went",
                    Id = "tableId",
                    PlayerEast = "uid",
                    PlayerNorth = "uid",
                    PlayerSouth = "uid",
                    PlayerWest = "uid"
                },
            };

            DB.PostMatchHistoryEntry(local).Await();
            var remote = DB.GetMatchHistoryEntry(local.MatchId).Await();
            Debugger.Print(remote.ToString());

            Assert.IsFalse(string.IsNullOrEmpty(local.DocId));
            Assert.AreEqual(remote.TableOne.ToString(), local.TableOne.ToString());

            Assert.AreEqual(time.ToFileTime(), remote.StartTime);
            Assert.AreEqual(time.AddHours(1).ToFileTime(), remote.EndTime);

            local.TableOne.Data = "test that this change gets posted to the server";
            DB.PostMatchHistoryEntry(local).Await();


            remote = DB.GetMatchHistoryEntry(local.MatchId).Await();
            Assert.AreEqual(local.TableOne.Data, remote.TableOne.Data);


            local = new BridgeFirestoreMatchHistory
            {
                TableOne = new BridgeFirestoreTable
                {
                    Data = "asdasd",
                    Id = "i22222d",
                    PlayerEast = "pe",
                    PlayerNorth = "pn",
                    PlayerSouth = "ps"
                },
            };

            DB.PostMatchHistoryEntry(local, "testId").Await();
            remote = DB.GetMatchHistoryEntry("testId").Await();


            Debugger.Print(local.DocId);
            Assert.IsFalse(string.IsNullOrEmpty(local.DocId));
            Assert.AreEqual(remote.TableOne.ToString(), local.TableOne.ToString());


            Assert.ThrowsException<Exception>(() => DB.AddUserMatchHistoryReference("wrong_uid", "match_id").Await());
            Assert.ThrowsException<Exception>(() => DB.AddUserMatchHistoryReference(null, "match_id").Await());
            Assert.ThrowsException<Exception>(() => DB.AddUserMatchHistoryReference("", "match_id").Await());
        }

        [TestMethod]
        public void TestTransform()
        {
            var doc = DB.CreateDocumentWithAutomaticId(Mock.FirestoreUser, "testing").Await();
            new DocumentTransform("testing/" + doc.DocId)
                .Increment("balance", -0.5)
                .Increment("xp", 100)
                .AppendMissingElements("friends", "theo", "castor")
                .RemoveAllFromArray("friends", "uid1", "uid2")
                .Send(DB).Await();
            
            
            // new DocTransform("testing/" + doc.DocId,
            //     new FieldTransform("friends").RemoveAllFromArray("uid1, uid2"), 
            //     new FieldTransform("friends").AppendMissingElements("theo")).Send(DB).Await();
                // new FieldTransform("elo").Increment(5),
                // new FieldTransform("balance").Increment(-0.5)).Send(DB).Await();
            var doc2 = DB.GetDocument<BridgeFirestoreUser>("testing/" + doc.DocId).Await();
            Assert.AreEqual(doc.balance-0.5, doc2.balance);
            Assert.AreEqual(doc.xp+100, doc2.xp);
            Assert.AreEqual(doc.xp+100, doc2.xp);
            CollectionAssert.AreEqual(new[]{"theo", "castor"}, doc2.friends);
            
            Debug.Print(JsonUtil.Prettify(doc2.ToString()));
            DB.DeleteDocument("testing/" + doc.DocId).Await();
        }


        
        // [TestMethod]
        // public void MoveOldAccountsToEmailIndex()
        // {
        //     Debugger.Disable();
        //     var docs = FirestoreDocument.BuildCollectionFromJson<BridgeUsername>(DB.GetRaw("usernames").Await());
        //     Debugger.Enable();
        //     Debugger.Print(docs.Length);
        //     foreach (var u in docs)
        //     {
        //         Debugger.Print(u.DocId);
        //         Debugger.Disable();
        //         var available = DB.IsUsernameAvailable(u.DocId).ToSuccessBoolean().Await();
        //         Debugger.Enable();
        //         Debugger.Print(available ? "Available" : "NOT AVAILABLE");
        //         
        //         if(!available) DB.DeleteDocument("usernames/"+u.DocId).Await();
        //         else
        //         {
        //             Debugger.Disable();
        //             var alreadyExist = DB.GetDocument<BridgeEmail>("emailIndex/" + u.email).ToSuccessBoolean().Await();
        //             Debugger.Enable();
        //             Debugger.Print(alreadyExist ? "ALREADY DONE" : "NOT DONE");
        //             if(alreadyExist) DB.DeleteDocument("usernames/"+u.DocId).Await();
        //             else
        //             {
        //                 DB.PostDocument(new BridgeEmail{username = u.DocId}, "emailIndex/"+u.email).Await();     
        //             }
        //             
        //         }
        //          
        //
        //     }
        // }

        //[TestMethod]
        public void TestChangePassword()
        {
            //var cred = CreateUser();
        }

        [TestMethod]
        public void CreateAccount()
        {
            Debugger.Print("ensure the account is deleted");
            Auth.SignInWithUsername("auth_tester", "passwordsupersecret")
                .Then(Auth.DeleteAccountAndData)
                .AwaitCatch();

            Debugger.Print("can create account");
            Auth.SignUpWithUsername("AUTH_TESTER", "authTESTER@bridgestars.net", "passwordsupersecret").Await();

            Debugger.Print("can not create second account with same username");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("auth_tesTer", "otherEmail@bridgestars.net", "passwordsupersecret").Await());

            Debugger.Print("can not create second account with same email");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("otherUSername", "authtester@bridgestars.net", "passwordsupersecret").Await());

            Debugger.Print("Can sign in with email");
            Auth.SignInWithEmail("AUTHtester@bridgestars.net", "passwordsupersecret").Await();

            Debugger.Print("Sign in and remove account");
            Auth.SignInWithUsername("auth_tester", "passwordsupersecret")
                .Then(Auth.DeleteAccountAndData)
                .AwaitCatch();

            Debugger.Print("ensure that we can now create a now account");
            Auth.SignUpWithUsername("AUTH_TESTER", "authTESTER@bridgestars.net", "passwordsupersecret").Await();

            Debugger.Print("remove account");
            Auth.SignInWithUsername("auth_tester", "passwordsupersecret")
                .Then(Auth.DeleteAccountAndData)
                .AwaitCatch();


            Debugger.Print("can not create second account with too short username");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("abc", "authtester@bridgestars.net", "passwordsupersecret").Await());

            Debugger.Print("can not create second account with too long username");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("abcdabcdabcdabcdB", "authtester@bridgestars.net", "passwordsupersecret")
                    .Await());

            Debugger.Print("can not create second account with invalid email");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("abcde", "authtester@bridgestars.netasd", "passwordsupersecret").Await());

            Debugger.Print("can not create second account with too short password");
            Assert.ThrowsException<Exception>(() =>
                Auth.SignUpWithUsername("abcdabcdabcdabcdB", "authtester@bridgestars.net", "passwor").Await());
        }


        [TestMethod]
        public void TestGetMultipleUsersParallel()
        {
            //MergeWith to run in parallel.
            var users =
                CreateUser(username: Mock.userInfo.username, email: Mock.userInfo.email)
                    .MergeWith(CreateUser(username: Mock.userInfo.nextUsername, email: Mock.userInfo.nextEmail))
                    .Await();

            //GetUserData(string[]) to send all requests in parallel and return when all are done.
            for (int i = 0; i < 5; i++)
            {
                var data = DB.GetUserData(users.Select(u => u.uid)).MergeWith(
                    DB.GetUserData(users.Select(u => u.uid))).Await();
            }


            //Assert.AreEqual(data[0].username, Mock.userInfo.username);
            //Assert.AreEqual(data[1].username, Mock.userInfo.nextUsername);
        }

        [TestMethod]
        public void TestSignup()
        {
            var cred = CreateUser().Await();
            DB.SetCredential(cred);

            var user = DB.GetUserData(cred.uid).Await();
            Assert.IsFalse(DB.IsUsernameAvailable(user.username).ToSuccessBoolean().Await());

            //Assert.AreEqual(cred.email, DB.GetEmailFromUsername(user.username).Await());

            UseAdminCredentialSync();
            Assert.ThrowsException<Exception>(() => CreateUser().Await());
            Assert.ThrowsException<Exception>(() =>
                CreateUser(Mock.userInfo.username, Mock.userInfo.nextEmail).Await());
            Assert.ThrowsException<Exception>(() =>
                CreateUser(Mock.userInfo.nextUsername, Mock.userInfo.email).Await());

            Assert.IsTrue(DB.IsUsernameAvailable(Mock.userInfo.nextUsername).ToSuccessBoolean().Await());
        }

        // [TestMethod]
        // public void TestFirestoreRules()
        // {
        //     //Create account without credential
        //     DB.SetCredential(null);
        //     Assert.IsFalse(CreateUser().ToSuccessBoolean().Await());
        //     
        //     //create account with admin cred
        //     UseAdminCredentialSync();
        //     Assert.IsTrue(CreateUser().ToSuccessBoolean().Await());
        //     
        //     //sign in without cred
        //     DB.SetCredential(null);
        //     Assert.IsTrue(Auth.SignInWithEmail(Mock.userInfo.email, Mock.userInfo.password).ToSuccessBoolean().Await());
        //     Assert.IsTrue(Auth.SignInWithUsername(Mock.userInfo.username, Mock.userInfo.password).ToSuccessBoolean().Await());
        //     UseAdminCredentialSync();
        // }

        [TestMethod]
        public void TestAwaitAndMerge()
        {
            Assert.IsTrue(DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await());
            Assert.IsTrue(DB.IsUsernameAvailable(Mock.userInfo.nextUsername).ToSuccessBoolean().Await());
            Credential cred = CreateUser().Await();

            Assert.IsFalse(DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await());

            Assert.IsTrue(DB.IsUsernameAvailable(Mock.userInfo.nextUsername).ToSuccessBoolean().Await());


            // Assert.AreEqual(Mock.userInfo.email, 
            //     DB.GetEmailFromUsername(Mock.userInfo.username).Await());

            Assert.IsTrue(CreateUser(Mock.userInfo.nextUsername, Mock.userInfo.nextEmail).ToSuccessBoolean().Await());

            // var emails = Promise<string>.All(new []{
            //     DB.GetEmailFromUsername(Mock.userInfo.username),
            //     DB.GetEmailFromUsername(Mock.userInfo.nextUsername)}).Await().ToArray();

            // Assert.AreEqual(Mock.userInfo.email, emails[0]);
            // Assert.AreEqual(Mock.userInfo.nextEmail, emails[1]);

            // emails =
            //     DB.GetEmailFromUsername(Mock.userInfo.username).MergeWith(
            //     DB.GetEmailFromUsername(Mock.userInfo.nextUsername)).Await();
            //
            // Assert.AreEqual(Mock.userInfo.email, emails[0]);
            // Assert.AreEqual(Mock.userInfo.nextEmail, emails[1]);
            //
            // Assert.IsFalse(
            //     DB.GetEmailFromUsername(Mock.userInfo.username).MergeWith(
            //     DB.GetEmailFromUsername("jyhfgujhg")).ToSuccessBoolean().Await());
            //
            //
            // Assert.IsTrue(
            //     DB.GetEmailFromUsername(Mock.userInfo.username).ToSuccessBoolean().Await());
        }


        [TestMethod]
        public void TestAddMatchHistory()
        {
            var cred = CreateUser().Await();
            DB.UpdateUserData(Mock.FirestoreUser, cred).Await();
            var u = DB.AddUserMatchHistoryReference(cred.uid, "entry").Await();

            var uServer = DB.GetUserData(cred.uid).Await();
            CollectionAssert.AreEqual(u.matchHistory, uServer.matchHistory);
            Assert.AreEqual(Mock.FirestoreUser.matchHistory.Length + 1, u.matchHistory.Length);
            CollectionAssert.AreEqual(u.matchHistory, Mock.FirestoreUser.matchHistory.Prepend("entry").ToArray());
        }


        [TestMethod]
        public void TestFirestoreUserNoNullsWhenCreatedFromLocal()
        {
            Assert.IsFalse(JsonUtil.ToJson(Mock.FirestoreUser).Contains("null"));
            Assert.IsFalse(Mock.FirestoreUser.ToFirestoreJson().Contains("null"));
        }

        [TestMethod]
        public void TestChat()
        {
            var cred = CreateUser().ThenKeepVal(cred => DB.UpdateUserData(Mock.FirestoreUser, cred)).Await();
            var uServer = DB.GetUserData(cred.uid).Await();
            Assert.AreEqual(Mock.FirestoreUser.chats.Length, uServer.chats.Length);
            var u = DB.JoinChat(cred.uid, "abc123").Await();
            uServer = DB.GetUserData(cred.uid).Await();

            CollectionAssert.AreEqual(u.chats, uServer.chats);
            CollectionAssert.AreEqual(u.chats, Mock.FirestoreUser.chats.Prepend("abc123").ToArray());
        }

        [TestMethod]
        public void TestAddFriend()
        {
            DisableLogging();
            var sender = CreateUser().Await();
            var receiver = CreateUser(Mock.userInfo.nextUsername, Mock.userInfo.nextEmail).Await();
            EnableLogging("USERS CREATED");
            DB.SetCredential(sender);

            //TEST unexisting uid
            Assert.IsFalse(
                DB.SendFriendRequest(sender.uid, "uid that doesnt exist")
                    .ToSuccessBoolean().Await());

            //admin can send request
            Assert.IsTrue(DB.SendFriendRequest(sender.uid, receiver.uid).ToSuccessBoolean().Await());
            var u = DB.GetUserData(sender.uid).MergeWith(DB.GetUserData(receiver.uid)).Await();

            Assert.AreEqual(receiver.uid, u[0].outgoingFriendRequests[0]);
            Assert.AreEqual(sender.uid, u[1].incomingFriendRequests[0]);
            Assert.AreEqual(0, u[0].friends.Length);
            Assert.AreEqual(0, u[1].friends.Length);

            //DENY FRIEND REQUEST
            Assert.IsTrue(DB.RemoveFriend(sender.uid, receiver.uid).ToSuccessBoolean().Await());
            u = DB.GetUserData(sender.uid).MergeWith(DB.GetUserData(receiver.uid)).Await();

            Assert.AreEqual(0, u[0].outgoingFriendRequests.Length);
            Assert.AreEqual(0, u[1].incomingFriendRequests.Length);
            Assert.AreEqual(0, u[0].friends.Length);
            Assert.AreEqual(0, u[1].friends.Length);
            //RESTORE
            DB.SendFriendRequest(sender.uid, receiver.uid).Await();

            //ACCEPT
            //Assert.IsFalse(DB.AcceptFriendRequest(receiver.uid, sender.uid).ToSuccessBoolean().Await());  SENDING LIKE THIS AS ADMIN WILL WORK BUT NOT DO THE RIGHT THING
            Assert.IsFalse(DB.AcceptFriendRequest(sender.uid, receiver.uid).ToSuccessBoolean().Await());
            Assert.IsFalse(DB.AcceptFriendRequest(receiver.uid, sender.uid).ToSuccessBoolean().Await());
            
            DB.SetCredential(receiver);
            Assert.IsTrue(DB.AcceptFriendRequest(sender.uid, receiver.uid).ToSuccessBoolean().Await());

            DB.SetCredential(sender);
            //check friends have been added
            u = DB.GetUserData(sender.uid).MergeWith(DB.GetUserData(receiver.uid)).Await();
            Assert.AreEqual(0, u[0].outgoingFriendRequests.Length);
            Assert.AreEqual(0, u[1].incomingFriendRequests.Length);
            Assert.AreEqual(receiver.uid, u[0].friends[0]);
            Assert.AreEqual(sender.uid, u[1].friends[0]);

            //REMOVE FRIEND
            Assert.IsTrue(DB.RemoveFriend(sender.uid, receiver.uid).ToSuccessBoolean().Await());
            u = DB.GetUserData(sender.uid).MergeWith(DB.GetUserData(receiver.uid)).Await();
            Assert.AreEqual(0, u[0].outgoingFriendRequests.Length);
            Assert.AreEqual(0, u[1].incomingFriendRequests.Length);
            Assert.AreEqual(0, u[0].friends.Length);
            Assert.AreEqual(0, u[1].friends.Length);
        }


        [TestMethod]
        public void TestDatabaseCreate()
        {
            var doc = DB.CreateDocumentWithAutomaticId(Mock.FirestoreUser, "testCollection")
                .ThenKeepVal(ud =>
                {
                    Debugger.Print("success\n" + ud.GetPath());
                    DB.DeleteDocument("testCollection");
                    Assert.AreEqual(5, 5);
                })
                .Catch(e =>
                {
                    Debugger.Print("fail");
                    Assert.Fail(e.Message);
                    throw e;
                })
                .Await();

            Assert.AreEqual(5, 5);

            DB.DeleteDocument("testCollection/" + doc.GetPath()).Await();
        }

        [TestMethod]
        public void TestSearch()
        {
            var username2 = Mock.userInfo.nextUsername;
            //DisableLogging();
            var c = CreateUser(Mock.userInfo.username, Mock.userInfo.email).Await();
            Assert.IsNotNull(c);
            EnableLogging();
            var c2 = CreateUser(username2, Mock.userInfo.nextEmail).Await();
            Assert.IsNotNull(c2);


            Assert.AreNotEqual(c, default(Credential));

            Debugger.Print("\n\n_____Created USER____\n\n");

            var users = Search().Await();
            foreach (var u in users)
            {
                Debugger.Print(u.username + "\n");
            }

            Assert.AreEqual(Mock.userInfo.username, users[0].username);
            Assert.AreEqual(username2, users[1].username);

            DB.DeleteDocument("users/" + c2.uid).Await();
            DB.DeleteDocument("usernames/" + username2).Await();
        }


        private IPromise<BridgeFirestoreUser[]> Search()
        {
            return DB.FindUsersByUsername(
                Mock.userInfo.username.Substring(0, (int) (Mock.userInfo.username.Length * 0.7)));
        }

        [TestMethod]
        public void TestAuth()
        {
            //USERNAME DOES NOT EXIST
            var exists = DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await();
            Assert.AreEqual(true, exists);

            //REMOVE AND CREATE USER
            var cred = CreateUser().Await();
            Assert.AreEqual(cred.email, Mock.userInfo.username.ToLower() + ".account@bridgestars.net");


            //DATA HAS BEEN UPLOADED AND CAN BE DOWNLOADED AND PARSED
            var ud = DB.GetUserData(cred.uid).Await();
            Assert.AreEqual(ud.username, Mock.userInfo.username);

            //USERNAME DOES NOW EXIST
            exists = DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await();
            Assert.AreEqual(false, exists);

            //Assert.AreEqual(5, 5);
        }


        [TestMethod]
        public void TestProtectedFields()
        {
            //Sign up
            var cred = CreateUser().Await();
            
            //check
            var f = Auth.GetProtectedFields(cred).Await();
            Debugger.Print(f);
            Assert.AreEqual("{\"nationality\":\"sweden\",\"timeOfBirth\":\"\",\"firstName\":\"\",\"lastName\":\"\",\"publicFields\":[]}", f.ToString());
            
            //update
            f.FirstName = "adminFirstName";
            f.LastName = "adminLastName";
            f.Nationality = "norway";
            f.TimeOfBirth = "today";
            Auth.UpdateProtectedFields(f, cred).Await();
            
            //check
            f = Auth.GetProtectedFields(cred).Await();
            Debugger.Print(f);
            Assert.AreEqual("{\"nationality\":\"norway\",\"timeOfBirth\":\"today\",\"firstName\":\"adminFirstName\",\"lastName\":\"adminLastName\",\"publicFields\":[]}", f.ToString());

            var cred2 = CreateUser(Mock.userInfo.nextUsername, Mock.userInfo.nextEmail).Await();
            
            var f2 = Auth.GetProtectedFields(cred.uid, cred2).Await();
            Assert.AreEqual("{\"nationality\":\"private\",\"timeOfBirth\":\"private\",\"firstName\":\"private\",\"lastName\":\"private\",\"publicFields\":[]}", f2.ToString());
            
            f.PublicFields = new[]{"nationality", "TiMEOFbiRTH"};
            Auth.UpdateProtectedFields(f, cred).Await();
            Auth.UpdateProtectedFields(new ProtectedUserData()
            {
                FirstName = "jonas"
            }, cred).Await();

            f2 = Auth.GetProtectedFields(cred.uid, cred2).Await();
            Debugger.Print(f2);





        }


        [TestMethod]
        public void TestMigrate()
        {
            // //Functions.MigrateAccount("HERR.jonas.mann@gmail.com").Await();
            // var c = UseAdminCredentialSync();
            // var p = Auth.GetProtectedFields("hzzUiApNWYak3X4SVe4CzCwd6QL2", c).Await();
            // Debugger.Print(p);
            //
            // var c2 = CreateUser().Await();
            // var p2 = Auth.GetProtectedFields("hzzUiApNWYak3X4SVe4CzCwd6QL2", c2).Await();
            // Debugger.Print(p2);
            Auth.SendPasswordResetEmail("castonnen@gmail.com").Await();
        }
        
        [TestMethod]
        public void TestChangeEmail()
        {
            var cred = CreateUser().Await();

            Assert.IsFalse(DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await());
            
            Auth.UpdateEmail(Mock.userInfo.nextEmail, cred).Await();

            Assert.ThrowsException<Exception>(() => Auth.SignInWithEmail(Mock.userInfo.email, Mock.userInfo.password).Await());
            Auth.SignInWithEmail(Mock.userInfo.nextEmail, Mock.userInfo.password).Await();

            Assert.ThrowsException<Exception>(()=>DB.GetRaw("emailIndex/" + Mock.userInfo.email).Await());
            DB.GetRaw("emailIndex/" + Mock.userInfo.nextEmail).Await();
            
        }

        [TestMethod]
        public void TestChangeUsername()
        {
            var cred = CreateUser().Await();
            //CHANGE USERNAME
            var u = DB.GetUserData(cred.uid).Await();
            u.username = Mock.userInfo.nextUsername;

            //OLD USERNAME DOES EXIST
            var free = DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await();
            Assert.IsFalse(free);

            //NEW USERNAME DOES NOT EXIST
            free = DB.IsUsernameAvailable(Mock.userInfo.nextUsername).ToSuccessBoolean().Await();
            Assert.IsTrue(free);

            DB.SetCredential(cred);
            //USER DOCUMENT HAS BEEN UPDATED
            var OK = DB.UpdateUserData(u, cred).ToSuccessBoolean().Await();
            Assert.IsTrue(OK);
            var newUD = DB.GetUserData(cred.uid).Await();
            Assert.AreEqual(Mock.userInfo.nextUsername, newUD.username);

            //NEW USERNAME DOES EXIST
            free = DB.IsUsernameAvailable(Mock.userInfo.nextUsername).ToSuccessBoolean().Await();
            Assert.IsFalse(free);

            //OLD USERNAME DOES NOT EXIST
            free = DB.IsUsernameAvailable(Mock.userInfo.username).ToSuccessBoolean().Await();
            Assert.IsTrue(free);
        }

        [TestMethod]
        public void TestFirestoreDataParser()
        {
            //var u = Mock.FirestoreUser;
            //u.username = Mock.userInfo.username;
            //u.usernameLower = u.username.ToLower();

            //u.BuildUpdateRequestJson();

            //var u2 = new BridgeFirestoreUser();
            //u2.bridgeUserFields = u.bridgeUserFields;
            //u2.LoadFields();

            //Assert.AreEqual(u.username, u2.username);
            //Assert.AreEqual(u.firstName, u2.firstName);
            //Assert.AreEqual(u.timeOfBirth, u2.timeOfBirth);
            //Assert.AreEqual(u.friends[0], u2.friends[0]);
            //Assert.AreEqual(u.friends.Length, u2.friends.Length);

            //Debugger.Print(JsonUtil.ToJson(u.elo));
            //Debugger.Print(JsonUtil.ToJson(u2.elo));
            ////BRIDGEUSER SPECIFIC
            //Assert.AreEqual(u.elo, u2.elo);
            //Assert.AreEqual(u.balance, u2.balance);
            //Assert.AreEqual(u.xp, u2.xp);
        }

        [TestMethod]
        public void TestBridgeUserData()
        {
            var cred = CreateUser().ThenKeepVal(c => DB.UpdateUserData(Mock.FirestoreUser, c)).Await();

            var u = DB.GetUserData(cred.uid).Await();
            Debugger.Print(JsonUtil.ToJson(u));
            Assert.AreEqual(Mock.userInfo.username, u.username);
            Assert.AreEqual(Mock.FirestoreUser.balance, u.balance);
            Assert.AreEqual(Mock.FirestoreUser.chats[0], u.chats[0]);
            Assert.AreEqual(Mock.FirestoreUser.friends[0], u.friends[0]);
            Assert.AreEqual(Mock.FirestoreUser.friends.Length, u.friends.Length);

            Debugger.Print(JsonUtil.ToJson(u.elo));
            Debugger.Print(JsonUtil.ToJson(Mock.FirestoreUser.elo));
            //BRIDGEUSER SPECIFIC
            Assert.AreEqual(Mock.FirestoreUser.elo, u.elo);
            Assert.AreEqual(Mock.FirestoreUser.balance, u.balance);
            Assert.AreEqual(Mock.FirestoreUser.xp, u.xp);
        }

        [TestMethod]
        public void TestCreateUser()
        {
            Assert.ThrowsException<Exception>(() =>
                Auth.SignInWithUsername(Mock.userInfo.username, Mock.userInfo.password).Await());
            Assert.ThrowsException<Exception>(() =>
                Auth.SignInWithEmail(Mock.userInfo.email, Mock.userInfo.password).Await());
            var uid = Auth.SignUpWithUsername(
                Mock.userInfo.username,
                Mock.userInfo.email,
                Mock.userInfo.password,
                Mock.AuthUser).Await();

            var cred = Auth.SignInWithEmail(Mock.userInfo.email, Mock.userInfo.password).Await();

            var u = Mock.FirestoreUser;
            u.balance += 100;
            u.username = Mock.userInfo.username;

            Debugger.Print(u.HasPath());
            Debugger.Print(u.GetPath());
            // u.DocPath = FirebaseCredentials.dbPath + "users/" + cred.uid;
            // Debugger.Print(u.HasPath());
            // Debugger.Print(u.DocPath);
            //     
            // Debugger.Print(u.GetUpdateJson());
            DB.SetCredential(cred);
            DB.UpdateUserData(u, cred).Await();
        }


        [TestMethod]
        public void TestUsernameAvailable()
        {
            //var cred = Auth.SignUpWithUsername(Mock.FirestoreUser, Mock.userInfo.username, Mock.userInfo.email,
            //Mock.userInfo.password).Await();
            DB.IsUsernameAvailable("theoh").Await();
            DB.IsUsernameAvailable("theooooooo").Await();
            //Assert.IsTrue(DB.UsernameExists(Mock.userInfo.username).ToSuccessBoolean().Await());
        }

        //[TestMethod]
        //public void TestChangeEmail()
        //{
        //    var cred = await(CreateUser());
        //    //CHANGE EMAIL


        //    var u = DB.GetUserData(cred.uid).Await();

        //    u.email = Mock.userInfo.nextEmail;
        //    var updateSuccess = DB.UpdateUserData(u, cred.uid).ToSuccessBoolean().Await();
        //    Assert.AreEqual(true, updateSuccess);

        //    var newUD = await(DB.GetUserData(cred.uid));
        //    Assert.AreEqual(Mock.userInfo.nextEmail, newUD.email); //user doc email updated

        //    var newEMAIL = await(DB.GetEmailFromUsername(newUD.username)); //username doc email updated
        //    Assert.AreEqual(newEMAIL, newUD.email);


        //    u.email = Mock.userInfo.email;
        //    u.username = Mock.userInfo.username;
        //    updateSuccess = DB.UpdateUserData(u, cred.uid).ToSuccessBoolean().Await();
        //    Assert.AreEqual(true, updateSuccess);

        //    newUD = await(DB.GetUserData(cred.uid));
        //    Assert.AreEqual(Mock.userInfo.email, newUD.email); //user doc email updated

        //    newEMAIL = await(DB.GetEmailFromUsername(newUD.username)); //username doc email updated
        //    Assert.AreEqual(newEMAIL, newUD.email);
        //}

        // [TestMethod]
        // public void TestChangeEmailWhenNotYetSupported()
        // {
        //     var cred = CreateUser().Await();
        //     //CHANGE EMAIL
        //
        //
        //
        //     var u = DB.GetUserData(cred.uid).Await();
        //
        //     u.email = Mock.userInfo.nextEmail;
        //     var updateSuccess = DB.PostUserData(u, cred).ToSuccessBoolean().Await();
        //     Assert.AreEqual(false, updateSuccess);
        //
        //     var newUD = DB.GetUserData(cred.uid).Await();
        //     Assert.AreEqual(Mock.userInfo.email, newUD.email); //user doc email updated
        //
        //     var newEMAIL = DB.GetEmailFromUsername(newUD.username).Await(); //username doc email updated
        //     Assert.AreEqual(newEMAIL, newUD.email);
        // }

        
        void RemoveUser()
        {
            Auth.SignInWithUsername(Mock.userInfo.username, Mock.userInfo.password)
                .Then(cred =>
                {
                    Auth.DeleteAccountAndData(cred).Await();
                    //AUTH REMOVE USER
                }).AwaitCatch();
            Auth.SignInWithUsername(Mock.userInfo.nextUsername, Mock.userInfo.password)
                .Then(cred =>
                {
                    Auth.DeleteAccountAndData(cred).Await();
                    //AUTH REMOVE USER
                }).AwaitCatch();

            UseAdminCredentialSync();
            DB.RunQuery<BridgeFirestoreUser>(
                    new Query().SelectCollection("users").AddFieldFilter(
                        "usernameLower",
                        Query.OPERATOR.EQUAL,
                        new OldData.String(Mock.userInfo.username.ToLower())
                    )
                )
                .Then(users =>
                {
                    Console.WriteLine("REMOVING " + users.Length + " USER DOCUMENTS");
                    Promise.All(users.Select(u => DB.DeleteDocument("users/" + u.DocId)));
                    if(users.Length != 0) throw new Exception("HAD TO MANUALLY REMOVE " + users.Length + " DOCUMENTS");
                }).Await();
            DB.RunQuery<BridgeFirestoreUser>(new Query().SelectCollection("users").AddFieldFilter("usernameLower",
                    Query.OPERATOR.EQUAL, new OldData.String(Mock.userInfo.nextUsername.ToLower())))
                .Then(users =>
                {
                    Console.WriteLine("REMOVING " + users.Length + " USER DOCUMENTS");
                    Promise.All(users.Select(u => DB.DeleteDocument("users/" + u.DocId)));
                    if(users.Length != 0) throw new Exception("HAD TO MANUALLY REMOVE " + users.Length + " DOCUMENTS");
                }).Await();
        }

        

        IPromise<Credential> CreateUser(string username = Mock.userInfo.username, string email = Mock.userInfo.email)
        {
            Auth.SetDatabaseReference(ref DB);
            return Auth.SignUpWithUsername(username, email, Mock.userInfo.password, Mock.AuthUser)
                .Then(uid => { Debugger.Print("NEW USER CREATED\nuid: " + uid); })
                .CatchWrap("COULD NOT CREATE NEW USER: ")
                .Then(() => Auth.SignInWithUsername(username, Mock.userInfo.password));
        }
    }
}
#pragma warning restore CS0618
