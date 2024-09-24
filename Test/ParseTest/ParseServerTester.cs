using System.Collections.ObjectModel;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project.ParseORM;
using Project.ParseORM.Data;
using Project.Util;
using Parse;
using Parse.Infrastructure;
using Array = System.Array;
using DB = Project.ParseORM.Database;
using Debugger = Project.Util.Debugger;
using Project.ParseORM.Data.Room;
using Project.ParseORM.Data.Commerce;
using Parse.Infrastructure.Execution;
using static Test.Util;
using Admin = Test.Util.Admin;
using Tester = Test.Util.Tester;

namespace Test
{
    [TestClass]
    public class ParseServerTester
    {


        [TestInitialize]
        public void Init()
        {
            Debugger.Print("REMEMBER TO START DEV SERVER!");
            Util.TestInit(dev: true);
        }

        [TestCleanup]
        public void Clean()
        {
            Util.CleanUp();
        }



        [TestMethod]
        public void TestTransaction()
        {
            DeleteAllTestObjs();
            var t1 = Tester.SignUp(1);
            t1.Increment("balance", 2);
            t1.Elo = 10;
            Debugger.PrintJson(t1.GetDirtyKeys());
            Debugger.PrintJson(t1.GetDirtyRemoteKeys());
            t1.SaveAsync().Await();
            // t1 is not actually saved, instead a copy is saved.
            Debugger.PrintJson(t1.Underlying.CurrentOperations.Values.Select(x => x.Encode(DB.Client)));

            DB.EnableLocalCache(false);
            var t2 = DB.GetObjectAsync<DbUser>(t1.Uid).Await();
            DB.EnableLocalCache(true);
            Debugger.PrintJson(t2);
            Debugger.PrintJson(t2.Underlying.CurrentOperations.Values.Select(x => x.Encode(DB.Client)));



        }


        [TestMethod]
        public void TestChat()
        {
            DB.EnableLocalCache(true);
            DeleteAllTestObjs();
            // return;
            var t1 = Tester.SignUp(1);

            var c = DbObject.Instantiate<DbChat>();
            c.Name = "BS_CHAT_TEST";
            c.AddUsers(t1.Uid);
            c.SaveAsync().Await();


            var t2 = Tester.SignUp(2);
            Assert.AreEqual(0, c.Users.Count); //should wipe at sign up
            c.AddUsers(t2);
            Assert.ThrowsException<ParseFailureException>(() => c.SaveAsync().Await()); //Should say that chat does not exist since t2 does not have access to it.


            Tester.SignIn(1);
            Assert.AreEqual(0, c.Users.Count); //should wipe at sign in
            c.AddUsers(t2);
            Assert.AreEqual(1, c.Users.Count);
            // Debugger.PrintJson(c.Underlying.CurrentOperations.Values.First()..Encode(DB.Client));
            c.SaveAsync().Await();
            var temp = DB.GetObjectAsync<DbChat>(c.Uid).Await();
            Assert.AreEqual(2, temp.Users.Count);
            Assert.AreEqual(2, c.Users.Count);
            c.DeleteAsync().Await();

            c = DbObject.Instantiate<DbChat>();
            c.Name = "BS_CHAT_TEST";
            c.AddUsers(t1.Uid).SaveAsync().Await();
            t2 = Tester.SignIn(2);
            // Tester.SignIn(1);
            Assert.ThrowsException<ParseFailureException>(() => c.AddUsers(t2).SaveAsync().Await());
            Tester.SignIn(1);
            c.AddUsers(t2).SaveAsync().Await(); //cache cleared so have to add t2 again
            var t3 = Tester.SignUp(3);

            Tester.SignIn(2);
            t2.GetAllChatsAsync().Await();
            c.AddUsers(t3).SaveAsync().Await();


            Tester.SignIn(3);
            var chats = t3.GetAllChatsAsync().Await();
            Assert.AreEqual(1, chats.Length);
            Assert.AreEqual(c.Uid, chats[0].Uid);
            Assert.AreEqual(3, chats[0].Users.Count);

            var chat = chats[0];
            var m = new DbMessage(t3, chat, "0");
            m.SaveAsync().Await();

            t2 = Tester.SignIn(2);
            Debugger.Print("START HERE");
            chat = t2.GetAllChatsAsync().Await()[0];
            Debugger.PrintJson(t2.GetAllChatsAsync().Await().Select(x => x.Uid));
            var messages = chat.FetchNewMessagesAsync().Await();

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual(m.Uid, messages[0].Uid);
            Assert.AreEqual(t3.Uid, messages[0].Sender.Uid);
            Assert.AreEqual("0", messages[0].Text);

            DbMessage mess = null;
            Debugger.Print("Send messages nr1-2...");

            for (int i = 1; i < 3; i++)
            {
                mess = new DbMessage(t2, chat, i.ToString()).Send().Await();
            }
            Debugger.Print("Save message 2 to local chat...");
            chat.AddDownloadedMessage(mess);
            Debugger.Print("Send messages nr3-5...");
            for (int i = 3; i < 6; i++)
            {
                mess = new DbMessage(t2, chat, i.ToString()).Send().Await();
                Assert.AreEqual(true, DB.Cache.Contains<DbMessage>(mess.Uid));
            }

            Debugger.Print("Get downloaded...");
            var m0 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m0);
            Assert.AreEqual(2, m0.Count()); //cache should be cleared

            var m1234 = DB.Cache.Get<DbChat>(chat.Uid);
            Debugger.PrintJson(m1234);
            Debugger.PrintJson(chat);
            Assert.AreSame(m1234, chat);

            t1 = Tester.SignIn(1);
            Assert.AreEqual(true, DB.Cache.Contains<DbChat>(chat.Uid)); //cache does not contain


            var m123 = DB.Cache.Get<DbChat>(chat.Uid).GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m123);
            Debugger.Print("Get downloaded...");
            var m1 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m1);
            Assert.AreEqual(0, m1.Count()); //cache should be cleared

            Debugger.Print("Fetch new...");
            m1 = chat.FetchNewMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5" });


            Debugger.Print("Get downloaded...");
            m1 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5" });


            Debugger.Print("Fetch old...");
            m1 = chat.FetchOlderMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5" });

            Debugger.Print("Fetch new...");
            m1 = chat.FetchNewMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5" });


            Debugger.Print("Send messages nr6-19...");
            for (int i = 6; i < 20; i++)
            {
                mess = new DbMessage(t1, chat, i.ToString()).Send().Await();
            }

            t2 = Tester.SignIn(2);

            Debugger.Print("Get downloaded...");
            m1 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m1);
            Assert.AreEqual(0, m1.Count());


            Debugger.Print("Fetch new...");
            m1 = chat.FetchNewMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" });

            //since we downloaded 10 new messages (the limit) all old messages should have been deleted so that the messages in between have a chance to download 
            Debugger.Print("Get downloaded...");
            m1 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" });


            Debugger.Print("Fetch old...");
            m1 = chat.FetchOlderMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" });

            Debugger.Print("Get downloaded...");
            m1 = chat.GetAllDownloadedMessages().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(),
                new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" });

            Debugger.Print("Fetch old...");
            m1 = chat.FetchOlderMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" });

            Debugger.Print("Fetch new...");
            m1 = chat.FetchNewMessagesAsync().Await().Select(m => m.Text);
            Debugger.PrintJson(m1);
            CollectionAssert.AreEqual(m1.ToArray(), chat.GetAllDownloadedMessages().Select(m => m.Text).ToArray());

            var t4 = Tester.SignUp(4);
            Assert.ThrowsException<ParseFailureException>(() => chat.AddUsers(t4).SaveAsync().Await());
            Assert.ThrowsException<ParseFailureException>(() => new DbMessage(t4, chat, "I am not a member of this chat.").Send().Await());

        }


        [TestMethod]
        public void TestMatchAndTable()
        {

            DeleteAllTestObjs();
            // return;
            var u = Tester.SignUp(1);
            var player_name = u.Username;
            Assert.AreEqual(u.Username, "bs_tester1");


            var t1 = new DbTable("BS_TABLE_TEST_0");

            var t2 = new DbTable("BS_TABLE_TEST_1");

            var m = new DbMatch(DateTime.UnixEpoch, DateTime.Now);
            m.Name = "0";


            t2.South = u;
            //user not admin
            Assert.ThrowsException<ParseFailureException>(() => t2.SaveAsync().Await());

            Admin.SignIn();
            //tables not added
            Assert.ThrowsException<ParseFailureException>(() => m.SaveAsync().Await());

            //table must have at least one player NOT ANYMORE
            // Assert.ThrowsException<ParseFailureException>(() => t1.SaveAsync().Await());
            t1.SaveAsync().Await();

            t1.North = u;
            t1.SaveAsync().Await();


            //all tables not saved yet
            Assert.ThrowsException<ParseFailureException>(() => m.SaveAsync().Await());

            t2.SaveAsync().Await();

            m.AddTablesAsync(t1, t2).Await();
            // now should be able to save
            m.SaveAsync().Await();

            // DB.Cache.Clear();
            // var match = DB.GetObjectAsync<DbMatch>(m.Uid).Await();
            // Debugger.PrintJson(match);
            var match = m;

            // Assert.AreEqual(match.Uid, m.Uid);
            // Assert.AreEqual(match.StartTime!.Value.ToString(CultureInfo.InvariantCulture), m.StartTime!.Value.ToString(CultureInfo.InvariantCulture));
            // Assert.AreEqual(match.EndTime!.Value.ToString(CultureInfo.InvariantCulture), m.EndTime!.Value.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(match.TableRefs.Count, 2);
            Assert.AreEqual(match.TableRefs[0].Uid, t1.Uid);
            Assert.AreEqual(match.TableRefs[1].Uid, t2.Uid);
            Assert.AreEqual(match.PlayerRefs.Count, 0);
            // match.FetchAsync().Await(); OR
            match = DB.GetObjectAsync<DbMatch>(match.Uid).Await();
            Assert.AreEqual(match.PlayerRefs.Count, 0);
            match.NoteStale();
            match = DB.GetObjectAsync<DbMatch>(match.Uid).Await();
            Assert.AreEqual(match.PlayerRefs.Count, 1);
            Assert.AreEqual(match.PlayerRefs[0].Uid, u.Uid);


            Action<string, List<DbMatch>, string[], string[], string[]> test = (print, mh, expectedMatches, expectedTables, expectedPlayers) =>
            {
                Debugger.Print(print);
                var tables = mh.Select(m => m.GetTablesAsync().Await().Select(t => t.Data[0]).ToArray()).ToArray();
                var players = mh.Select(m => m.GetPlayersAsync().Await().Select(t => t.Username).ToArray()).ToArray();
                Debugger.PrintJson(mh.Select(m => m.Name));
                Debugger.PrintJson(tables);
                Debugger.PrintJson(players);
                CollectionAssert.AreEquivalent(expectedMatches, mh.Select(m => m.Name).ToArray());
                CollectionAssert.AreEquivalent(expectedTables, tables.SelectMany(t => t.Select(x => x)).ToArray());
                CollectionAssert.AreEquivalent(expectedPlayers, players.SelectMany(t => t.Select(x => x)).ToArray());
            };

            Action<string, List<DbMatch>> testEmpty = (print, mh) =>
            {
                test(print, mh, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
            };

            testEmpty("Get downloaded tables...",
                u.Matches.GetAllFetchedItems());

            test("Get new tables...",
                u.Matches.FetchGreaterItemsAsync().Await(), // possible bug? should this be FetchLesser? (I changed this form oboslete MatchHistory.FetchNew(), /Castor)
                new[] { "0" },
                new[] { "BS_TABLE_TEST_1", "BS_TABLE_TEST_0" },
                new[] { player_name });

            Debugger.Print("Creating match 1-4 with table 2-9");
            for (int i = 1; i < 5; i++)
            {
                var ti = new DbTable(new[] { "BS_TABLE_TEST_" + i * 2 });
                ti.South = u;
                var ti2 = new DbTable(new[] { "BS_TABLE_TEST_" + (i * 2 + 1) });
                ti2.North = u;

                var mi = new DbMatch(DateTime.UnixEpoch, DateTime.Now)
                { Name = i.ToString() };
                mi.AddTablesAsync(ti, ti2).Await();
                mi.SaveAsync().Await();
            }


            test("Get downloaded tables...",
                u.Matches.GetAllFetchedItems(),
                new[] { "0" },
                new[] { "BS_TABLE_TEST_0", "BS_TABLE_TEST_1" },
                new[] { player_name });

            test("Get new tables...",
                u.Matches.FetchGreaterItemsAsync().Await(),
                new[] { "0", "1", "2", "3", "4" },
                new[] { "BS_TABLE_TEST_0", "BS_TABLE_TEST_1", "BS_TABLE_TEST_2", "BS_TABLE_TEST_3", "BS_TABLE_TEST_4", "BS_TABLE_TEST_5", "BS_TABLE_TEST_6", "BS_TABLE_TEST_7", "BS_TABLE_TEST_8", "BS_TABLE_TEST_9" },
                new[] { player_name, player_name, player_name, player_name, player_name });

            test("Get downloaded tables...",
                u.Matches.GetAllFetchedItems(),
                new[] { "0", "1", "2", "3", "4" },
                new[] { "BS_TABLE_TEST_0", "BS_TABLE_TEST_1", "BS_TABLE_TEST_2", "BS_TABLE_TEST_3", "BS_TABLE_TEST_4", "BS_TABLE_TEST_5", "BS_TABLE_TEST_6", "BS_TABLE_TEST_7", "BS_TABLE_TEST_8", "BS_TABLE_TEST_9" },
                new[] { player_name, player_name, player_name, player_name, player_name });

            match = u.Matches.GetAllFetchedItems()[0];
            var tables = match.GetTablesAsync().Await();
            var table = tables[0];
            Debugger.PrintJson(table);
            table.SetAllDeals("BS_TABLE_TEST_100", "BS_TABLE_TEST_101");
            Debugger.PrintJson(table);
            Tester.SignIn(1);
            Assert.ThrowsException<ParseFailureException>(() => table.SaveAsync().Await()); //only admin should be allowed to do this, admin is signed in now
            Admin.SignIn();
            match.FetchAsync().Await(); //cache wiped
            table.SetAllDeals("BS_TABLE_TEST_100", "BS_TABLE_TEST_101"); //cache wiped
            table.SaveAsync().Await();

            CollectionAssert.AreEqual(new[] { "BS_TABLE_TEST_100", "BS_TABLE_TEST_101" }, match.GetTablesAsync().Await()[0].Data);
            table.AddDeal("BS_TABLE_TEST_102").SaveAsync().Await();
            CollectionAssert.AreEqual(new[] { "BS_TABLE_TEST_100", "BS_TABLE_TEST_101", "BS_TABLE_TEST_102" }, match.GetTablesAsync().Await()[0].Data);

        }


        [TestMethod]
        public void TestFriends()
        {
            DB.EnableLocalCache(true);
            DeleteAllTestObjs();
            Tester.DeleteInstance(1);
            Tester.DeleteInstance(2);

            var t1 = Tester.SignUp(1);
            DB.SignOutAsync().Await();
            var t2 = Tester.SignUp(2);

            t2.SendFriendRequestAsync(t1).Await();

            Assert.AreEqual(1, t2.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t2.Friends.Count);

            DB.SignOutAsync().Await();
            t1 = Tester.SignIn(1);
            // t1.FetchAsync().Await();

            Assert.AreEqual(0, t1.OutgoingFriendRequests.Count);
            Assert.AreEqual(1, t1.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t1.Friends.Count);

            // Debugger.Print((t1.Underlying as ParseUser).IsAuthenticated);
            t1.IncomingFriendRequests[0].AcceptAsync().Await();
            // t1.FetchAsync().Await();
            // Debugger.Print((t1.Underlying as ParseUser).IsAuthenticated);

            Assert.AreEqual(0, t1.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t1.IncomingFriendRequests.Count);
            Assert.AreEqual(1, t1.Friends.Count);
            Assert.AreEqual(t2.Uid, t1.Friends[0].Uid);

            var temp1 = DB.GetUserAsync(t1.Uid).Await();
            Assert.AreEqual(0, temp1.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, temp1.IncomingFriendRequests.Count);
            Assert.AreEqual(1, temp1.Friends.Count);
            Assert.AreEqual(t2.Uid, temp1.Friends[0].Uid);



            Assert.AreEqual(0, t1.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t1.IncomingFriendRequests.Count);
            Assert.AreEqual(1, t1.Friends.Count);

            t2 = Tester.SignIn(2);
            Assert.AreEqual(t1.Uid, t2.Friends[0].Uid);
            Assert.AreEqual(0, t1.Friends.Count); //cache should be wiped.

            t2.RemoveFriendAsync(t1).Await();
            Assert.AreEqual(0, t2.Friends.Count);

            var t1_2 = Tester.SignIn(1);
            Assert.AreEqual(0, t1.Friends.Count);
            Assert.AreEqual(0, t1_2.Friends.Count);
            Assert.AreEqual(0, t1_2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t1_2.OutgoingFriendRequests.Count);

            Assert.AreEqual(0, t2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t2.OutgoingFriendRequests.Count);
            t1 = t1_2;

            t1.SendFriendRequestAsync(t2).Await();
            Assert.AreEqual(0, t1.IncomingFriendRequests.Count);
            Assert.AreEqual(1, t1.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t1.Friends.Count);
            Assert.AreEqual(0, t2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t2.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t2.Friends.Count);

            t2 = Tester.SignIn(2);
            Assert.AreEqual(1, t2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t2.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t2.Friends.Count);
            t2.IncomingFriendRequests[0].DenyAsync().Await();
            Assert.AreEqual(0, t2.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t2.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t2.Friends.Count);

            t1 = Tester.SignIn(1);
            Assert.AreEqual(0, t1.IncomingFriendRequests.Count);
            Assert.AreEqual(0, t1.OutgoingFriendRequests.Count);
            Assert.AreEqual(0, t1.Friends.Count);


            t1.RemoveFriendAsync("asdasd").Await(); //should not throw
        }

        [TestMethod]
        public void TestPrivateUserProfile()
        {
            DeleteAllTestObjs();
            Action<DbUser.ProfileAccessType, bool, bool> test =
                (accessType, areFriends, expectedAccess) =>
                {
                    Debugger.Print("Testing . . . [Access: " + accessType + ", Friends:" + areFriends + ", ExpectedAccess: " + expectedAccess + "]");
                    DbUser fetchedUser = null;
                    string first = "Theodor";
                    string last = "Lundqvist";
                    string nationality = "Sweden";
                    DateTime birth = DateTime.Today.AddDays(-10000);
                    // var t2 = 
                    DB.ClearLocalCache();
                    var t2 = Tester.SignUp(2);
                    var t1 = Tester.SignUp(1);
                    Tester.SignIn(2);

                    t2.FirstName = first;
                    t2.LastName = last;
                    t2.Nationality = nationality;
                    t2.DateOfBirth = birth;
                    t2.ProfileAccess = accessType;
                    if (areFriends) t2.SendFriendRequestAsync(t1).Await();//t2.ArrayAppend("friends", t1.Uid);
                    Debugger.PrintJson(t2.GetDirtyKeys());
                    Debugger.PrintJson(t2.GetDirtyRemoteKeys());
                    //create list of keys that are dirty in underlying
                    var keys = t2.GetRemoteKeys();
                    Debugger.PrintJson(keys.Where(k => t2.Underlying.IsKeyDirty(k)));
                    t2.SaveAsync().Await();

                    t2.SignOutAsync(); // to clear cache here

                    t1 = Tester.SignIn(1);
                    if (areFriends) t1.IncomingFriendRequests[0].AcceptAsync().Await();
                    // var uid = t2.Uid;
                    // t2 = null;
                    fetchedUser = DB.GetUserAsync(t2.Uid).Await();
                    Assert.IsNotNull(fetchedUser);

                    //values should be defaults since this data is not fetched yet
                    Assert.AreEqual(DbUser.ProfileAccessType.NoOne, fetchedUser.ProfileAccess);
                    Assert.AreEqual("", fetchedUser.FirstName);
                    Assert.AreEqual("", fetchedUser.LastName);
                    Assert.AreEqual("", fetchedUser.Nationality);
                    Assert.AreEqual(null, fetchedUser.DateOfBirth);
                    // Debugger.PrintJson(fetchedUser);
                    fetchedUser.GetUserInfoAsync().Await();
                    if ((expectedAccess ? accessType : DbUser.ProfileAccessType.NoOne) != fetchedUser.ProfileAccess)
                        Debugger.PrintJson(fetchedUser);

                    //if access should be granted then the values should be the same as the one saved earlier
                    Assert.AreEqual(expectedAccess ? accessType : DbUser.ProfileAccessType.NoOne, fetchedUser.ProfileAccess);
                    Assert.AreEqual(expectedAccess ? first : "", fetchedUser.FirstName);
                    Assert.AreEqual(expectedAccess ? last : "", fetchedUser.LastName);
                    Assert.AreEqual(expectedAccess ? nationality : "", fetchedUser.Nationality);
                    Assert.AreEqual(expectedAccess ? birth : null, fetchedUser.DateOfBirth);
                    Debugger.Print("Test passed");
                    DeleteAllTestObjs();
                };

            //Access, areFriends, expectedSuccess

            //no one can view, are/aren't friends, does not work
            test(DbUser.ProfileAccessType.NoOne, true, false);
            test(DbUser.ProfileAccessType.NoOne, false, false);

            //friends can view, are/aren't friends, does/doesn't work
            test(DbUser.ProfileAccessType.Friends, false, false);
            test(DbUser.ProfileAccessType.Friends, true, true);

            //everyone can view, are/aren't friends, does work
            test(DbUser.ProfileAccessType.Public, true, true);
            test(DbUser.ProfileAccessType.Public, false, true);
        }


        [TestMethod]
        public void TestLogin()
        {
            Admin.SignIn();
        }

        [TestMethod]
        public void TestSignup()
        {
            DeleteAllTestObjs();
            if (Database.GetCurrentUser() != null) Database.SignOutAsync().Await();
            var u = Tester.SignUp();
            var u2 = DB.GetObjectAsync<DbUser>(u.Uid).Await();
            Assert.AreEqual(u.Uid, u2.Uid);
            Assert.AreEqual(u.Username, u2.Username);
            Assert.AreEqual(u.DisplayUsername, u2.DisplayUsername);
            Assert.AreEqual(u.DisplayUsername.ToLower(), u2.Username);
            Assert.AreNotEqual(u.DisplayUsername, u2.Username);
            Assert.AreEqual(u.Email, u2.Email);

        }


        [TestMethod]
        public void TestRemoveAccount()
        {
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);

            var s = DB.Client.GetSessionQuery().WhereEqualTo("user", DbUser.Reference(u.Uid).Underlying).FindAsync().ToPromise().Await();
            Assert.AreEqual(1, s.Count());


            Admin.SignIn();
            u.DeleteAsync().Await();
            Assert.ThrowsException<ParseFailureException>(() => DB.GetUserAsync(u.Uid).Await());

            s = DB.Client.GetSessionQuery().WhereEqualTo("user", DbUser.Reference("4nmwJKvaZI").Underlying).FindAsync().ToPromise().Await();
            Assert.AreEqual(0, s.Count());
        }

        [TestMethod]
        public void TestSearch()
        {
            Debugger.Disable();
            // Admin.SignIn();
            DeleteAllTestObjs();
            Debugger.Enable();
            var u1 = Tester.SignUp(1);
            var u1Username = u1.Username;
            var u2 = Tester.SignUp(2);
            var u2Username = u2.Username;
            var u3 = Tester.SignUp(3);
            var u3Username = u3.Username;
            var u4 = Tester.SignUp(4);
            var u4Username = u4.Username;
            //

            //assert that the signed out users don't have essential data left in cache.
            Assert.AreEqual("", u1.Username);
            Assert.AreEqual("", u2.Username);
            Assert.AreEqual("", u3.Username);
            Assert.AreEqual(u4Username, u4.Username);

            // //Cache has been cleared now. We need to fetch the users again.
            DB.FetchObjectsAsync(new[] { u1, u2, u3 }).Await();
            Debugger.Print($"u1: {u1.Uid}, username: {u1.Username}\nu2: {u2.Uid}, username: {u2.Username}\nu3: {u3.Uid}, username: {u3.Username}\nu4: {u4.Uid}, username: {u4.Username}");
            //assert that the fetch order is correct
            Assert.AreEqual(u1Username, u1.Username);
            Assert.AreEqual(u2Username, u2.Username);
            Assert.AreEqual(u3Username, u3.Username);
            Assert.AreEqual(u4Username, u4.Username);


            string search = u1.DisplayUsername.Substring(0, 4);

            Debugger.Print("SEARCHTERM: " + search);
            var d = DB.SearchUsersAsync(search, 2).Await();
            Debugger.PrintJson(d);
            Assert.AreEqual(2, d.Length);
            Debugger.Print(u1.DisplayUsername);
            Assert.AreEqual(u1.DisplayUsername, d[0].DisplayUsername);
            Assert.AreEqual(u2.DisplayUsername, d[1].DisplayUsername);
            var d2 = DB.SearchUsersAsync(search, d[1].DisplayUsername, 2).Await();
            Debugger.PrintJson(d2);
            Assert.AreEqual(2, d2.Length);
            Assert.AreEqual(u3.DisplayUsername, d2[0].DisplayUsername);

            //last characters of u1.DisplayUsername
            search = u3.DisplayUsername.Substring(u3.DisplayUsername.Length - 4, 4);
            Debugger.Print("SEARCHTERM: " + search);
            var d3 = DB.SearchUsersAsync(search, 2).Await();
            Debugger.PrintJson(d3);
            // Assert.AreEqual(1, d3.Length);
            Assert.AreEqual(u3.DisplayUsername, d3[0].DisplayUsername);

            search = u3.DisplayUsername.Substring(1, 4);
            Debugger.Print("SEARCHTERM: " + search);
            var d4 = DB.SearchUsersAsync(search, 2).Await();
            Debugger.PrintJson(d4);
            Assert.AreEqual(u1.DisplayUsername, d4[0].DisplayUsername);

            DeleteAllTestObjs();
        }

        [TestMethod]
        public void TestEmail()
        {
            Admin.SignIn();
            foreach (var dbUser in DB.GetUserQuery().WhereEqualTo("email", "theodor.lundqvist2@gmail.com").FindAsync().Await())
            {
                Debugger.Print("Delete user with email: " + dbUser.Email);
                dbUser.DeleteAsync().Await();
            }

            DB.SignUpAsync("BS_TESTER18972", "theodor.lundqvist2@gmail.com", "asdASD123").Await();
        }



        [TestMethod]
        public void TestFirebaseSignInMANUAL()
        {
            // SET REAL PASSWORD IN PARSE TO SOMETHING ELSE, ex 123ASDasd 
            var u = DB.Client.GetCurrentUser();
            Debugger.Print(u);
            // new password is okay
            DB.Client.LogInAsync("theo", "123ASDasd").ToPromise().Await();
            // old password is not okay 
            Assert.ThrowsException<ParseFailureException>(() => DB.Client.LogInAsync("theo", "asdASD123").ToPromise().Await());

            DB.SignOutFromAllDevicesAsync().Await();
            // firebase password should be okay to use
            var user = DB.SignInAsync("theo", "asdASD123").Await();

            Debugger.PrintJson(user);
            // DB.SignOutFromAllDevicesAsync();
            DB.Client.LogInAsync("theo", "asdASD123").ToPromise().Await();
            //This password in not valid on parse server, but is valid on firebase so the password should now be transferred to parse server.

        }


        [TestMethod]
        public void TestReferenceAsync()
        {
            DeleteAllTestObjs();

            var data = "BS_TABLE_TEST_0";

            Admin.SignIn();
            var c = new DbTable(data);
            c.Underlying.SaveAsync().ToPromise().Await();
            DB.AddToCache(ref c);
            Debugger.PrintJson(c);

            var c2 = DB.ReferenceAsync<DbTable>(c.Uid, true).Await(); //Should fetch locally
            Assert.AreSame(c, c2);
            Assert.AreEqual(data, c2.Data[0]);

            DB.ClearLocalCache(); //clear objects


            Assert.AreEqual(0, c.Data.Count()); //should have no data now.

            Debugger.PrintJson(c);
            Debugger.Print("Should fetch from server since updatedAt has been set to null");
            var c3 = DB.ReferenceAsync<DbTable>(c.Uid, true).Await(); //Should fetch from server since updatedAt has been set to null
            Debugger.PrintJson(c3);

            Assert.AreEqual(data, c3.Data[0]);
            c3.NoteStale();
            Debugger.Print("Should ask server, server should say we have latest");
            var c4 = DB.ReferenceAsync<DbTable>(c.Uid, true).Await(); //
            Thread.Sleep(10000); //go to the dashboard and change something on the object

            Debugger.Print("Not stale so should not ask server");
            var c5 = DB.ReferenceAsync<DbTable>(c.Uid, true).Await(); //
            Assert.AreSame(c4, c5);


            Debugger.Print("Stale so should ask server, and if you had time to change the object on the server, it should be updated");
            c5.NoteStale();
            var c6 = DB.ReferenceAsync<DbTable>(c.Uid, true).Await(); //
            Assert.AreSame(c4, c6);
            Debugger.PrintJson(c6);

        }

        [TestMethod]
        public void TestSignIn()
        {
            DB.SignInAsync("theo", "asdASD123").Await();
        }


        [TestMethod]
        public void TestPostReaction()
        {
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);
            var p = new DbBridgeProblem(DbBridgeProblem.Category.Bidding, "BS_POST_TEST", "ddaata");
            p.SaveAsync().Await();

            //CREATE REACTION

            var r = p.AddReactionAsync(DbReaction.ReactionType.Like).Await();
            p.FetchAsync().Await();
            Assert.AreEqual(1, p.Reactions.Count);
            Assert.AreEqual(1, p.Reactions[DbReaction.ReactionType.Like]);
            Debugger.PrintJson(p.Author);
            //REMOVE REACTION
            r.DeleteAsync().Await();
            p.FetchAsync().Await();
            Assert.AreEqual(1, p.Reactions.Count);
            Assert.AreEqual(0, p.Reactions[DbReaction.ReactionType.Like]);

            //CHANGE REACTION TYPE
            r = p.AddReactionAsync(DbReaction.ReactionType.Like).Await();
            p.FetchAsync().Await();
            Assert.AreEqual(1, p.Reactions.Count);
            Assert.AreEqual(1, p.Reactions[DbReaction.ReactionType.Like]);
            r.SetReactionData(DbReaction.ReactionType.Dislike).SaveAsync().Await();
            p.FetchAsync().Await();
            Assert.AreEqual(2, p.Reactions.Count);
            Assert.AreEqual(1, p.Reactions[DbReaction.ReactionType.Dislike]);
            Assert.AreEqual(0, p.Reactions[DbReaction.ReactionType.Like]);
        }


        [TestMethod]
        public void TestPostChat()
        {
            //CREATE POST AND AUTOMATIC CHAT CREATION
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);
            var p = new DbBridgeProblem(DbBridgeProblem.Category.Bidding, "BS_POST_TEST", "ddaata");
            p.SaveAsync().Await();
            Assert.AreEqual(null, p.Chat);
            p.FetchAsync().Await();
            Assert.AreEqual(10, p.Chat.Uid.Length);
            Debugger.Print("Chat uid: " + p.Chat.Uid);

            //ADD MESSAGE
            var m = p.Chat.AddMessageAsync("BS_MESSAGE_TEST", u).Await();

            //CREATE REACTION
            var r = m.AddReactionAsync(DbReaction.ReactionType.Like).Await();
            m.FetchAsync().Await();
            Assert.AreEqual(1, m.Reactions.Count);
            Assert.AreEqual(1, m.Reactions[DbReaction.ReactionType.Like]);

            //REMOVE REACTION
            r.DeleteAsync().Await();
            m.FetchAsync().Await();
            Assert.AreEqual(1, m.Reactions.Count);
            Assert.AreEqual(0, m.Reactions[DbReaction.ReactionType.Like]);

            //CHANGE REACTION TYPE
            r = m.AddReactionAsync(DbReaction.ReactionType.Like).Await();
            m.FetchAsync().Await();
            Assert.AreEqual(1, m.Reactions.Count);
            Assert.AreEqual(1, m.Reactions[DbReaction.ReactionType.Like]);
            r.SetReactionData(DbReaction.ReactionType.Dislike).SaveAsync().Await();

            m.FetchAsync().Await();
            Assert.AreEqual(2, m.Reactions.Count);
            Assert.AreEqual(1, m.Reactions[DbReaction.ReactionType.Dislike]);
            Assert.AreEqual(0, m.Reactions[DbReaction.ReactionType.Like]);


            //READ CHAT AND REACTIONS FROM OTHER USER
            var u2 = Tester.SignUp(2);
            var p2 = DB.ReferenceAsync<DbBridgeProblem>(p.Uid, true).Await();
            Assert.AreEqual(10, p2.Chat.Uid.Length);
            var messages = p2.Chat.FetchNewMessagesAsync().Await();
            Assert.AreEqual(1, messages.Count); //CRASHES NEXT LINE

            var mx2 = p2.Chat.GetAllDownloadedMessages();
            Assert.AreEqual(1, mx2.Count);
            var m2 = mx2[0];
            Assert.AreEqual(2, m2.Reactions.Count);
            Assert.AreEqual(1, m2.Reactions[DbReaction.ReactionType.Dislike]);
            Assert.AreEqual(0, m2.Reactions[DbReaction.ReactionType.Like]);

            //REACT ON POST WITH CUSTOM DATA
            var r2 = p2.AddReactionAsync(12486915).Await();

            p2.FetchAsync().Await();
            Assert.AreEqual(1, p2.Reactions.Count);
            Assert.AreEqual(1, p2.GetReactionsWithCustomData[12486915]);


        }

        [TestMethod]
        public void TestPostCategories()
        {
            //CREATE POST AND AUTOMATIC CHAT CREATION
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);
            var p = new DbBridgeProblem(DbBridgeProblem.Category.Bidding, "BS_POST_TEST", "ddaata");
            p.SaveAsync().Await();
            var p2 = new DbFeatureRequest("BS_POST_TEST", "ddaata").SaveAsync().Await();

            var p3 = new DbNewsItem("BS_POST_TEST", "desc", "ddaata", DbNewsItem.Category.News);

            Assert.ThrowsException<ParseFailureException>(() => p3.SaveAsync().Await()); //only admin can create

            Admin.SignIn();
            p3 = new DbNewsItem("BS_POST_TEST", "desc", "ddaata", DbNewsItem.Category.News);
            p3.SaveAsync().Await();

        }

        [TestMethod]
        public void TestRoom()
        {

            DeleteAllTestObjs();

            // CREATE ROOM
            Admin.SignIn();
            var r = new DbRoom("title", "BS_ROOM_TEST").SaveAsync().Await();


            var u = Tester.SignUp(1);
            // expected: 0 rooms
            Assert.AreEqual(0, u.Rooms.Count());
            Assert.AreEqual(0, new DbQuery<DbRoom>().FindAsync().Await().Count());
            // cant add room
            Assert.ThrowsException<ParseFailureException>(() => u.ArrayAppendUnique("rooms", r.Uid).SaveAsync().Await());
            Admin.SignIn();
            var roomCode = new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST")
                    .SaveAsync().Await().RedemptionCode;
            
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomUser-" + r.Uid } }).Await();
            // u.ArrayAppendUnique("rooms", r.Uid).SaveAsync().Await();
            // Database.AssignRoomModAsync(u, r).Await();

            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(roomCode).Await();
            // u.FetchAsync().Await();
            // expected: 1 room 

            Assert.AreEqual(1, u.Rooms.Count());
            Assert.AreEqual(1, new DbQuery<DbRoom>().FindAsync().Await().Count());

            // expected: not allowed to update/create room

            r.Name = "new title";
            Assert.ThrowsException<ParseFailureException>(() => r.SaveAsync().Await()); // Object not found, which is correct

            // must be admin to create room
            Assert.ThrowsException<ParseFailureException>(() => new DbRoom("new title", "BS_ROOM_TEST").SaveAsync().Await());



            // Can't create room with same name
            Admin.SignIn();
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbRoom("title", "BS_ROOM_TEST").SaveAsync().Await()); // should throw exception

            // If user is mod allow editing etc, but not creating new rooms
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomMod-" + r.Uid } }).Await();
            Database.AssignRoomModAsync(u, r).Await();

            Tester.SignIn(1);
            r.Name = "new title";
            r.SaveAsync().Await(); // should now be allowed to edit

            Assert.ThrowsException<ParseFailureException>(() => //but not create new
              new DbRoom("title", "BS_ROOM_TEST").SaveAsync().Await());


        }

        [TestMethod]
        public void TestCourse()
        {
            DeleteAllTestObjs();
            var u1 = Tester.SignUp(1);
            var u2 = Tester.SignUp(2);
            Admin.SignIn();

            // add some other data to the db since it is empty right now
            var noise = new DbRoom("other", "BS_ROOM_TEST").SaveAsync().Await();
            new DbCourse(noise, "other", "BS_COURSE_TEST").SaveAsync().Await();

            var r = new DbRoom("title", "BS_ROOM_TEST").SaveAsync().Await();

            var c1 = new DbCourse(r, "title1", "BS_COURSE_TEST").SaveAsync().Await();
            var c2 = new DbCourse(r, "title2", "BS_COURSE_TEST").SaveAsync().Await();
            var c3 = new DbCourse(r, "title3", "BS_COURSE_TEST").SaveAsync().Await();
            var c4 = new DbCourse(r, "title4", "BS_COURSE_TEST").SaveAsync().Await();
            var c5 = new DbCourse(r, "title5", "BS_COURSE_TEST").SaveAsync().Await();
            var c6 = new DbCourse(r, "title6", "BS_COURSE_TEST").SaveAsync().Await();

            // check same title not allowed
            Assert.ThrowsException<ParseFailureException>(() => new DbCourse(r, "title1", "BS_COURSE_TEST").SaveAsync().Await());

            // Add users to room
            var vouch = new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST")
                {MaxUses = 2}
                .SaveAsync().Await();
            var code = vouch.RedemptionCode;
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u1.Uid }, { "role", "roomUser-" + r.Uid } }).Await();
            // u1.ArrayAppendUnique("rooms", r.Uid).SaveAsync().Await();

            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u2.Uid }, { "role", "roomUser-" + r.Uid } }).Await();
            // u2.ArrayAppendUnique("rooms", r.Uid).SaveAsync().Await();

            Tester.SignIn(2);
            Database.RedeemVoucherCodeAsync(vouch.RedemptionCode).Await();

            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(vouch.RedemptionCode).Await();
            u1.FetchAsync().Await();
            Assert.AreEqual(1, u1.Rooms.Count()); // 1 room

            Debugger.Print("Fetch courses..."); // sorts title descending
            var courses = u1.Rooms[0].Courses.FetchGreaterItemsAsync(3).Await(); // first 3 courses
            var titles = courses.Select(x => x.Title).ToArray();
            Debugger.PrintJson(titles);
            CollectionAssert.AreEqual(titles.ToArray(), new[] { "title1", "title2", "title3" });

            Debugger.Print("Fetch more courses..."); // sorts title descending
            courses = u1.Rooms[0].Courses.FetchGreaterItemsAsync(4).Await(); // first 3 courses
            titles = courses.Select(x => x.Title).ToArray();
            Debugger.PrintJson(titles);
            CollectionAssert.AreEqual(titles.ToArray(), new[] { "title1", "title2", "title3", "title4", "title5", "title6" });

            Debugger.Print("Check all courses..."); // sorts title descending
            courses = u1.Rooms[0].Courses.GetAllFetchedItems(); // first 3 courses
            titles = courses.Select(x => x.Title).ToArray();
            Debugger.PrintJson(titles);
            CollectionAssert.AreEqual(titles.ToArray(), new[] { "title1", "title2", "title3", "title4", "title5", "title6" });

            //check that user can't create or modify courses
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbCourse(r, "title7", "BS_COURSE_TEST").SaveAsync().Await()); // should throw exception

            c6.Title = "new title";
            Assert.ThrowsException<ParseFailureException>(() => c6.SaveAsync().Await()); // should throw exception
            c6.LengthWeeks = 11;
            Assert.ThrowsException<ParseFailureException>(() => c6.SaveAsync().Await()); // should throw exception
            c6.StartDate = DateTime.Today.AddDays(1);
            Assert.ThrowsException<ParseFailureException>(() => c6.SaveAsync().Await()); // should throw exception
            // check that user can't delete courses
            Assert.ThrowsException<ParseFailureException>(() => c6.DeleteAsync().Await()); // should throw exception 


            // GET ALL USERS IN THIS ROOM
            Debugger.Print("Check users in room..."); // sorts displayName descending
            var users = u1.Rooms[0].Users.FetchGreaterItemsAsync().Await(); // all users
            var names = users.Select(x => x.Username).ToArray();
            Debugger.PrintJson(names);
            CollectionAssert.AreEqual(names.ToArray(), new[] { "bs_tester1", "bs_tester2" });

            Debugger.Print("Check users in course..."); // sorts displayName descending
            users = courses[0].Users.FetchGreaterItemsAsync().Await(); // all users
            names = users.Select(x => x.DisplayUsername).ToArray();
            Debugger.PrintJson(names);
            CollectionAssert.AreEqual(names.ToArray(), new string[0]);

            //make user mod
            Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u2.Uid }, { "role", "roomMod-" + r.Uid } }).Await();
            Database.AssignRoomModAsync(u2, r).Await();
            Tester.SignIn(2);

            c6.Title = "new title";
            c6.LengthWeeks = 11;
            var time = DateTime.Today.AddDays(1);
            c6.StartDate = time;
            c6.SaveAsync().Await(); //can save these fields
            
            Tester.SignIn(1); //clear cache
            Assert.AreEqual("", c6.Title);
            c6 = Database.GetObjectAsync<DbCourse>(c6.Uid).Await();
            Assert.AreEqual("new title", c6.Title);
            Assert.AreEqual(11, c6.LengthWeeks);
            Assert.AreEqual(time, c6.StartDate);
            
            Tester.SignIn(2);
            c6.Members = 12;
            Assert.ThrowsException<ParseFailureException>(() => c6.SaveAsync().Await());
            // check that user can delete courses
            c6.DeleteAsync().Await();
        }

        [TestMethod]
        public void TestChapter()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            //create two rooms with two courses each
            var r1 = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            var c1 = new DbCourse(r1, "course1", "BS_COURSE_TEST", free:true).SaveAsync().Await();
            var c2 = new DbCourse(r1, "course2", "BS_COURSE_TEST").SaveAsync().Await();
            var c3 = new DbCourse(r2, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            var c4 = new DbCourse(r2, "course2", "BS_COURSE_TEST").SaveAsync().Await();
            var roomVoucher = new DbRoomEntryVoucher(r1, "BS_VOUCHER_TEST"){MaxUses = 3}.SaveAsync().Await();
            var roomEntryCode = roomVoucher.RedemptionCode;
            var courseVoucher = new DbCourseEntryVoucher(c1, "BS_VOUCHER_TEST"){MaxUses = 3}.SaveAsync().Await();
            var courseEntryCode = courseVoucher.RedemptionCode;

            //create two chapters in each course
            var courses = new List<DbCourse>() { c1, c2, c3, c4 };
            DbChapter chapter = null;
            foreach (var c in courses)
            {
                new DbChapter(c.Room, c, 1, "chapter1", "BS_CHAPTER_TEST").SaveAsync().Await();
                chapter = new DbChapter(c.Room, c, 2, "chapter2", "BS_CHAPTER_TEST").SaveAsync().Await();
            }

            var u = Tester.SignUp(1);
            // check that user does not have access to chapters
            // assert cant create
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbChapter(r1, c1, 12, "chapter3", "BS_CHAPTER_TEST").SaveAsync().Await());
            // assert cant query
            Assert.AreEqual(0, c1.Chapters.FetchGreaterItemsAsync().Await().Count());
            var zero = new DbQuery<DbChapter>().FindAsync().Await();
            Debugger.PrintJson(zero);
            Assert.AreEqual(0, new DbQuery<DbChapter>().FindAsync().Await().Count());
            // assert cant modify
            chapter.Title = "new title";
            Assert.ThrowsException<ParseFailureException>(() => chapter.SaveAsync().Await());
            // assert cant delete
            Assert.ThrowsException<ParseFailureException>(() => chapter.DeleteAsync().Await());


            // add user to room
            // Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomUser-" + r1.Uid } }).Await();
            // u.ArrayAppend("rooms", r1.Uid).SaveAsync().Await();
            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(roomEntryCode).Await();
            
            // check that user can access courses but not chapters
            // assert can query
            var rooms = u.Rooms;
            Assert.AreEqual(1, rooms.Count);
            courses = rooms[0].Courses.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(2, courses.Count);
            var chapters = courses[0].Chapters.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(0, chapters.Count);


            //add role courseUser-id to user
            // Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "courseUser-" + c1.Uid } }).Await();
            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(courseEntryCode).Await();
            // check that user can access courses but not chapters
            // assert can query
            rooms = u.Rooms;
            Assert.AreEqual(1, rooms.Count);
            courses = rooms[0].Courses.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(2, courses.Count);
            chapters = courses[0].Chapters.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(2, chapters.Count);



            // assert cant create
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbChapter(r1, c1, 12, "chapter3", "BS_CHAPTER_TEST").SaveAsync().Await());
            // assert cant modify
            chapters[0].Title = "new title";
            Assert.ThrowsException<ParseFailureException>(() => chapters[0].SaveAsync().Await());
            // assert cant delete
            Assert.ThrowsException<ParseFailureException>(() => chapters[0].DeleteAsync().Await());


            // assert only first course chapters are visible
            var q = new DbQuery<DbChapter>().FindAsync().Await();
            Assert.AreEqual(2, q.Count());

        }

        [TestMethod]
        public void TestQuestion()
        {

            DeleteAllTestObjs();
            Admin.SignIn();
            //create two rooms with two courses each
            var r1 = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            var c1 = new DbCourse(r1, "course1", "BS_COURSE_TEST", free:true).SaveAsync().Await();
            var c2 = new DbCourse(r1, "course2", "BS_COURSE_TEST").SaveAsync().Await();
            var c3 = new DbCourse(r2, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            var c4 = new DbCourse(r2, "course2", "BS_COURSE_TEST").SaveAsync().Await();

            var q1 = new DbQuestion(r1, "question1", "BS_QUESTION_TEST").SaveAsync().Await();
            var roomVoucher = new DbRoomEntryVoucher(r1, "BS_VOUCHER_TEST").SaveAsync().Await();
            var roomEntryCode = roomVoucher.RedemptionCode;
            var courseVoucher = new DbCourseEntryVoucher(c1, "BS_VOUCHER_TEST").SaveAsync().Await();
            var courseEntryCode = courseVoucher.RedemptionCode;
            //create two chapters in each course
            var courses = new List<DbCourse>() { c1, c2, c3, c4 };
            DbChapter chapter = null;
            foreach (var c in courses)
            {
                new DbChapter(c.Room, c, 1, "chapter1", "BS_CHAPTER_TEST").SaveAsync().Await();
                chapter = new DbChapter(c.Room, c, 2, "chapter2", "BS_CHAPTER_TEST").SaveAsync().Await();
            }


            var u = Tester.SignUp(1);
            //check that user can't access existing questions
            var xs = new DbQuery<DbQuestion>().FindAsync().Await();
            Assert.AreEqual(0, xs.Count());

            //cant create when not member of room
            Assert.ThrowsException<ParseFailureException>(() => new DbQuestion(r1, "question1", "BS_QUESTION_TEST").SaveAsync().Await());

            //add user to room
            // Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomUser-" + r1.Uid } }).Await();
            // u.ArrayAppend("rooms", r1.Uid).SaveAsync().Await();
            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(roomEntryCode).Await();
            // u.FetchAsync().Await();
            //check that user now can access existing questions
            var questions = u.Rooms[0].Questions.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(1, questions.Count);

            //check that user can post own question
            var q2 = new DbQuestion(r1, "question2", "BS_QUESTION_TEST").SaveAsync().Await();
            questions = r1.Questions.FetchGreaterItemsAsync().Await();
            Assert.AreEqual(2, questions.Count);
            foreach (var q in questions)
            {
                Debugger.Print(q.Title);
                Debugger.Print(q.Chat.Uid);
            }
            //assert cant modify
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                questions[0].Title = "new title";
                questions[0].SaveAsync().Await();
            });

            //assert can access question chat and post messages
            var chat = questions[0].Chat;
            Assert.AreEqual("new title", questions[0].Title); //check that it is the right question, new title is still set locally
            var m = new DbMessage(u, chat, "BS_MESSAGE_TEST");
            m.SaveAsync().Await();

            chat = questions[1].Chat;
            m = new DbMessage(u, chat, "BS_MESSAGE_TEST").SaveAsync().Await();

            //assert cant modify chat
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                chat.Name = "new title";
                chat.SaveAsync().Await();
            });
            //assert can modify message
            // Assert.ThrowsException<ParseFailureException>(() => {
            m.Text = "new title";
            m.SaveAsync().Await();
            // });

            m.NotifyReceived().Await();
            m.NotifyRead().Await(); // a read message cant be edited anymore

            //assert cant modify message
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                m.Text = "new title";
                m.SaveAsync().Await();
            });

            //assert admin can modify message
            Admin.SignIn();
            m.Text = "new title";
            m.SaveAsync().Await();
            //make user mod
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomMod-" + r1.Uid } }).Await();
            Database.AssignRoomModAsync(u, r1).Await();
            var m2 = new DbMessage(u, chat, "BS_MESSAGE_TEST").SaveAsync().Await();
            Tester.SignIn(1);
            //sender not test1 so test1 should not be able to modify even if mod

            //assert cant modify message but can delete
            m2.Text = "new title";
            Assert.ThrowsException<ParseFailureException>(() => m2.SaveAsync().Await());
            m2.DeleteAsync().Await();

        }

        [TestMethod]
        public void TestPracticeSet()
        {

            DeleteAllTestObjs();
            Admin.SignIn();
            //create two rooms with two courses each
            var r1 = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            var c1 = new DbCourse(r1, "course1", "BS_COURSE_TEST", free: true).SaveAsync().Await();
            var c2 = new DbCourse(r1, "course2", "BS_COURSE_TEST", free: true).SaveAsync().Await();
            var c3 = new DbCourse(r2, "course1", "BS_COURSE_TEST", free: true).SaveAsync().Await();
            var c4 = new DbCourse(r2, "course2", "BS_COURSE_TEST", free: true).SaveAsync().Await();

            var roomVoucher = new DbRoomEntryVoucher(r1, "BS_VOUCHER_TEST").SaveAsync().Await();
            var roomEntryCode = roomVoucher.RedemptionCode;

            var courseVoucher = new DbCourseEntryVoucher(c1, "BS_VOUCHER_TEST").SaveAsync().Await();
            var courseEntryCode = courseVoucher.RedemptionCode;
            //create two chapters in each course
            var courses = new List<DbCourse>() { c1, c2, c3, c4 };
            DbChapter chapter = null;
            DbPracticeSet ps = null;
            foreach (var c in courses)
            {
                new DbChapter(c.Room, c, 1, "chapter1", "BS_CHAPTER_TEST").SaveAsync().Await();
                chapter = new DbChapter(c.Room, c, 2, "chapter2", "BS_CHAPTER_TEST").SaveAsync().Await();
                ps = new DbPracticeSet(r1, chapter, "title1", "BS_PRACTICE_TEST", "test_data").SaveAsync().Await();
            }

            DbChapter c2Chapter = c2.Chapters.FetchGreaterItemsAsync().Await()[0];

            var u = Tester.SignUp(1);
            //check user no access to ps
            Assert.AreEqual(0, new DbQuery<DbPracticeSet>().FindAsync().Await().Count());
            //add user to room
            // Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomUser-" + r1.Uid } }).Await();
            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(roomEntryCode).Await();
            //check user no access to ps
            Assert.AreEqual(0, new DbQuery<DbPracticeSet>().FindAsync().Await().Count());


            //add user to right course
            // Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "courseUser-" + c1.Uid } }).Await();
            Tester.SignIn(1);
            Database.RedeemVoucherCodeAsync(courseEntryCode).Await();
            //check user access to ps
            Assert.AreEqual(1, new DbQuery<DbPracticeSet>().FindAsync().Await().Count());

            //check user only access to total 1 practice sea 
            var psx = new DbQuery<DbPracticeSet>().FindAsync().Await();
            Assert.AreEqual(1, psx.Count());
            //check user cant add more practice sets 
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbPracticeSet(r1, chapter, "title1", "BS_PRACTICE_TEST", "test_data").SaveAsync().Await());
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbPracticeSet(r1, chapter, "other title", "BS_PRACTICE_TEST", "test_data").SaveAsync().Await());

            Assert.ThrowsException<ParseFailureException>(() =>
              new DbPracticeSet(r1, c2Chapter, "title1", "BS_PRACTICE_TEST", "test_data").SaveAsync().Await());
            //check cant modify
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                ps.Title = "new title";
                ps.SaveAsync().Await();
            });
            //check cant delete
            Assert.ThrowsException<ParseFailureException>(() => ps.DeleteAsync().Await());

            //make user mod
            Admin.SignIn();
            Database.AssignRoomModAsync(u, r1).Await();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomMod-" + r1.Uid } }).Await();
            Tester.SignIn(1);
            //check user can now edit and delete practice sets
            ps.Title = "new title";
            ps.SaveAsync().Await();
            ps.DeleteAsync().Await();

        }

        [TestMethod]
        public void TestRedeemRoomCode()
        {

        }

        [TestMethod]
        public void TestRoomEntryVoucher()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            //create a room and a user
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            new DbCourse(r, "course1", "BS_COURSE_TEST", free:true).SaveAsync().Await();

            var voucher = new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10))
              .SaveAsync().Await();

            var u = Tester.SignUp(1);

            //check user cant view vouchers
            Assert.AreEqual(0, new DbQuery<DbVoucher>().FindAsync().Await().Count());

            //create voucher


            //check user cant create voucher
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10))
                .SaveAsync().Await()
              );
            //add user to room
            Admin.SignIn();
            Database.AssignRoleAsync(u, "roomUser-" + r.Uid).Await();
            Tester.SignIn(1);

            //check user cant view vouchers
            Assert.AreEqual(0, new DbQuery<DbVoucher>().FindAsync().Await().Count());


            //check user cant create voucher
            Assert.ThrowsException<ParseFailureException>(() =>
              new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10))
                .SaveAsync().Await()
              );

            //make user mod
            Admin.SignIn();
            Database.AssignRoomModAsync(u, r).Await();
            Tester.SignIn(1);

            //check user can view vouchers
            Assert.AreEqual(1, new DbQuery<DbVoucher>().FindAsync().Await().Count());

            //check user can create voucher
            var rv = new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10))
              .SaveAsync().Await();
            //check user can create course
            var c = new DbCourse(r, "course2", "BS_COURSE_TEST").SaveAsync().Await();
            //check user can create course voucher
            var cv = new DbCourseEntryVoucher(c, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10))
              .SaveAsync().Await();
            var courseCode = cv.RedemptionCode;
            var roomCode = rv.RedemptionCode;


            //check user can view vouchers
            Assert.AreEqual(3, new DbQuery<DbVoucher>().FindAsync().Await().Count());


            // check subtypelimiter
            Assert.AreEqual(1, new DbQuery<DbCourseEntryVoucher>().FindAsync().Await().Count());
            Assert.AreEqual(2, new DbQuery<DbRoomEntryVoucher>().FindAsync().Await().Count());

            //check user can modify voucher, enable/disable
            rv.Enabled = !rv.Enabled;
            rv.Increment("maxUses", 10);
            rv.ValidUntil = rv.ValidUntil.Value.AddDays(10);
            rv.Description = "new BS_VOUCHER_TEST";
            rv.RedemptionCode = "CUSTOM CODE";
            roomCode = rv.RedemptionCode;
            rv.SaveAsync().Await();
            rv.Description = "BS_VOUCHER_TEST"; //reset, so we can delete it later with the others
            rv.Enabled = true;
            rv.SaveAsync().Await();

            //check user cant modify voucher
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.Increment("uses", 10);
                rv.SaveAsync().Await();
            });
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.SetValue("usedAt", new DateTime[] { DateTime.Now });
                rv.SaveAsync().Await();
            });
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.SetValue("usedBy", new string[] { u.Uid });
                rv.SaveAsync().Await();
            });
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.SetValue("data", r2.Uid); //must be valid room
                rv.SaveAsync().Await();
            });
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.SetValue("type", 2);
                rv.SaveAsync().Await();
            });
            Assert.ThrowsException<ParseFailureException>(() =>
            {
                rv.ArrayAppend("pending", u.Uid);
                rv.SaveAsync().Await();
            });

            //check user can use voucher
            Tester.SignUp(2);
            Assert.IsTrue(rv.RedemptionCode.Length > 0); //uid is not wiped
            var res = Database.RedeemVoucherCodeAsync(roomCode).Await();
            Debugger.PrintJson(res);
            Assert.AreEqual(DbVoucher._VoucherType.RoomEntry, res.Type);
            Assert.AreEqual("BS_VOUCHER_TEST", res.Description);
            // if(!res.Url.Contains(r.Uid))
            //     Assert.Fail("url does not contain room uid");
            //user is now member of course so should be able to redeem course voucher
            res = Database.RedeemVoucherCodeAsync(courseCode).Await();
            Assert.AreEqual(DbVoucher._VoucherType.CourseEntry, res.Type);
            Assert.AreEqual("BS_VOUCHER_TEST", res.Description);



        }


        [TestMethod]
        public void TestSearchInRoom()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var u = Tester.SignUp(1);
            Tester.SignIn(1);
            var res = Database.SearchUsersAsync("bs_tester1").Await();
            Assert.AreNotEqual(0, res.Count());
            res = Database.SearchUsersAsync("bs_tester1", room: r.Uid).Await();
            Assert.AreEqual(0, res.Count());

            Admin.SignIn();
            // add user to room
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u.Uid }, { "role", "roomUser-" + r.Uid } }).Await();
            // u.ArrayAppend("rooms", r.Uid).SaveAsync().Await();
            Database.AssignRoomModAsync(u, r).Await();

            // add some noise
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            var u2 = Tester.SignUp(2);
            Admin.SignIn();
            // Database.CallCloudFunctionAsync<string>("addUserToRole",
            //   new Dictionary<string, object>() { { "uid", u2.Uid }, { "role", "roomUser-" + r2.Uid } }).Await();
            // u.ArrayAppend("rooms", r2.Uid).SaveAsync().Await();
            Database.AssignRoomModAsync(u2, r2).Await();

            Tester.SignIn(1);
            res = Database.SearchUsersAsync("bs_tester1", room: r.Uid).Await();
            Assert.AreEqual(1, res.Count());
        }

        [TestMethod]
        public void RemoveAllTestObjects()
        {
            DeleteAllTestObjs();
        }



        [TestMethod]
        public void TestDeleteTestObjects()
        {
            Admin.SignIn();
            DeleteAllTestObjs();
            new DbQuery<DbRoom>().WhereEqualTo("name", "BS_ROOM_TEST")
                .FindAsync().Await().DeleteObjectsAsync().Await();

            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            DB.Connect(dev:false);
            Admin.SignIn();
            var other = new DbRoom("BS_ROOM_TEST", "not a test object").SaveAsync().Await();
            DB.Connect(dev:true);
            Admin.SignIn();
            DeleteAllTestObjs();
            //check r is deleted but not other then delete other
            //query for room with name room1 and assert count = 0
            Assert.AreEqual(0,
                new DbQuery<DbRoom>().WhereEqualTo("name", "room1").FindAsync().Await().Count());

            other.FetchAsync().Await();
            other.DeleteAsync().Await();
        }

        [TestMethod]
        public void TestGetPaymentLink()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            //create course code
            var cv = new DbCourseEntryVoucher(c, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10));
            cv.SaveAsync().Await();
            Debugger.PrintJson(cv);
            string code = cv.RedemptionCode; //save, otherwise cache is cleaned
            Debugger.Print($"code: {code}");
            Tester.SignUp(1);
            //redeem course code
            var res = Database.RedeemVoucherCodeAsync(code).Await();
            Debugger.Print(res.PaymentLink); //temporary null since payment is disabled right now 
            Assert.IsTrue(res.PaymentLink.Contains("https"));

        }

        [TestMethod]
        public void TestVoucher()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var r2 = new DbRoom("room2", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            var c2 = new DbCourse(r2, "course2", "BS_COURSE_TEST").SaveAsync().Await();

            //create course code
            var cv = new DbCourseEntryVoucher(c, "BS_VOUCHER_TEST", validUntil: DateTime.Now.AddDays(10));
            cv.SaveAsync().Await();
            string code = cv.RedemptionCode; //save, otherwise cache is cleaned
            Debugger.Print($"code: {code}");
            var u = Tester.SignUp(1);
            //redeem course code
            var res = Database.RedeemVoucherCodeAsync(code).Await();
            Debugger.Print(res.PaymentLink);
            Assert.IsTrue(res.PaymentLink.Contains("https"));

            Admin.SignIn();
            //check voucher pending
            cv.FetchAsync().Await();
            Assert.AreEqual(u.Uid, cv.PendingUses.First()?.Uid);
            Assert.AreEqual(1, cv.Uses);
            Assert.AreEqual(0, cv.UsedAt.Count);
            Assert.AreEqual(0, cv.UsedBy.Count);

            Database.CallCloudFunctionAsync<string>("testCheckoutExpired",
                new() { { "voucher", cv.Uid }, { "user", u.Uid } }).Await();

            cv.FetchAsync().Await();
            Assert.AreEqual(0, cv.PendingUses.Count);
            Assert.AreEqual(0, cv.Uses);

            Tester.SignIn(1);
            res = Database.RedeemVoucherCodeAsync(cv.RedemptionCode).Await();
            Assert.IsTrue(res.PaymentLink.Contains("https"));

            Admin.SignIn();
            Database.CallCloudFunctionAsync<string>("testCheckoutConfirmed",
                new() { { "user", u.Uid }, { "voucher", cv.Uid } }).Await();

            // Debugger.PrintJson(cv.Underlying);
            cv.FetchAsync().Await();
            // Debugger.PrintJson(cv.Underlying);


            Assert.AreEqual(1, cv.Uses);
            Assert.AreEqual(0, cv.PendingUses.Count);
            Assert.AreEqual(1, cv.UsedAt.Count);
            Debugger.Print($@"
timeDiff(ms): {(cv.UsedAt[0] - DateTime.UtcNow).TotalSeconds}
            ");

            Assert.IsTrue(cv.UsedAt.First().Ticks > DateTime.UtcNow.AddMinutes(-1).Ticks);
            Assert.IsTrue(cv.UsedAt.First().Ticks < DateTime.UtcNow.AddMinutes(1).Ticks);

            Assert.AreEqual(1, cv.UsedBy.Count);

            Tester.SignIn(1);
            //check room access etc
            Assert.ThrowsException<ParseFailureException>(() =>
                Database.GetQuery<DbRoom>().GetObjectByIdAsync(r2.Uid).Await());

            Database.GetQuery<DbRoom>().GetObjectByIdAsync(r.Uid).Await();
            Database.GetQuery<DbCourse>().GetObjectByIdAsync(c.Uid).Await();
            Assert.ThrowsException<ParseFailureException>(() => Database.RedeemVoucherCodeAsync(cv.RedemptionCode).Await());

            Tester.SignUp(2);
            Assert.ThrowsException<ParseFailureException>(() =>
                Database.GetQuery<DbRoom>().GetObjectByIdAsync(r.Uid).Await());
            Assert.ThrowsException<ParseFailureException>(() => Database.RedeemVoucherCodeAsync(cv.RedemptionCode).Await());
        }

        [TestMethod]
        public void TestVoucherCustomCode()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            //create course code
            var cv = new DbRoomEntryVoucher(r,
                "BS_VOUCHER_TEST",
                 validUntil: DateTime.Now.AddDays(10));
            cv.RedemptionCode = "customredemptioncode";
            cv.SaveAsync().Await();

            string code = cv.RedemptionCode; //save, otherwise cache is cleaned
            Debugger.Print($"code: {code}");

            var u = Tester.SignUp(1);
            //redeem course code
            var res = DbVoucher.Redeem(code).Await();
            Debugger.Print(res.PaymentLink);
            Assert.AreEqual(false, res.IsPaymentRequired);
            Assert.AreEqual(null, res.PaymentLink);
            Assert.AreEqual(DbVoucher._VoucherType.RoomEntry, res.Type);

            Assert.AreEqual(r.Uid, res.Data);
            u.FetchAsync().Await();
            Debugger.PrintJson(u.Rooms);
            Assert.IsTrue(u.Rooms.Contains(r));

        }

        [TestMethod]
        public void TestSubscriptionDashboard()
        {
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);
            Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST").SaveAsync().Await();

            Database.CallCloudFunctionAsync<string>("test_assignSubscriptionToCustomer",
                new() { { "user", u.Uid }, { "course", c.Uid } }
            ).Await();
            for (int i = 0; i < 10; i++)
            {
                //wait one second
                u.FetchAsync().Await();
                if (u.HasSubscription(DbUser.Subscription.Education)) break;
                Thread.Sleep(1000);
            }
            Tester.SignIn(1);
            var url = Database.GenerateSubscriptionDashboardLinkAsync().Await();
            Debugger.Print(url);
        }


        [TestMethod]
        public void TestSubscriptionBasedAccess()
        {
            DeleteAllTestObjs();
            var u = Tester.SignUp(1);
            Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST", start: DateTime.Now).SaveAsync().Await();
            var chap = new DbChapter(r, c, 1, "chapter3", "BS_CHAPTER_TEST").SaveAsync().Await();

            Database.CallCloudFunctionAsync<string>("test_assignSubscriptionToCustomer",
                new() { { "user", u.Uid }, { "course", c.Uid } }
            ).Await();

            for (int i = 0; i < 10; i++)
            {
                //wait one second
                u.FetchAsync().Await();
                if (u.HasSubscription(DbUser.Subscription.Education)) break;
                Thread.Sleep(1000);
            }
            Assert.IsTrue(u.HasSubscription(DbUser.Subscription.Education));


            //create room and course voucher
            var vr = new DbRoomEntryVoucher(r, "BS_VOUCHER_TEST").SaveAsync().Await();
            var vr_code = vr.RedemptionCode;
            var vc = new DbCourseEntryVoucher(c, "BS_VOUCHER_TEST").SaveAsync().Await();
            var vc_code = vc.RedemptionCode;

            Tester.SignIn(1);

            Assert.ThrowsException<ParseFailureException>(() => 
                new DbQuery<DbRoom>().GetObjectByIdAsync(r.Uid).Await());
            Assert.ThrowsException<ParseFailureException>(() => 
                new DbQuery<DbCourse>().GetObjectByIdAsync(c.Uid).Await());
            Assert.ThrowsException<ParseFailureException>(() => 
                new DbQuery<DbChapter>().GetObjectByIdAsync(chap.Uid).Await());

            // can use vouchers without paying
            var res = Database.RedeemVoucherCodeAsync(vr_code).Await();
            // check room access
            Assert.IsFalse(res.IsPaymentRequired);
            new DbQuery<DbRoom>().GetObjectByIdAsync(r.Uid).Await();
            new DbQuery<DbCourse>().GetObjectByIdAsync(c.Uid).Await();
            Assert.ThrowsException<ParseFailureException>(() => 
                new DbQuery<DbChapter>().GetObjectByIdAsync(chap.Uid).Await());

            // can use vouchers without paying
            res = Database.RedeemVoucherCodeAsync(vc_code).Await();
            // check room access
            Assert.IsFalse(res.IsPaymentRequired);
            new DbQuery<DbChapter>().GetObjectByIdAsync(chap.Uid).Await();
            var roles = Database.CallCloudFunctionAsync<List<object>>("listMyRoles").Await().Select(x => x.ToString()).ToArray();
            Assert.AreEqual(2, roles.Length); 
            Assert.IsTrue(roles.Contains("roomUser-" + r.Uid));
            Assert.IsTrue(roles.Contains("courseUser-" + c.Uid));
            
            Admin.SignIn();
            Database.CallCloudFunctionAsync<string>("test_cancelSubscription",
                new() { { "user", u.Uid } }
            ).Await();
            for (int i = 0; i < 10; i++)
            {
                //wait one second
                u.FetchAsync().Await();
                if (!u.HasSubscription(DbUser.Subscription.Education)) break;
                Thread.Sleep(1000);
            }

            u.FetchAsync().Await();
            Assert.IsFalse(u.HasSubscription(DbUser.Subscription.Education));
            Database.SignOutAsync().Await();
            Tester.SignIn(1);
            Database.SignOutFromAllDevicesAsync().Await();
            
            var chapId = chap.Uid;
            Database.Cache.RemoveFromCache(ref chap);
            
            Database.EnableLocalCache(false);
            Tester.SignIn(1);
            roles = Database.CallCloudFunctionAsync<List<object>>("listMyRoles").Await().Select(x => x.ToString()).ToArray();
            Assert.AreEqual(1, roles.Length); 
            Assert.IsTrue(roles.Contains("roomUser-" + r.Uid));
            Assert.IsFalse(roles.Contains("courseUser-" + c.Uid));
            
            // can see room
            Debugger.Print("can see room: " + r.Uid);
            new DbQuery<DbRoom>().GetObjectByIdAsync(r.Uid).Await();
            // can see course because is member in room
            Debugger.Print("can see course: " + c.Uid);
            new DbQuery<DbCourse>().GetObjectByIdAsync(c.Uid).Await();
            var chapters = new DbQuery<DbChapter>().FindAsync().Await();
            Debugger.PrintJson(chapters.Select(x => x.Title).ToArray());
            //but cant see chapter because is member it course but does not have education subscription 
            
            Debugger.Print("Check user " +Database.GetCurrentUser().Uid +" cant see chapter: " + chapId);
            //Debugger.Print("chap is already in cache: " + Database.Cache.Contains<DbChapter>(chap.Uid));
            //Debugger.Print("chap is stale: " + chap.IsStale);
            //Debugger.Print("chap title: " + chap.Title);
            //    var z = Database.GetObjectAsync<DbChapter>(chap.Uid).Await();
            //Debugger.Print("chap title: " + chap.Title);
            //Debugger.Print("z title: " + z.Title);
            //Debugger.PrintJson(z);
            Assert.IsNull(chap);
            
            //test that uid = null yields error
            Assert.ThrowsException<ParseFailureException>(() => 
                Database.GetObjectAsync<DbChapter>("").Await());
            Assert.ThrowsException<ParseFailureException>(() => 
                Database.GetObjectAsync<DbChapter>(null).Await());
            
            Database.SignOutAsync().Await();
            Tester.SignIn(1);
            Database.Connect(dev:true);
            Assert.ThrowsException<ParseFailureException>(() => 
                Database.GetObjectAsync<DbChapter>(chapId).Await());
            

            //add subscription again
            Admin.SignIn();
            Database.CallCloudFunctionAsync<string>("test_assignSubscriptionToCustomer",
                new() { { "user", u.Uid }, { "course", c.Uid } }
            ).Await();
            Tester.SignIn(1);
            for (int i = 0; i < 10; i++)
            {
                //wait for stripe webhook
                u.FetchAsync().Await();
                if (u.HasSubscription(DbUser.Subscription.Education)) break;
                Thread.Sleep(1000);
            }
            //check chapter access
            new DbQuery<DbChapter>().GetObjectByIdAsync(chapId).Await();
        }

        [TestMethod]
        public void TestUserStillCAntAccess()
        {
            var u = Tester.SignIn(1);
            var roles = Database.CallCloudFunctionAsync<List<object>>("listMyRoles").Await().Select(x => x.ToString()).ToArray();
            Assert.AreEqual(1, roles.Length); 
            Assert.IsTrue(roles.Contains("roomUser-" + u.Rooms.First().Uid));
            Assert.IsFalse(roles.Contains("courseUser-" + u.Courses.First().Uid));
            Debugger.PrintJson(roles);
            var course = u.Courses.First().FetchAsync().Await();
            var chapters = course.Chapters.FetchGreaterItemsAsync().Await();
            Debugger.PrintJson("Found chapters: " + chapters.Count);
            Debugger.PrintJson(chapters.Select(x => x.Uid));

            var chapters2 = new DbQuery<DbChapter>().FindAsync().Await();
            Debugger.PrintJson(chapters.Select(x => x.Uid));
        }


        [TestMethod]
        public void TestFileUpload()
        { 
           DeleteAllTestObjs();
           Tester.SignUp(1);
           Debugger.Print(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory).ToJson());
           var user = Database.GetCurrentUser();
           
           var image = new byte[] { 0, 1, 2 }; //....
           //or
           var img = ImageUtil.LoadFromFile("/Users/theo/Documents/bridgestars/lib/bridgestars-lib/Test/IMG_2280.jpg");
            
           //clamp size to be max 256px in any direction
           img = ImageUtil.ClampSize(img, 256);
           //pure image upscale to 1000x1000
           img = ImageUtil.ResizeImage(img, 1000, 1000);
           //crop image
           img = ImageUtil.Crop(img, 200, 200, 100, 100);
           //create db rep.
           DbFile file = new DbFile("name.jpeg", img);
           var bytes1 = img.ToByteArray();
           //listen for progress %
          // deprecated  
          // file.UploadProgress.ProgressChanged += (sender, level) =>
          // {
          //     Debugger.Print("progress: " + level.Amount);
          // };
           
           //upload
           file.SaveAsync(amount => Debugger.Print("progress: " + amount)).Await();
           
           //file is now saved and url is accessible to download image
           Assert.IsTrue(file.IsSaved);
           Debugger.Print(file.Url);
          
           //set user avatar reference to image
           user.Avatar = file;
           user.SaveAsync().Await();

           byte[] bytes2 = null;
           user.Avatar.DownloadAsync().Then(x =>
           {
               bytes2 = x;
               File.WriteAllBytes("/Users/theo/Documents/bridgestars/lib/bridgestars-lib/Test/IMG_2280_DOWNLOADED.jpeg",x);
           }).Await();
           CollectionAssert.AreEqual(bytes1, bytes2);
        }









        [TestMethod]
        public void TestTournamentPBNUpload()
        {
            // DeleteAllTestObjs();
            // Admin.SignIn();
            
            //var t = Tournament.FromPBNText(Tournament.TestTournament);
            // var json1 = JsonUtil.ToJson(t);
            //
            // var dbt = DbTournament.NewExternal(t);
            // dbt.SaveAsync().Await();
            //
            //
            // Tester.SignUp(1);
            // dbt.FetchAsync().Await();
            //
            // var file =  dbt.ResultsFile.DownloadAsync().Await();
            //var tournament = Tournament.FromPBNBytes(file);
            
            //var json2 = JsonUtil.ToJson(tournament);
            //Assert.AreEqual(json1, json2);

            
        }

        [TestMethod]
        public void TestAdminGetRoomRoles()
        {
            DeleteAllTestObjs();
            Admin.SignIn();
            var roles = Database.CallCloudFunctionAsync<List<object>>("listMyRoles").Await().Select(x => x.ToString()).ToArray();
            Debugger.PrintJson(roles);

        }












    }
}

