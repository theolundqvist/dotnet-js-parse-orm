using Project.FirebaseORM.Database.Data;
using Project.Util;

namespace Project.FirebaseORM.Database
{
    using _ = OldData;
    [Obsolete("Use new Project.Backend")]
    public class Query
    {
        public enum OPERATOR
        {
            EQUAL,
            LESS_THAN,
            LESS_THAN_OR_EQUAL,
            GREATER_THAN,
            GREATER_THAN_OR_EQUAL,
            NOT_EQUAL,
            ARRAY_CONTAINS,
            IN,
            ARRAY_CONTAINS_ANY,
            NOT_IN,
        }
        public enum ORDERING
        {
            ASCENDING,
            DESCENDING
        }
        internal class FieldReference
        {
            public string fieldPath;
            public FieldReference(string path) => fieldPath = path;
        }

        internal class CompositeFilter
        {
            public enum OP { AND }
            public OP op = OP.AND;
            public FieldFilter[] filters = new FieldFilter[0] { };
        }

        internal class FieldFilter
        {
            internal class FieldFilterInternal
            {
                public FieldReference field;
                public OPERATOR op;
                public object value; //THIS BEING OBJECT MAKES THE SERIALIZER ABLE TO READ PROP OF DERIVED TYPE
            }
            public FieldFilterInternal fieldFilter;
            public FieldFilter(string path, OPERATOR op, _.FirestoreValue value)
            {
                fieldFilter = new FieldFilterInternal();
                fieldFilter.field = new FieldReference(path);
                fieldFilter.op = op;
                fieldFilter.value = value;
            }
        }
        internal class Order
        {
            public FieldReference field;
            public ORDERING direction = ORDERING.ASCENDING;
            public Order(string path, ORDERING direction)
            {
                field = new FieldReference(path);
                this.direction = direction;
            }
        }
        internal class CollectionSelector
        {
            public string collectionId;
            public bool allDescendants = false;
            public CollectionSelector(string collectionId, bool allDescendants)
            {
                this.collectionId = collectionId;
                this.allDescendants = allDescendants;
            }
        }

        internal class Projection
        {
            public FieldReference[] fields;
            public Projection(string[] fields)
            {
                this.fields = new FieldReference[] { };
                foreach (var field in fields)
                {
                    this.fields = this.fields.Concat(new FieldReference[] { new FieldReference(field) }).ToArray();
                }
            }
        }

        internal class Where
        {
            CompositeFilter compositeFilter = new CompositeFilter();
            public void AddFilter(FieldFilter filter)
            {
                compositeFilter.filters = compositeFilter.filters.Concat(new FieldFilter[] { filter }).ToArray();
            }
        }

        internal class Cursor
        {
            public object[] values;
            public bool before;

            public Cursor(_.FirestoreValue[] values, bool before)
            {
                this.values = values;
                this.before = before;
            }
        }

        internal Projection select = null;
        internal CollectionSelector[] from = null;
        internal Where where = null;
        internal Order[] orderBy = null;
        internal Cursor startAt = null;
        internal Cursor endAt = null;
        internal int offset;
        internal int limit = 10;

        public Query() { }


        /**
         * Select fields to return, otherwise return all
         */
        public Query SelectFields(params string[] fields)
        {
            select = new Projection(fields);
            return this;
        }

        /**
         * Select collection to query, can select many
         */
        public Query SelectCollection(string collectionId, bool allDescendants = false)
        {
            if (from == null) from = new CollectionSelector[] { };
            from = from.Concat(new[] { new CollectionSelector(collectionId, allDescendants) }).ToArray();
            return this;
        }


        /**
         * Specify an order to use, ORDERS.ASCEDNING or ORDERS.DESCENDING
         */
        public Query OrderBy(string fieldPath, ORDERING direction = ORDERING.ASCENDING)
        {
            if (orderBy == null) orderBy = new Order[] { };
            orderBy = orderBy.Concat(new[] { new Order(fieldPath, direction) }).ToArray();
            return this;
        }

        /**
         * Add a fieldFilter. Select documents that satisfy the filter criteria that "fieldName op value == true"
         * Can select many filters
         */
        public Query AddFieldFilter(string fieldName, OPERATOR op, _.FirestoreValue value)
        {
            if (where == null) where = new Where();
            where.AddFilter(new FieldFilter(fieldName, op, value));
            return this;
        }

        /**
         * How many documents to select
         */
        public Query SetReadLimit(int limit)
        {
            this.limit = limit;
            return this;
        }
        /**
         * How many documents to skip before selecting
         * All skipped documents counts as reads!!
         */
        public Query SetStartOffset(int offset)
        {
            this.offset = offset;
            return this;
        }

        public Query StartAt(params _.FirestoreValue[] values)
        {
            this.startAt = new Cursor(values, true);
            return this;
        }
        
        public Query StartAfter(params _.FirestoreValue[] values)
        {
            this.startAt = new Cursor(values, false);
            return this;
        }

        public Query EndAt(params _.FirestoreValue[] values)
        {
            this.endAt = new Cursor(values, true);
            return this;
        }

        internal string GetPayload()
        {
            JsonUtil.AllowPrivateExcludeNull();
            return JsonUtil.Prettify("{\"structuredQuery\":" + JsonUtil.ToJson(this) + "}");
        }
    }
}
