// using System.Runtime.Serialization;
// using Project.Backend.Data;
// using Parse;
// using Project.Util;
// using RSG;
//
// namespace Project.Backend;
//
// public class DbRelation<T> where T : DbObject
// {
//     private ParseRelation<ParseObject> _objRel;
//     private ParseRelation<ParseUser> _userRel;
//     private bool ReadOnly = false;
//     private bool IsUser => typeof(T) == typeof(DbUser);
//
//     internal DbRelation(ParseRelation<ParseUser> userRel, bool readOnly = false)
//     {
//         if(IsUser) _userRel = userRel;
//         else throw new Exception("This relation can only be created for DbUser");
//         ReadOnly = readOnly;
//     }
//     
//     internal DbRelation(ParseRelation<ParseObject> objRel, bool readOnly = false) {
//         if(!IsUser) _objRel = objRel;
//         else throw new Exception("This relation can not be created for DbUser");
//         ReadOnly = readOnly;
//     }
//     
//     /// <summary>
//     /// Add an object to this relation, save the object containing this relation to save the change.
//     /// </summary>
//     /// <remarks>If the object is not saved, running this function will first save it, this is an requirement since we can't point to a non existing object. Calling this function should always be followed up by running SaveAsync on the object containing this relation so that the newly created objects aren't floating freely.</remarks>
//     public IPromise AddAsync(params T[] objects)
//     {
//         if(ReadOnly) return new Promise().Rejected("Relation is readonly");
//         List<T> unsavedObjects = new();
//         foreach (var o in objects)
//         {
//             if (IsUser) Add(o); 
//             else
//             {
//                 if (o.IsNew)
//                 {
//                     unsavedObjects.Add(o);
//                 }
//                 else Add(o);
//             }
//         }
//
//         if (unsavedObjects.Count > 0)
//         {
//             return Database.SaveObjectsAsync(unsavedObjects.ToArray())
//                 .Then(saved => unsavedObjects.ForEach(x => Add(x)));
//         }
//         return Promise.Resolved();
//     }
//
//     /// <summary>
//     /// Add an object to this relation, save the object containing this relation to save the change.
//     /// </summary>
//     /// <remarks>Use this overload only if you are sure that all of the objects already exists on the remote.</remarks>
//     public void Add(params T[] objects)
//     {
//         if(ReadOnly) throw new Exception("Relation is readonly");
//         foreach (var o in objects)
//         {
//             if (IsUser) _userRel.Add((ParseUser)o.Underlying);
//             else _objRel.Add(o.Underlying);
//         }
//     }
//     
//     /// <summary>
//     /// Remove an object from this relation, save the object containing this relation to save the change
//     /// </summary>
//     public void Remove(params T[] objects) {
//         if(ReadOnly) throw new Exception("Relation is readonly");
//         foreach (var o in objects)
//         {
//             if (IsUser) _userRel.Remove((ParseUser)o.Underlying);
//             else _objRel.Remove(o.Underlying);
//         }
//     }
//
//     
//     /// <summary>
//     /// Query the objects specified by this relation
//     /// </summary>
//     [IgnoreDataMember]
//     public DbQuery<T> DbQuery => IsUser ? new DbQuery<T>(_userRel.Query) : new DbQuery<T>(_objRel.Query);
//
//     /// <summary>
//     /// Retrieve all objects specified by this relation
//     /// </summary>
//     public IPromise<T[]> GetAllAsync(params string[] fieldsToInclude) =>  DbQuery.SelectMultiple(fieldsToInclude).FindAsync();
// }