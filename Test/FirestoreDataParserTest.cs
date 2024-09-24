using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project.FirebaseORM.Authentication;
using Project.FirebaseORM.Database;
using Project.FirebaseORM.Database.Data;
using Project.FirebaseORM.Database.Transform;
using Project.Util;
//using Project.Util;
using RSG;
using Debugger = Project.Util.Debugger;

[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]

#pragma warning disable CS0618
#pragma warning disable CS0612
namespace Test
{
    [TestClass]
    public class FirestoreDataParserTest
    {
        void Print(string s) => System.Diagnostics.Debug.WriteLine(s);
        string Pretty(string json) => Project.Util.JsonUtil.Prettify(json);

        private string data =
            "{\"name\":\"projects/bridge-fcee8/databases/(default)/documents/users/gGzDzf5pNthS3DUthcuY2ORKyOs2\",\"fields\":{\"firstName\":{\"stringValue\":\"firstname\"},\"friends\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"uid1\"},{\"stringValue\":\"uid2\"}]}},\"xp\":{\"integerValue\":\"110\"},\"matchHistory\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"asdasd\"},{\"stringValue\":\"asdasd\"}]}},\"username\":{\"stringValue\":\"TEST_username\"},\"usernameLower\":{\"stringValue\":\"test_username\"},\"img\":{\"stringValue\":\"imgurl\"},\"balance\":{\"doubleValue\":1000.5},\"lastName\":{\"stringValue\":\"lastname\"},\"elo\":{\"integerValue\":\"100\"},\"chats\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"chatid\"},{\"stringValue\":\"chatid2\"}]}},\"email\":{\"stringValue\":\"test_username@gmail.com\"},\"timeOfBirth\":{\"stringValue\":\"11/19/2000 18:00:00\"}},\"createTime\":\"2022-04-09T14:34:29.551177Z\",\"updateTime\":\"2022-04-09T14:34:29.551177Z\"}";

        //paste on one line with '' and then option + enter and "to string literal"
        private string mapData =
            "{  \"name\": \"projects/bridge-fcee8/databases/(default)/documents/matchHistory/d8NTF750K5ZPR0ogbW2H\",  \"fields\": {    \"TableTwo\": {      \"mapValue\": {}    },    \"TableOne\": {      \"mapValue\": {        \"fields\": {          \"Data\": {            \"stringValue\": \"asdasd\"          },          \"PlayerSouth\": {            \"stringValue\": \"ps\"          },          \"Id\": {            \"stringValue\": \"id\"          },          \"PlayerWest\": {            \"stringValue\": \"\"          },          \"PlayerNorth\": {            \"stringValue\": \"pn\"          },          \"PlayerEast\": {            \"stringValue\": \"pe\"          }        }      }    }  },  \"createTime\": \"2022-05-03T08:33:53.345236Z\",  \"updateTime\": \"2022-05-03T08:33:53.345236Z\"}";


        [TestMethod]
        public void TestDocTransform()
        {
            var t = new DocumentTransform("users/uid") 
                .Increment("xp",15)
                .RemoveAllFromArray("friends", "uid2");
            Debugger.Print(t.GetTransformPayload());
        }


        [TestMethod]
        public void TestFirestoreMap()
        {
            var match = new BridgeFirestoreMatchHistory(mapData);

            Debugger.Print(match.ToFirestoreJson());
            Assert.IsFalse(match.IsTeamGame);

            match = FirestoreDocument.BuildFromJson<BridgeFirestoreMatchHistory>(mapData);
            Debugger.Print(match);
            Debugger.Print(match.DocId);
            Debugger.Print(match.TableOne);
            Assert.IsFalse(match.IsTeamGame);

            Debugger.Print(match.GetUpdateJson());
        }

        [TestMethod]
        public void TestJsonParsing()
        {
            var user = new BridgeFirestoreUser(data);
            Debugger.Print(user.ToString());
            //Assert.AreEqual("firstname", user.firstName);
            Assert.AreEqual("uid1", user.friends[0]);
            Assert.AreEqual("uid2", user.friends[1]);
            Assert.AreEqual(110, user.xp);
            Assert.AreEqual("asdasd", user.matchHistory[0]);
            Assert.AreEqual("asdasd", user.matchHistory[1]);
            Assert.AreEqual("TEST_username", user.username);
            Assert.AreEqual("test_username", user.usernameLower);
            Assert.AreEqual("imgurl", user.img);
            Assert.AreEqual(1000.5, user.balance);
            Assert.AreEqual(100, user.elo);
            Assert.AreEqual("chatid", user.chats[0]);
            Assert.AreEqual("chatid2", user.chats[1]);
            //Assert.AreEqual("test_username@gmail.com", user.email);
            //Assert.AreEqual("11/19/2000 18:00:00", user.timeOfBirth);

            Assert.AreEqual("2022-04-09T14:34:29.551177Z", user.CreateTime);
            int a = 0;
            //Assert.IsFalse(new FirestoreParser(data).TryGetValue("asd", ref a));
            int[] b = new int[] { };
            //Assert.ThrowsException<Exception>(() => new FirestoreDocumentParser(data).TryGetArray("matchHistory", ref b));
            Assert.AreEqual(0, user.friendRequests.Length);
            Assert.AreEqual(0, user.incomingFriendRequests.Length);
            Assert.AreEqual(0, a);
            user.elo += 100;
            user.matchHistory = user.matchHistory.Append("match2").ToArray();
            Console.WriteLine(user.GetUpdateJson());
            Console.WriteLine(((FirestoreUser)user).GetUpdateJson());
            Assert.AreEqual(user.GetUpdateJson(), ((FirestoreUser)user).GetUpdateJson());
            var u = new BridgeFirestoreUser();
            u.balance = 100;

            Assert.ThrowsException<ArgumentException>(() => u.GetUpdateJson());
        }

        #region promise builders

        IPromise WillReject()
        {
            return new Promise((res, rej) =>
            {
                Thread.Sleep(50);
                rej(new Exception("throws"));
            });
        }

        IPromise WillReject(int i)
        {
            return new Promise((res, rej) =>
            {
                Thread.Sleep(50);
                rej(new Exception("throws " + i));
            });
        }

        IPromise WillResolve()
        {
            return new Promise((res, rej) =>
            {
                Thread.Sleep(50);
                res();
            });
        }

        IPromise<T> WillReturnVal<T>(T val)
        {
            return new Promise<T>((res, rej) =>
            {
                Thread.Sleep(50);
                res(val);
            });
        }

        IPromise<int> WillReturnInt(int val)
        {
            return new Promise<int>((res, rej) =>
            {
                Thread.Sleep(50);
                if (val == -1) rej(new Exception("deep reject")); //rej(new Exception("can we override this?"));
                else res(val);
            });
        }

        #endregion

        [TestMethod]
        public void TestPromises()
        {
            Assert.ThrowsException<Exception>(() => WillReject(8).Await());
            Assert.IsFalse(WillReject().ToSuccessBoolean().Await());
            Assert.IsTrue(WillResolve().ToSuccessBoolean().Await());
            Assert.AreEqual(5, WillReturnInt(5).Await());

            Assert.AreEqual(2,
                WillResolve()
                    .Then(() => WillReturnInt(1))
                    .Then(one => WillReturnInt(2))
                    .Await());

            Assert.AreEqual(1,
                WillResolve()
                    .Then(() => WillReturnInt(1))
                    .ThenKeepVal(one => WillReturnInt(2))
                    .Await());

            WillReject(4).Catch(e => throw new Exception("OK"))
                .Then(() => WillReject(2))
                .Catch(e => Assert.AreEqual("OK", e.Message))
                .Await();

            WillReject(4).Catch(e => { })
                .Then(() => WillReject(2).Catch(e => throw new Exception("OK")))
                .Catch(e => Assert.AreEqual("OK", e.Message))
                .Await();

            new Promise().Then(() => { }).Reject("message").Catch(e => Assert.AreEqual("message", e.Message)).Await();


            //.Catch(e => Console.WriteLine("fail: " + e.Message));

            //a.Await();
            Thread.Sleep(1000);
        }

        [TestMethod]
        public void TestUpdateJson()
        {
            Debugger.Print(new BridgeFirestoreUser
            {
                elo = 100
            }.GetUpdateJson());

            var u = new FirestoreUser
            {
                username = "theo",
                incomingFriendRequests = new string[2] { "bbbbb", "bbbb" },
                friendRequests = new string[2] { "asdasd", "asd" },
            };

            var db = new BridgeDatabase();

            db.UpdateUserData(
                new FirestoreUser
                {
                    username = "theo"
                },
                new Credential());


            var res =
                "{\"writes\": [{\"update\":{\"name\":\"\",\"fields\": {\"username\":{\"stringValue\":\"theo\"},\"incomingFriendRequests\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"asdasd\"},{\"stringValue\":\"asd\"}]}}}},\"updateMask\": {\"fieldPaths\": [\"username\",\"incomingFriendRequests\"]}}]}";
            Assert.AreEqual(res, u.GetUpdateJson());

            var str =
                "{\"name\":\"\", \"updateTime\":\"\",\"createTime\":\"\", \"fields\":{\"username\":{\"stringValue\":\"theo\"},\"usernameLower\":{\"stringValue\":\"\"},\"img\":{\"stringValue\":\"\"},\"friends\":{\"arrayValue\":{\"values\":[]}},\"chats\":{\"arrayValue\":{\"values\":[]}},\"incomingFriendRequests\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"asdasd\"},{\"stringValue\":\"asd\"}]}},\"outgoingFriendRequests\":{\"arrayValue\":{\"values\":[]}}}}";
            Assert.AreEqual(u.ToString(), str);


            var u2 = new FirestoreUser(u.ToString());
            Assert.AreEqual(u.ToString(), u2.ToString());
            u2.username = "asd";
            var expectedResult =
                "{\"writes\": [{\"update\":{\"name\":\"\",\"fields\": {\"username\":{\"stringValue\":\"asd\"}}},\"updateMask\": {\"fieldPaths\": [\"username\"]}}]}";
            Assert.AreEqual(expectedResult, u2.GetUpdateJson());

            //RENAME friendRequests to incomingFriendRequests
            var str2 =
                "{\"name\":\"\", \"updateTime\":\"\",\"createTime\":\"\", \"fields\":{\"username\":{\"stringValue\":\"theo\"},\"usernameLower\":{\"stringValue\":\"\"},\"img\":{\"stringValue\":\"\"},\"friends\":{\"arrayValue\":{\"values\":[]}},\"chats\":{\"arrayValue\":{\"values\":[]}},\"friendRequests\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"asdasd\"},{\"stringValue\":\"asd\"}]}},\"outgoingFriendRequests\":{\"arrayValue\":{\"values\":[]}}}}";
            var u3 = new FirestoreUser(str2);
            Assert.AreEqual(2, u3.incomingFriendRequests.Length);
            Assert.AreEqual(u3.ToString(),
                "{\"name\":\"\", \"updateTime\":\"\",\"createTime\":\"\", \"fields\":{\"username\":{\"stringValue\":\"theo\"},\"usernameLower\":{\"stringValue\":\"\"},\"img\":{\"stringValue\":\"\"},\"friends\":{\"arrayValue\":{\"values\":[]}},\"chats\":{\"arrayValue\":{\"values\":[]}},\"incomingFriendRequests\":{\"arrayValue\":{\"values\":[{\"stringValue\":\"asdasd\"},{\"stringValue\":\"asd\"}]}},\"outgoingFriendRequests\":{\"arrayValue\":{\"values\":[]}}}}");
        }


        [TestMethod]
        public void TestNestedObjects()
        {
            //checking if rebuilding this object works, having o2 = null breaks this but i checked with a json diff tool and the result is correct but just that all the empty structures has been created as well
            var o = new NestedTestObj
            {
                o2 = new NestedTestObj2(),
                xs1 = new[] { "1", "2", "3" }
            };
            var o2 = new NestedTestObj(o.ToString());
            Debugger.Print(o.ToString());
            Debugger.Print(o2.ToString());

            CollectionAssert.AreEqual(o.xs1, o2.xs1);
            Assert.AreEqual(o.ToString(), o2.ToString());


            var d = new NestedTestDoc
            {
                o = new NestedTestObj
                {
                    xs1 = new[] { "1", "2" },
                    o2 = new NestedTestObj2
                    {
                        xs2 = new[]
                        {
                            new NestedTestObj3
                            {
                                xs3 = new[] { "3", "4" }
                            },
                            new NestedTestObj3
                            {
                                xs3 = new[] { "4", "5" }
                            }
                        },
                    }
                }
            };
            Debugger.Print("FIRST");
            Debugger.Print(d);
            var d2 = new NestedTestDoc(d.ToString());
            Debugger.Print("SECOND");
            Debugger.Print(d2);
            Assert.AreEqual(d.ToString(), d2.ToString());
            CollectionAssert.AreEqual(d.o.xs1, d2.o.xs1);
            CollectionAssert.AreEqual(d.o.o2.xs2[0].xs3, d2.o.o2.xs2[0].xs3);
            //CollectionAssert.AreEqual(d.o.o2.o3.xs3, d2.o.o2.o3.xs3);
        }
    }


    public class NestedTestDoc : FirestoreDocument
    {
        public NestedTestDoc(string json) : base(json)
        {
            TryGetObject("o", ref o);
        }

        public NestedTestDoc()
        {
        }

        public NestedTestObj o;
    }

    public class NestedTestObj : FirestoreObject
    {
        public NestedTestObj(string json) : base(json)
        {
            TryGetObject("o2", ref o2);
            TryGetArray("xs1", ref xs1);
        }

        public NestedTestObj()
        {
        }

        public string[] xs1;
        public NestedTestObj2 o2;
    }

    public class NestedTestObj2 : FirestoreObject
    {
        public NestedTestObj2(string json) : base(json)
        {
            //TryGetObject("o3", ref o3);
            TryGetArray("xs2", ref xs2);
        }

        public NestedTestObj2()
        {
        }

        //array of objects
        public NestedTestObj3[] xs2;
    }

    public class NestedTestObj3 : FirestoreObject
    {
        public NestedTestObj3(string json) : base(json)
        {
            TryGetArray("xs3", ref xs3);
        }

        public NestedTestObj3()
        {
        }

        public string[] xs3;
    }
}
#pragma warning restore CS0618
#pragma warning restore CS0612
