using Project.Util;
using Debugger = Project.Util.Debugger;

namespace Project.FirebaseORM.Database.Data;
[Obsolete("Use new Project.Backend")]
    public abstract class FirestoreDocument : FirestoreObject
    {
         /// <returns>
         /// True if the document path is defined, the path is crucial when posting the document to the firestore database.
         /// </returns>
         public bool HasPath() => !string.IsNullOrEmpty(DocPath);

        /// <returns>The full path of the document</returns>
         public string GetPath() => DocPath;

         /// <summary>
        /// The full path of the document.
        /// </summary>
         [NonSerialized]
         internal string DocPath;
        
         
         /// <summary>
         /// The id/name of the document, the last part of the path.
         /// </summary>
         public string DocId
         {
             get => DocPath?.Split('/')?.Last() ?? "";
             internal set => DocPath = CollectionPath + value;
         }
         
         /// <summary>
         /// The path to the collection which in the document is located.
         /// </summary>
         public string CollectionPath
         {
             get => DocPath?[..(int)DocPath?.LastIndexOf('/')] ?? "";
             internal set => DocPath = value + DocId;
         }
        
        /// <summary>
        /// A string that represents the time of creation of the document on database.
        /// </summary>
        [NonSerialized]
        public readonly string CreateTime="";
        
        /// <summary>
        /// A string that represents the time of last modification of the document on database.
        /// </summary>
        [NonSerialized]
        public readonly string UpdateTime="";
        
        /// <summary>
        /// Create FirestoreDocumentParser from json data string, will try to parse document details and make fields available to <see cref="FirestoreDocument"/> 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        protected FirestoreDocument(string data = null) : base(data)
        {
            if (data == null) return;
            if (!TryGetField("createTime", out CreateTime, Doc))
                throw new ArgumentException(
                    "Provided json string is not a firestore document, could not parse 'createTime'");
            if (!TryGetField("updateTime", out UpdateTime, Doc))
                throw new ArgumentException(
                    "Provided json string is not a firestore document, could not parse 'updateTime'");
            if (!TryGetField("name", out DocPath, Doc))
                throw new ArgumentException(
                    "Provided json string is not a firestore document, could not parse 'name'");
        }

        protected FirestoreDocument() : base() { }

        public bool TryGetOldValue<E>(string name, ref E result) => TryGetValue(name, ref result);


        ///<summary>
        /// Generates update json with field mask. Will only apply changes to fields that have been changed since the document was downloaded.
        /// A <see cref="FirestoreDocument"/> that has not been downloaded from server have nothing to compare to and will overwrite all changes. 
        /// </summary>
        /// <returns> Json string that can be used for updating existing document on the firestore database.</returns>
        public string GetUpdateJson()
        {
            //if (DocPath == null) throw new ArgumentException("Could not get update json since the document path is null. To solve this, update document.DocPath, run `GetCreateJson` or create your User through Firebase.Auth.SignUp, If you are trying to post userdata then you should pass a valid Credential.");
            var fields = this.GetType().GetFields().Where(f => !f.IsNotSerialized);
            var result = new List<string>();
            var propsToUpdate = new List<string>();

            void Add(string name, string json)
            {
                propsToUpdate.Add("\"" + name + "\"");
                result.Add(json);
            }
            
            foreach (var f in fields)
            {
                if(IsFieldUpdated(f.Name)) Add(f.Name, FieldToJson(this, f));
            }

            if (result.Count == 0) return null;

            
            // update obj
            string fieldsJson = "\"fields\": {" + string.Join(",", result) + "}";
            string docRef = "\"name\":\"" + DocPath + "\"";
            string update = "\"update\":{" + docRef + "," + fieldsJson + "}";

            
            // update mask
            string updateMask = "\"updateMask\": {\"fieldPaths\": [" + string.Join(",", propsToUpdate) + "]}";
            
            // build request
            string payload = "{\"writes\": [{" + update + "," + updateMask + "}]}";
            

            return payload;
            
        }

        public override string ToString()
        {
            var fieldsJson = ToFirestoreJson();
            fieldsJson = fieldsJson[1..^1];
            return "{\"name\":\"" + DocPath + "\", \"updateTime\":\"" + UpdateTime + "\",\"createTime\":\"" + CreateTime + "\", "+ fieldsJson +"}";
        }
        
        /// <returns>
        /// Json string that represents all fields on the document. This method only used for creating novel documents on the firestore database with random id generation. 
        /// </returns>
        internal string GetCreateJson() => ToFirestoreJson();

        /// <summary>
        /// Returns true if the value of the attribute with name <b><paramref name="name"/></b> is different from the underlying json data.
        /// Json data is updated when downloading the user from the database.
        /// </summary>
        public bool IsFieldUpdated(string name)
        {
            var fields = this.GetType().GetFields();
           var f = fields.ToList().Find(f => f.Name.Equals(name));
           
           var fieldJson = FieldToJson(this, f);
           
           // Debugger.Print(name);
           // Debugger.Print("Value: " + f.GetValue(this));
           // Debugger.Print("Exists Old: " + FieldExistsOld(name));
           // Debugger.Print("Json diff: " + IsJsonDifferent(name, fieldJson));
           
           return IsJsonDifferent(name, fieldJson);
        }
       
        private bool IsJsonDifferent(string name, string fieldJson)
        {
            return !fieldJson.Equals("\"" + name + "\":" + JsonUtil.ToJson(Fields.ContainsKey(name) ? Fields[name] : ""));
        }

        private bool FieldExistsOld(string name) => Fields.ContainsKey(name);


        #region build from json
       
       /// <summary>
       /// Takes a json string representing an array of firestore documents and parses it. 
       /// </summary>
       /// <param name="collectionJson"></param>
       /// <typeparam name="T"> Subtype of <see cref="FirestoreDocument"/> </typeparam>
       /// <returns> Array of <see cref="FirestoreDocument"/> </returns>
       /// <exception cref="Exception"> If json string doesn't conform to the specified structure. </exception>
       public static T[] BuildCollectionFromJson<T>(string collectionJson) where T : FirestoreDocument
       {
           if (!collectionJson.Contains("documents")) return Array.Empty<T>();
           dynamic docs = JsonUtil.FromJson<dynamic>(collectionJson);//["documents"];
           if (docs == null || docs.Count == 0) throw new Exception("");
           T[] res = new T[docs.Count];
           for (int i = 0; i < docs.Count; i++)
           {
               try
               {
                   res[i] = (T)FirestoreObject.BuildFromJson<T>(
                       JsonUtil.ToJson(docs[i]["document"])); //added ["document"] here instead
               }
               catch (Exception e)
               {
                   // ignored
                   Debugger.Print("Could not parse document, json may be metadata: " + e.Message);
               }
           }
           return res.ToList().FindAll(x => x != default(T)).ToArray();
       }   
       
               
       /// <summary>
       /// Takes a json string representing an firestore document and parses it. 
       /// </summary>
       /// <param name="documentJson"></param>
       /// <typeparam name="T"> Subtype of <see cref="FirestoreDocument"/> </typeparam>
       /// <returns> Array of <see cref="FirestoreDocument"/> </returns>
       /// <exception cref="Exception"> If json string doesn't conform to the specified structure. </exception>
       public new static T BuildFromJson<T>(string documentJson) where T : FirestoreDocument =>
           (T) Activator.CreateInstance(typeof(T), documentJson);
       
       #endregion 
    }