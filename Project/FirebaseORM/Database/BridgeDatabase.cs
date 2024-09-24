using Project.FirebaseORM.Authentication;
using Project.FirebaseORM.Database.Data;
using Project.Util;
using RSG;

namespace Project.FirebaseORM.Database
{

    [Obsolete("Use Project.Backend.Database instead")]
    public class BridgeDatabase : UserDatabaseWrapper<BridgeFirestoreUser>
    {
        protected override string API_KEY { get => FirebaseCredentials.API_KEY; }
        protected override string API_URL { get => FirebaseCredentials.API_URL;  }
        protected override string dbPath { get => FirebaseCredentials.dbPath; }


        /// <summary>
        /// Prepend matchId to user's matchHistory field
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="matchId"></param>
        /// <param name="userdata"> If available, will save a read </param>
        /// <returns> updated user, (server synced) </returns>
        public IPromise<BridgeFirestoreUser> AddUserMatchHistoryReference(string uid, string matchId, BridgeFirestoreUser userdata = null, int xp = 0)
        {
            return MapServerSideUserdata(userdata, uid, u =>
            {
                u.matchHistory = u.matchHistory.Prepend(matchId);
                u.xp += xp;
                return u;
            });
        }

        /// <summary>
        /// Create or update MatchHistory document in the firestore database, declare data.MatchId or use the optional parameter matchId
        /// </summary>
        /// <exception cref="Exception">If the match does not have a matchId or if all the tables are null</exception>
        public IPromise PostMatchHistoryEntry(BridgeFirestoreMatchHistory data, string matchId = null)
        {
            if (matchId == null) matchId = data.MatchId;
            if (matchId == null) return Rejected("Match does not have an MatchId");
            if (string.IsNullOrEmpty(data.TableOne?.Id) && string.IsNullOrEmpty(data.TableTwo?.Id))
                return Rejected("Both tables are null, this is not a valid match state");
            return PostDocument(data, "matchHistory/" + matchId);
        }

        /// <summary>
        ///  Queries the firestore database for a certain matchHistory object
        /// </summary>
        /// <returns>MatchHistory object containing the matches id and tables</returns>
        public IPromise<BridgeFirestoreMatchHistory> GetMatchHistoryEntry(string matchId)
        {
            return GetDocument<BridgeFirestoreMatchHistory>("matchHistory/" + matchId);
        }

    }
}
