using Project.ParseORM.Data;
using Project.Util;
using Parse;
using RSG;

namespace Project.ParseORM;

public static class TaskExtensions{
    
    public static IPromise<T> ToPromise<T>(this Task<T> t)
    {
        var p = new Promise<T>();
        AwaitTask(t, p);
        return p;
    }
    
    public static IPromise<T[]> ToPromise<T>(this Task<IEnumerable<ParseObject>> t) where T : DbObject
    {
        var p = new Promise<IEnumerable<ParseObject>>();
        AwaitTask(t, p);
        return p.Then(x => x.Select(DbObject.Instantiate<T>).ToArray());
    }
    
    public static IPromise<T[]> ToPromise<T>(this Task<IEnumerable<ParseUser>> t) where T : DbObject
    {
        var p = new Promise<IEnumerable<ParseUser>>();
        AwaitTask(t, p);
        return p.Then(x => x.Select(DbObject.Instantiate<T>).ToArray());
    }
    

    public static IPromise<T> ToPromise<T>(this Task<ParseUser> t) where T : DbObject
    {
        var p = new Promise<ParseUser>();
        AwaitTask(t, p);
        return p.Then(DbObject.Instantiate<T>);
    }

    public static IPromise<T> ToPromise<T>(this Task<ParseObject> t) where T : DbObject
    {
        var p = new Promise<ParseObject>();
        AwaitTask(t, p);
        return p.Then(DbObject.Instantiate<T>);
    }


    private static async void AwaitTask<T>(Task<T> t, Promise<T> p)
    {
        try
        {
            T res = await t;
            p.Resolve(res);
        }
        catch (Exception e)
        {
            p.Reject(e);
        }
        
    }
    public static IPromise ToPromise(this Task t)
    {
        var p = new Promise();
        AwaitTask(t, p);
        return p;
    }

    private static async void AwaitTask(Task t, IPromise p)
    {
        try
        {
            await t;
            p.Resolve();
        }
        catch (Exception e)
        {
            p.Reject(e);
        }
        
    }
    
}