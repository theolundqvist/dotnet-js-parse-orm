using Project.ParseORM.Data.Room;

namespace Project.ParseORM.Data.Commerce;

public class DbRoomEntryVoucher : DbVoucher
{
    //override query limiter so that type == 1
    protected override QueryLimiter? SubTypeLimiter =>
        new QueryLimiter() { Key = "type", Value = (int)DbVoucher._VoucherType.RoomEntry };
    protected override Dictionary<string, string> KeyMaps => new()
    {
        { "room", "data" }
    };

    private DbRoomEntryVoucher() { }
    public DbRoomEntryVoucher(DbRoom room,
        string description = null,
        DateTime? validUntil = null
    )
        : base(description, validUntil)
    {
        if (room.IsNew) throw new Exception("Room must be saved before creating a voucher for it.");
        Room = room;
        VoucherType = _VoucherType.RoomEntry;
    }

    public DbRoom Room
    {
        get => Database.Reference<DbRoom>(GetOrElse(""));
        set => Set(value.Uid);
    }
}