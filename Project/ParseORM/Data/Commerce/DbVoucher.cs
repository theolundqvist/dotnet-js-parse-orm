using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.ParseORM.Data.Commerce
{
    public class DbVoucher : DbObject
    {
        public override string ClassName => "Voucher";

        protected override Dictionary<string, string> KeyMaps { get; }

        private DbVoucher() { }
        public DbVoucher(
            string description = null,
            DateTime? validUntil = null,
            int maxUses = 1,
            _VoucherType type = _VoucherType.Unknown
        )
        {
            this.AddRemoteKey("Description", "desc");
            this.AddRemoteKey("VoucherType", "type");
            if (validUntil != null) ValidUntil = validUntil;
            Description = description;
            MaxUses = maxUses;
            VoucherType = type;
        }


        public static RSG.IPromise<Database.VoucherResult> Redeem(string code) =>
            Database.RedeemVoucherCodeAsync(code);



        /// <summary> The code to redeem to activate the voucher, same as Uid.</summary>
        public string RedemptionCode
        {
            get => GetOrElse<string>(null, "customCode") == null
                ? Uid :
                GetOrElse<string>(null, "customCode");
            set => Set(value, "customCode");
        }

        public List<DbUser> PendingUses =>
            GetArrayOrElse("pending", new string[0])
            .Select(x => Database.Reference<DbUser>(x)).ToList();
        public int MaxUses
        {
            get => GetOrElse(0);
            set => Set(value);
        }

        public bool? Enabled
        {
            get => GetOrElse<bool?>(null);
            set => Set(value);
        }

        public string Description
        {
            get => GetOrElse("");
            set => Set(value);
        }

        public DateTime? ValidUntil
        {
            get => GetOrElse<DateTime?>(null);
            set => Set(value);
        }

        public List<DateTime> UsedAt => GetArrayOrElse("UsedAt", new long[0]).Select(x => DateTime.UnixEpoch.AddMilliseconds(x)).ToList();

        public List<DbUser> UsedBy
        {
            get => GetArrayOrElse("UsedBy", new string[0]).Select(x => Database.Reference<DbUser>(x)).ToList();
        }

        public int Uses
        {
            get => GetOrElse(0);
            // set => Set(value);
        }


        public enum _VoucherType
        {
            Unknown = 0,
            RoomEntry = 1,
            CourseEntry = 2,
            // Premium,

        }

        public _VoucherType VoucherType
        {
            get => GetOrElse(_VoucherType.Unknown);
            set => Set(value);
        }

        public bool Paid
        {
            get => GetOrElse(false);
            set => Set(value);
        }
    }
}