using RSG;

namespace Project.ParseORM.Data;

public abstract class DbReactionable : DbObject
{
    protected abstract DbReaction.TargetType ReactionTargetType { get; }    

    /// Runs a query to get the votes of the signed in user and her friends.
    public IPromise<List<DbReaction>> GetFriendsReactionsAsync() =>
        Database.GetQuery<DbReaction>()
            .WhereEqualTo("type", ReactionTargetType)
            .WhereEqualTo("problem", this)
            .WhereContainedIn("user", Database.GetCurrentUser().Friends.Append(Database.GetCurrentUser()))
            .FindAsync()
            .Then(a => a.ToList());


    /// <summary>
    /// The vote frequency table, where the key is the vote data and the value is the frequency.
    /// </summary>
    public Dictionary<DbReaction.ReactionType, int> Reactions => GetDictionaryOrEmpty("reactions")
        .ToDictionary(a => (DbReaction.ReactionType)int.Parse(a.Key), a => int.Parse(a.Value.ToString()));

    /// <summary>
    /// <inheritdoc cref="Reactions"/>
    /// </summary>
    /// note: this method will not cast to (ReactionType), <see cref="Reactions"/> is okay to use as well.
    public Dictionary<int, int> GetReactionsWithCustomData => GetDictionaryOrEmpty("reactions")
        .ToDictionary(a => int.Parse(a.Key), a => int.Parse(a.Value.ToString()));



    /// <summary>
    /// Will add a reaction to the post. If the user has already reacted, the reaction will be updated.
    /// Does not update the frequency table. Pls either fetch the Post object or just add the vote to the frequency local UI.
    /// </summary>
    public IPromise<DbReaction> AddReactionAsync(DbReaction.ReactionType reaction) =>
        DbReaction.Create(Uid, ReactionTargetType, reaction).SaveAsync();


    /// <summary>
    /// Only difference from <see cref="AddReactionAsync(DbReaction.ReactionType)"/> is that data is casted to (ReactionType) for convenience. 
    /// </summary>
    public IPromise<DbReaction> AddReactionAsync(int data) =>
        DbReaction.Create(Uid, ReactionTargetType, (DbReaction.ReactionType) data).SaveAsync();



    /// <summary>
    /// Will find the reaction cast by the currently signed in user and remove it.
    /// Does not update the frequency table. Pls either fetch the Post object or just add the vote to the frequency local UI.
    /// </summary>
    public IPromise RemoveReactionAsync() =>
        Database.GetQuery<DbReaction>()
            .WhereEqualTo("problem", this)
            .WhereEqualTo("type", ReactionTargetType)
            .WhereEqualTo("user", Database.GetCurrentUser())
            .FindAsync()
            .Then(reactions =>
                reactions.DeleteObjectsAsync()); // delete all reactions for this user, should only be one
}