using System.Data;
using Bridgestars.Networking;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM.Data.Room
{
    using Room;
    public class DbCourse : DbObject
    {

        protected override Dictionary<string, string> KeyMaps =>
            new() { { "description", "desc" },{"LengthWeeks", "length_weeks"},{"StartDate","start"}};
        public override string ClassName => "Course";

        // needed for reflection
        private DbCourse() { }

        public DbCourse(DbRoom room, string title, string desc, DateTime? start = null, int lengthWeeks=0, bool free = false, CourseLevel level = CourseLevel.Unknown) {
            Room = room;
            Title = title;
            Description = desc;
            Free = free;
            StartDate = start;
            LengthWeeks = lengthWeeks;
        }
        

        /// <summary>
        /// Course description
        /// </summary>
        public string Description
        {
            get => GetOrElse("");
            set => Set(value);
        }

        /// The room title
        public string Title
        {
            get => GetOrElse("");
            set => Set(value);
        }

        public string Data
        {
            get => GetOrElse("");
            set => Set(value);
        }

        public enum CourseLevel {
            Unknown = 0,
            Beginner = 1,
            Intermediate = 2,
            Advanced = 3
        }

        public CourseLevel Level
        {
            get => GetOrElse(CourseLevel.Unknown);
            set => Set(value);
        }

        public DbRoom Room
        {
            get => Database.Reference<DbRoom>(GetOrElse(""));
            private set => Set(value.Uid);
        }

        public bool Free
        {
            get => GetOrElse(false);
            set => Set(value);
        }
        
        public int Members
        {
            get => GetOrElse(0);
            set => Set(value);
        }
        
        public int LengthWeeks
        {
            get => GetOrElse(0);
            set => Set(value);
        }

        public DateTime? StartDate
        {
            get => GetOrElse<DateTime?>(null);
            set => Set(value);
        }

        private DbPagedQuery<DbUser> _users;
        public DbPagedQuery<DbUser> Users => DbPagedQuery<DbUser>.LazyBuild(ref _users,
          (b) => b
            .WhereEqualTo("courses", this.Uid)
            .DefineOrdering("dispName", (r) => r.DisplayUsername, _ascending: false)
            .OnItemsFetched((items) => { })
          );
          
        private DbPagedQuery<DbChapter> _chapters;
        public DbPagedQuery<DbChapter> Chapters => DbPagedQuery<DbChapter>.LazyBuild(ref _chapters,
          (b) => b
            .WhereEqualTo("course", this.Uid)
            .DefineOrdering("subtype", (r) => r.ChapterNumber, _ascending: true)
            .OnItemsFetched((items) => { })
          );



    }

}
