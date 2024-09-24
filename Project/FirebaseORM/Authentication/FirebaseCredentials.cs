namespace Project.FirebaseORM.Authentication
{
    public struct FirebaseCredentials
    {
        public const string API_URL = "https://firestore.googleapis.com/v1/";

        //THESE SHOULD NOT BE IN PUBLIC NAMESPACE OR ON PUBLIC REPO
        internal const string ADMIN_EMAIL = "";
        internal const string ADMIN_PASSWORD = "";
        internal const string API_KEY = "";
        internal const string projectId = ""; // You can find this in your Firebase project settings
        internal const string dbPath = $"projects/{projectId}/databases/(default)/documents/";
        internal static readonly string rtdbPath = "https://" + projectId + "-default-rtdb.firebaseio.com";
    }
}
