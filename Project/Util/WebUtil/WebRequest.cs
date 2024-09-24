using System;
using RestSharp;

namespace Project.Util.WebUtil
{
    public class WebRequest
    {
        internal RestRequest restRequest = new();
        internal string uri;

        public static class ContentType
        {
            public static string x_www_form = "application/x-www-form-urlencoded";
            public static string json = "application/json";
        }

        public WebRequest()
        {
            SetContentType(ContentType.json);
        }

        public WebRequest AddParam(string name, object value)
        {
            restRequest.AddParameter(name, value);
            return this;
        }
        public WebRequest AddHeader(string name, string value)
        {
            restRequest.AddHeader(name, value);
            return this;
        }
        public WebRequest SetEndpoint(string uri)
        {
            Debugger.Print(uri);
            this.uri = uri;
            return this;
        }
        public WebRequest SetContentType(string type)
        {
            restRequest.AddHeader("ContentType", type);
            return this;
        }

        public WebRequest AddJsonBody(string json)
        {
            restRequest.AddParameter("application/json", json, ParameterType.RequestBody);
            //restRequest.AddJsonBody(json);
            return this;
        }
        public WebRequest AddJsonBody(object dict)
        {
            restRequest.AddParameter("application/json", JsonUtil.ToJson(dict), ParameterType.RequestBody);
            //restRequest.AddJsonBody(json);
            return this;
        }

        //???
        public RSG.IPromise<string> POST_STRING() => new WebClient(this).PostString();
        public RSG.IPromise<string> PUT_STRING() => new WebClient(this).SendString(Method.PUT);
        public RSG.IPromise<string> GET_STRING() => new WebClient(this).GetString();
        public RSG.IPromise<string> DELETE_STRING() => new WebClient(this).DeleteString();
        
        
        public RSG.IPromise<IRestResponse> POST() => new WebClient(this).Post();
        public RSG.IPromise<IRestResponse> PUT() => new WebClient(this).Send(Method.PUT);
        public RSG.IPromise<IRestResponse> GET() => new WebClient(this).Get();
        public RSG.IPromise<IRestResponse> DELETE() => new WebClient(this).Delete();
    }


}
