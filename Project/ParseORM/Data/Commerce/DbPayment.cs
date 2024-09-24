using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.ParseORM.Data.Commerce
{
    public class DbPayment : DbObject
    {
        public override string ClassName => "Voucher";

        protected override Dictionary<string, string> KeyMaps => new()
        {
            { "description", "desc" },
        };

        public DbPayment(_VoucherType type, 
            string description = null, 
            string data = null,
            DateTime? validUntil = null, 
            bool singleUse = true
        )
        {
            VoucherType = type;
            if (validUntil != null) ValidUntil = validUntil;
            Description = description;
            SingleUse = SingleUse;
        }

        public string Description {
            get => GetOrElse("");
            set => Set(value);
        }

        public DateTime? ValidUntil
        {
            get => GetOrElse<DateTime?>(null);
            set => Set(value);
        }

        public DbUser UsedBy
        {
            get => Database.Reference<DbUser>(GetOrElse(""));
        }

        public string Data {
            get => GetOrElse("");
            set => Set(value);
        }

        public DateTime? UsedAt
        {
            get => GetOrElse<DateTime?>(null);
        }

        public bool SingleUse {
            get => GetOrElse(true);
            set => Set(value);
        }

        public int NumberUses {
            get => GetOrElse(0);
            // set => Set(value);
        }


        public enum _VoucherType
        {
            Unknown,
            RoomEntry,
            // Premium,

        }

        public _VoucherType VoucherType
        {
            get => GetOrElse(_VoucherType.Unknown);
            set => Set(value);
        }
    }
}