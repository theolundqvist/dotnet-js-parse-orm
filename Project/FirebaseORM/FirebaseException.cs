using Project.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Project.FirebaseORM
{
    [Obsolete("Use new Project.Backend")]
    public class FirebaseException : Exception
    {
        public override string Message =>
            string.IsNullOrEmpty(error.status) ? error.message : error.status + " : " + error.message;

        public Error error;

        public static Exception Parse(Exception e)
        {
            Debugger.Print("PARSING EXCEPTION: " + e.Message);
            if (e.Message.Contains("Connection refused")) return new Exception("Connection failed", e);
            try
            {
                var ex = JsonUtil.FromJson<FirebaseException>(e.Message);
                Debugger.Print("PARSED EXCEPTION: " + ex.Message);
                return new Exception(ex.Message, e);
            }
            catch (Exception)
            {
                try
                {
                    var ex = JsonUtil.FromJson<FirebaseException[]>(e.Message);
                    Debugger.Print("PARSED EXCEPTION: " + ex[0].Message);
                    return new Exception(ex[0].Message, e);
                }
                catch (Exception)
                {
                    Debugger.Print("FAILED TO PARSE EXCEPTION");
                    return e;
                }
            }
        }
    }
    [Obsolete("Use new Project.Backend")]
    public class Error
    {
        public int code;
        public string message = "";
        public string status = "";
    }
}