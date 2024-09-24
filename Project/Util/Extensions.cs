using System;
using System.Linq;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Threading;
using RSG;

namespace Project.Util
{

    public static class TypeExtensions
    {
        public static bool IsNumeric(this Type type) =>
            type.Name switch
            {
                "Int32" => true, //All firestore ints are Int64
                "Int64" => true,
                "Single" => true, // There is no floatValue in Firestore
                "Double" => true,
                _ => false
            };

        public static bool IsString(this Type type) =>
            type.Name switch
            {
                "String" => true,
                _ => false
            };
        public static bool IsBool(this Type type) =>
            type.Name switch
            {
                "Boolean" => true,
                _ => false
            };
    }
    
    public static class Extensions
    {


        #region arrays
        
        public static T[] Append<T>(this T[] xs, T x)
        {
            if (xs == null) return new T[1] {x};
            if (xs.Contains(x)) return xs;
            return xs.Concat(new T[] { x }).ToArray();
        }
        public static T[] Prepend<T>(this T[] xs, T x)
        {
            if (xs == null) return new T[1] {x};
            if (xs.Contains(x)) return xs;
            return xs.ToList().Prepend(x).ToArray();
        }

        public static T[] Shuffle<T>(this T[] items)
        {
            Random rand = new();

            for (int i = 0; i < items.Length - 1; i++)
            {
                int j = rand.Next(i, items.Length);
                (items[j], items[i]) = (items[i], items[j]);
            }
            return items;
        }

        #endregion




        #region Exception

        public static Exception PrependMessage(this Exception e, string str)
        {
            return new Exception(str + e.Message, e);
        }

        #endregion

        #region IPromise

        
        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved. Returns a promise of a collection of the resolved results.
        /// </summary>
        [Obsolete("Sometimes does not resolve nor reject")] 
        public static IPromise<T[]> MergeWith<T>(this IPromise<T> p, IPromise<T> other)
        {
            return Promise<T>.All(p, other).Then(e => e.ToArray());
        }
        
        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// </summary>
        public static IPromise MergeWith(this IPromise p, IPromise other)
        {
            return Promise.All(new[] {p, other});
        }
        
        

        #region catch and rethrow (CatchThrow)

        
        /// <summary>
        /// Print error message using Debugger.Print()
        /// </summary>
        public static IPromise<T> PrintErrorMessage<T>(this IPromise<T> p, string message="") =>
            p.Catch(e =>
            {
                Debugger.Print(message + e.Message);
                throw e;
            });

        /// <summary>
        /// Print error message using Debugger.Print()
        /// </summary>
        public static IPromise PrintErrorMessage(this IPromise p, string message="") =>
            p.Catch(e =>
            {
                Debugger.Print(message + e.Message);
                throw e;
            });


        /// <summary>
        /// Catch and throw again with exception being wrapped by new Exception with old exception message prepended by message: <b><paramref name="message"/></b>.<br/>
        /// </summary>
        public static IPromise<T> CatchWrap<T>(this IPromise<T> p, string message)
        {
            return p.Catch(e => throw new Exception(message + e.Message, e));
        }
        
        /// <summary>
        /// Catch and throw again with exception being wrapped by new Exception with old exception message prepended by message: <b><paramref name="message"/></b>.<br/>
        /// </summary>
        public static IPromise CatchWrap(this IPromise p, string message)
        {
            return p.Catch(e => throw new Exception(message + e.Message, e));
        }
        
        /// <summary>
        /// Catch and throw with other message, doesn't keep innerException.
        /// </summary>
        public static IPromise<T> CatchThrow<T>(this IPromise<T> p, string message)
        {
            return p.CatchThrow(new Exception(message));
        }
        /// <summary>
        /// Catch and throw with other message, doesn't keep innerException.
        /// </summary>
        public static IPromise<T> CatchThrow<T>(this IPromise<T> p, Exception e)
        {
            return p.Catch(ex  => throw e);
        }
        /// <summary>
        /// Catch and throw with other message, doesn't keep innerException.
        /// </summary>
        public static IPromise CatchThrow(this IPromise p, string message)
        {
            return p.CatchThrow(new Exception(message));
        }
        /// <summary>
        /// Catch and throw with other message, doesn't keep innerException.
        /// </summary>
        public static IPromise CatchThrow(this IPromise p, Exception e)
        {
            return p.Catch(dontCare  => throw e);
        }
        
        
        /// <summary>
        /// Do something before promise when promise rejects
        /// </summary>
        public static IPromise<T> OnCatch<T>(this IPromise<T> p, Action<Exception> a)
        {
            return p.Catch(ex => { 
                a(ex);
                throw new Exception(ex.Message,ex.InnerException);
            });
        }
        /// <summary>
        /// Do something before promise when promise rejects
        /// </summary>
        public static IPromise OnCatch(this IPromise p, Action<Exception> a)
        {
            return p.Catch(ex => { 
                a(ex);
                throw new Exception(ex.Message, ex.InnerException);
            });
        }
        #endregion
        
        
        #region reject and resolve

        public static IPromise Resolve(this IPromise p)
        {
            ((Promise) p).Resolve();
            return p;
        }
        
        public static IPromise Resolve<T>(this IPromise<T> p) => ((Promise<T>) p).Empty().Resolve();

        public static IPromise<T> Reject<T>(this Promise<T> p, string message)
        {
            p.Reject(new Exception(message));
            return p;
        }
        public static IPromise Reject(this Promise p, Exception e)
        {
            p.Reject(e);
            return p;
        }
        
        public static IPromise<T> Reject<T>(this IPromise<T> p, string message) => p.Reject(new Exception(message));
        public static IPromise<T> Reject<T>(this IPromise<T> p, Exception e)
        {
            ((Promise<T>) p).Reject(e);
            return p;
        }
        public static IPromise Reject(this IPromise p, string message) => p.Reject(new Exception(message));
        public static IPromise Reject(this IPromise p, Exception e) 
        {
            ((Promise) p).Reject(e);
            return p;
        }
        #endregion
        
        /// <summary>
        /// Awaits promise and returns result or throws
        /// </summary>
        /// <param name="timeout"> Promise timeout in ms </param>
        /// <returns> T - if promise is resolved </returns>
        /// <exception cref="Exception"> if promise is rejected or time limit reached </exception>
        public static T Await<T>(this IPromise<T> p, int timeout = 10000)
        {
            var quit = false;
            Exception exception = null;
            ExceptionDispatchInfo exInfo = null;
            T result = default(T);
            p.Then(t =>
            {
                quit = true;
                result = t;
            });
            p.Catch(e =>
            {
                exInfo = ExceptionDispatchInfo.Capture(e);
                quit = true;
            });
            
            var start = DateTime.Now;
            while (!quit)
            {
                p.Progress(a => Debugger.Print(a));
                Thread.Sleep(1);
                if ((DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    throw new Exception("Promise timed out");
                }
            }

            if (exInfo != null) exInfo.Throw();
            return result;
        }

        /// <summary>
        /// Awaits empty promise resolution, throws if promise is rejected, blocks thread
        /// </summary>
        /// <param name="timeout"> Promise timeout in ms </param>
        /// <exception cref="Exception"> if promise is rejected or time limit reached </exception>

        public static void Await(this IPromise p, int timeout = 10000)
        {
            p.Then(() => true).Await(timeout);
        }
        
        /// <summary>
        /// Awaits empty promise resolution, discards exceptions 
        /// </summary>
        /// <param name="timeout"> Promise timeout in ms </param>
        public static void AwaitCatch(this IPromise p, int timeout = 10000)
        {
            p.Catch(e => {}).Await(timeout);
        }
        
        /// <summary>
        /// Awaits promise resolution, discards exceptions and in that case returns default(T) 
        /// </summary>
        /// <param name="timeout"> Promise timeout in ms </param>
        public static T AwaitCatch<T>(this IPromise<T> p, int timeout = 10000)
        {
            return p.Catch(e => default(T)).Await(timeout);
        }

        /// <summary>
        /// Add a resolved callback that chains a value promise (optionally converting to a different value type).
        /// </summary>
        public static IPromise<T> Then<T>(this IPromise p, Func<T> a)
        {
            return new Promise<T>((res, rej) => { p.Then(() => res(a())).Catch(rej); });
        }

        public static IPromise<T> Catch<T>(this IPromise<T> p)
        {
            return new Promise<T>((res, rej) => { p.Then(res).Catch(e =>
            {
                //a()
            }); });
            
        }
        
        /// <summary>
        /// Keeps value of last Promise and ignores value of provided lambda.
        /// ex.<br/><br/>
        /// p.Then(() => 1)<br />
        /// .Then(one => 2)<br />
        /// .Then(two => 3)<br />
        /// .ThenKeepPrev(three => 4)<br />
        /// .Then(three => 5)<br /><br/>
        /// The value 4 is ignored.
        /// </summary>
        public static IPromise<T> ThenKeepVal<T>(this IPromise<T> p, Action<T> a)
        {
            return p.Then(val => { 
                a(val);
                return val;
            });
        }
        
        /// <summary>
        /// Keeps value of last Promise and ignores value of provided lambda.
        /// ex.<br/><br/>
        /// p.Then(() => 1)<br />
        /// .Then(one => 2)<br />
        /// .Then(two => 3)<br />
        /// .ThenKeepPrev(three => 4)<br />
        /// .Then(three => 5)<br /><br/>
        /// The value 4 is ignored.
        /// </summary>
        public static IPromise<T> ThenKeepVal<T>(this IPromise<T> p, Func<T, IPromise> f)
        {
            return p.Then(val => { return f(val).Then(() => val); });
        }

        /// <summary>
        /// Keeps value of last Promise and ignores value of provided lambda.
        /// ex.<br/><br/>
        /// p.Then(() => 1)<br />
        /// .Then(one => 2)<br />
        /// .Then(two => 3)<br />
        /// .ThenKeepPrev(three => 4)<br />
        /// .Then(three => 5)<br /><br/>
        /// The value 4 is ignored.
        /// </summary>
        public static IPromise<T> ThenKeepVal<T, TG>(this IPromise<T> p, Func<T, IPromise<TG>> f)
        {
            return p.Then(val => { return f(val).Then(tg => val).Catch(e => throw e); });
        }

        /// <summary>
        /// Removes inner object of IPromise
        /// </summary>
        /// <param name="p"> promise </param>
        /// <typeparam name="T"> Inner type </typeparam>
        public static IPromise Empty<T>(this IPromise<T> p) => p.Then(t => { });

        /// <summary>
        /// Returns a new Promise[bool]. The new promise always resolves either true or false, never rejects.
        /// </summary>
        public static IPromise<bool> ToSuccessBoolean<T>(this IPromise<T> p) => p.Empty().ToSuccessBoolean();

        /// <summary>
        /// Returns a new Promise[bool]. The new promise always resolves either true or false, never rejects.
        /// </summary>
        public static IPromise<bool> ToSuccessBoolean(this IPromise p)
        {
            return new Promise<bool>((res, rej) => { p.Then(() => res(true)).Catch(e => res(false)); });
        }

        
        #endregion

        
        public static IPromise<T> Rejected<T>(this IPromise<T> p, string message)
        {
            p.Reject(new Exception(message));
            return p;
        }
        public static IPromise Rejected(this IPromise p, string message)
        {
            p.Reject(new Exception(message));
            return p;
        }
    }
}