using System;
using System.Collections.Generic;
using System.Linq;
using Project.ParseORM;
using Project.ParseORM.Data;
using Project.Util;

using DB = Project.ParseORM.Database;

namespace Test
{
    public class Util
    {
        public class Tester
        {
            public string Username;
            public string Email;
            public string Password = "asdASD123";
            private readonly int _nbr;
            private DbUser _user;
            private static readonly List<Tester> Instances = new();
            private static Tester ByNbr(int nbr) => Instances.Find(x => x._nbr == nbr);

            public static string Uid(int nbr) => Instances.Find(x => x._nbr == nbr)?._user.Uid;

            public static DbUser SignUp() => SignUp(new Random().Next(0, 1000));
            public static DbUser SignUp(int nbr)
            {
                var a = new Tester(nbr);
                return DB.SignUpAsync(a.Username, a.Email, a.Password).ThenKeepVal(u => a._user = u).Await();
            }

            public static DbUser SignIn(int nbr)
            {
                var instance = ByNbr(nbr) ?? new Tester(nbr);
                return DB.SignInAsync(instance.Email, instance.Password).ThenKeepVal(u => instance._user = u).Await();
            }

            //constructor for creating new testers
            private Tester(int nbr)
            {
                this._nbr = nbr;
                Username = "BS_TESTER" + nbr;
                Email = "BS_TESTER" + nbr + "@bridgestars.net";
                Instances.Add(this);
            }

            public static void DeleteInstance(int nbr)
            {
                try
                {
                    var t = ByNbr(nbr) ?? new Tester(nbr);
                    var c = DB.SignInAsync(t.Username, t.Password).AwaitCatch();
                    c.DeleteAsync().AwaitCatch();
                    Debugger.Print("Deleted user: [User:" + c.Uid + ":" + t.Username + "]");
                    Instances.Remove(t);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            public static void DeleteAllInstances()
            {
                if (Instances.Count <= 0) return;
                foreach (var t in Instances)
                {
                    // DB.SignInAsync(t.Email, t.Password).Await();
                    Admin.SignIn();
                    t._user.DeleteAsync().Await();
                }
                Debugger.Print("[" + string.Join(", ", Instances.Select(x => "User:" + x._user.Uid + ":" + x.Username)) + "]");
                Instances.Clear();
            }
        }

        public static class Admin
        {
            static internal void SignIn() => DB.SignInAsync("admin", "").Empty().Await();
        }

        public static void TestInit(bool dev = true)
        {
            Debugger.Print("DB Connected");
            DB.Connect(dev: dev);
            DB.EnableLocalCache(true);
            
            
        }

        public static void CleanUp()
        {
            Debugger.Print("--- Cleaning Up ---");
        }

        public static void DeleteAllTestObjs()
        {
            Admin.SignIn();
            Database.CallCloudFunctionAsync<string>("removeAllTestObjects").Await();
        }
    }
}