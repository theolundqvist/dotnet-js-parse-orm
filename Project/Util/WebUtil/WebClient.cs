using System;
using RestSharp;
using RSG;
using System.Threading;
using Project.Util;
using RestSharp.Serialization;

namespace Project.Util.WebUtil
{
    public class WebClient
    {
        private readonly WebRequest req;
        public WebClient(WebRequest req) => this.req = req;

        internal void SetMethod(Method method) => req.restRequest.Method = method;

        
        public IPromise<string> PostString() => StartRequestThread(Method.POST).Then(x => x.Content);
        public IPromise<string> SendString(Method m) => StartRequestThread(m).Then(x => x.Content);  
        public IPromise<string> GetString() => StartRequestThread(Method.GET).Then(x => x.Content);  
        public IPromise<string> DeleteString() => StartRequestThread(Method.DELETE).Then(x => x.Content);

        
        public IPromise<IRestResponse> Post() => StartRequestThread(Method.POST);
        public IPromise<IRestResponse> Send(Method m) => StartRequestThread(m);
        public IPromise<IRestResponse> Get() => StartRequestThread(Method.GET);
        public IPromise<IRestResponse> Delete() => StartRequestThread(Method.DELETE);
        
        
        private Promise<IRestResponse> StartRequestThread(Method method)
        {
            SetMethod(method);
            var p = new Promise<IRestResponse>();
            new Thread(() => PerformRestRequest(ref p)).Start();
            return p;
        }
        private void PerformRestRequest(ref Promise<IRestResponse> p)
        {
            try
            {
                IRestResponse res = CreateRestClient(req.uri).Execute(req.restRequest);

                if (res.IsSuccessful)   
                {
                    p.Resolve(res);
                }
                else
                {
                    if(p.CurState == PromiseState.Rejected) Debugger.Print("WARNING: Promise already rejected: " + res.ErrorMessage + "\n\n" + res.Content);
                    if (string.IsNullOrEmpty(res.Content))
                    {
                        if(p.CurState == PromiseState.Pending) p.Reject(res.ErrorMessage);
                    }
                    else if(p.CurState == PromiseState.Pending) p.Reject(res.Content);
                }
            }
            catch (Exception e)
            {
                if(p.CurState == PromiseState.Rejected) Debugger.Print("WARNING: Promise already rejected: " + e.Message);
                if(p.CurState == PromiseState.Pending) p.Reject(e.Message);
                //p.OnFailure(error => { Helpers.Debugger.Print(error.Message); });
            }
        }

        private static RestClient CreateRestClient(string uri)
        {
            var client = new RestClient(uri)
            {
                ThrowOnAnyError = true,
            };
            client.UseSerializer(() => new SimpleJsonSerializer());
            return client;
        }

    }

    public class SimpleJsonSerializer : IRestSerializer
    {
        public string Serialize(object obj) => JsonUtil.ToJson(obj);
        public T Deserialize<T>(IRestResponse response) => JsonUtil.FromJson<T>(response.Content);

        public string Serialize(Parameter parameter) => Serialize(parameter.Value);

        public string[] SupportedContentTypes { get; } = {
            "application/json", "text/json", "text/x-json", "text/javascript", "*+json"
        };

        public string ContentType { get; set; } = "application/json";

        public DataFormat DataFormat { get; } = DataFormat.Json;
    }

}