using Project.ParseORM.Data.Room;

namespace Project.ParseORM.Data.Commerce;

public class DbCourseEntryVoucher : DbVoucher
{
    //override query limiter so that type == 1
    protected override QueryLimiter? SubTypeLimiter =>
        new QueryLimiter() { Key = "type", Value = (int)DbVoucher._VoucherType.CourseEntry };
    protected override Dictionary<string, string> KeyMaps => new()
    {
        { "course", "data" }
    };
    private DbCourseEntryVoucher(){}
    public DbCourseEntryVoucher(DbCourse course,
        string description = null,
        DateTime? validUntil = null
    )
        : base(description, validUntil)
    {
        if (course.IsNew) throw new Exception("Course must be saved before creating a voucher for it.");
        Course = course;
        VoucherType = _VoucherType.CourseEntry;
    }

    public DbCourse Course
    {
        get => Database.Reference<DbCourse>(GetOrElse(""));
        set => Set(value.Uid);
    }
}