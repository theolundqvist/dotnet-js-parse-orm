using System;
using Project.Util;
namespace Project.Util.WebUtil
{
    public class WebRequestException : Exception
    {
        public readonly string message;
        public readonly Exception error;
        private readonly bool includeFootprints = false;


        public WebRequestException(Exception e)
        {
            this.includeFootprints = true;
            error = BuildRequestError(e, "");
            message = error.Message;
        }

        public WebRequestException(Exception e, string content, bool includeFootprints = false)
        {
            this.includeFootprints = includeFootprints;
            error = BuildRequestError(e, content);
            //message = GetWebExceptionMessage(content);
            if (message == "") message = error.Message;
        }

        public static Exception ParseWebException<T>(Exception e, string content) where T : Exception
        { 
            try { return JsonUtil.FromJson<T>(content); }
            catch { }
            return e;
        }


        private Exception BuildRequestError(Exception e, string content)
        {
            //Create error message
            string errorMessage = "";
            if (e != null)
            {
                if (!string.IsNullOrEmpty(e.Message)) errorMessage += e.Message + "\n\n";
            }
            errorMessage += content;
            if (errorMessage.Contains("NameResolutionFailure")) errorMessage += " (No internetconnection)";

            //TODO IF FIREBASE MESSAGE THEN ADD STATUS CODE TO MESSAGE

            //Tools.Debugger.Print(GetAllFootprints(e));

            return new Exception(errorMessage + (includeFootprints ? GetAllFootprints(e) : ""));
        }


        private string GetAllFootprints(Exception x)
        {
            var st = new System.Diagnostics.StackTrace(x, true);
            var frames = st.GetFrames();
            var traceString = new System.Text.StringBuilder();

            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                traceString.Append("File: " + frame.GetFileName());
                traceString.Append(", Method:" + frame.GetMethod().Name);
                traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
                traceString.Append("  -->  \n");
            }

            return traceString.ToString();
        }
    }
}
