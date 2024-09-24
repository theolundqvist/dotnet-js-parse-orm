using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Project.ParseORM;
using Project.ParseORM.Data;
using Project.ParseORM.Data.Commerce;
using Project.ParseORM.Data.Room;
using Project.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Test.Util;
using Admin = Test.Util.Admin;
using Debugger = Project.Util.Debugger;
using Tester = Test.Util.Tester;

namespace Test
{
    [TestClass]
    public class DatabaseManualTasks
    {
        [TestInitialize]
        public void Init()
        {
            Util.TestInit(dev:false);
        }
        
        [TestMethod]
        public void ManualTestRedeemVoucher(){
            Util.DeleteAllTestObjs();

            Util.Admin.SignIn();
            //Create room and course
            var r = new DbRoom("room1", "BS_ROOM_TEST").SaveAsync().Await();
            var c = new DbCourse(r, "course1", "BS_COURSE_TEST").SaveAsync().Await();
            var chap = new DbChapter(r, c, 1, "chapter3", "BS_CHAPTER_TEST").SaveAsync().Await();
            var voucher = new DbCourseEntryVoucher(c, "BS_VOUCHER_TEST").SaveAsync().Await();
            var u = Tester.SignUp(1);
            var res = Database.RedeemVoucherCodeAsync(voucher.RedemptionCode).Await();
            Assert.IsTrue(res.IsPaymentRequired);
            Debugger.Print(res.PaymentLink);
            var course = Database.GetObjectAsync<DbCourse>(res.Data).Await();
            Debugger.Print(course.Title);
        }

        [TestMethod]
        public void ManualTestDashboard(){
            //var u = Tester.SignIn(1);
            var u = Database.SignInAsync("theo2", "asdASD123").Await();
            var url = Database.GenerateSubscriptionDashboardLinkAsync().Await();
            Debugger.Print(url);
        }

        [TestMethod]
        public void ManualExitCourseAndCleanUpSubscriptionTest()
        {
            var uname = "theo";
            Admin.SignIn();
            var u = Database.GetQuery<DbUser>().WhereEqualTo("username", uname).FirstAsync().Await();
            Database.CallCloudFunctionAsync<string>("test_cancelAndExitAllCourses", 
                new Dictionary<string, object>{{"user", u.Uid}}).Await();


        }
        
        [TestMethod]
        public void ManualCreateVoucher()
        {
            Admin.SignIn();
            var courseName = "Testkurs 1";
            var voucherCount = 5;
            var courses = Database.GetQuery<DbCourse>().WhereEqualTo("title", courseName).FindAsync().Await();
            foreach (var dbCourse in courses)
            {
               Debugger.Print(dbCourse.Title); 
            }

            var vouchers = new List<DbVoucher>();
            for (int i = 0; i < voucherCount; i++)
            {
                var voucher = new DbCourseEntryVoucher(courses[0], "Välkommen till testkurs 1",
                    DateTime.Now.AddDays(30));
                voucher.MaxUses = 1;
                voucher.Paid = true;
                //voucher.RedemptionCode = "test3"; //temp we want random code
                voucher.SaveAsync().Await();
                vouchers.Add(voucher);
            }
            
            Debugger.PrintJson(vouchers.Select(x => x.RedemptionCode));
        }

        [TestMethod]
        public void ManualResetVoucher()
        {
            Admin.SignIn();
            var voucherCode = "";
            Database.CallCloudFunctionAsync<string>("resetVoucher", new Dictionary<string, object>{{"code", voucherCode}}).Await();
        }
        
        [TestMethod]
        public void ManualCreateCourse()
        {
            var room = "Testrum 1";
            var start = DateTime.Now; //course start date
            var length = 10; //weeks

            var name = "Testkurs 1";
            var desc = "Denna kurs innehåller övningar om X och Y.";
            var level = DbCourse.CourseLevel.Beginner;
            
            Admin.SignIn();
            var rooms = Database.GetQuery<DbRoom>().WhereEqualTo("name",  room).FindAsync().Await();
            Assert.AreEqual(1, rooms.Length);
            var course = new DbCourse(rooms[0], name, desc, level: level, start:start, lengthWeeks:length);
            course.SaveAsync().Await();
        }
    }
}