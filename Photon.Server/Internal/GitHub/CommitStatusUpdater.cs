﻿using Newtonsoft.Json;
using Photon.Framework;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Photon.Server.Internal.GitHub
{
    public class CommitStatusUpdater
    {
        //public string GitHubUrl {get; set;}
        //public string Owner {get; set;}
        //public string Repository {get; set;}
        public string StatusUrl {get; set;}
        public string Sha {get; set;}


        public async Task<CommitStatusResponse> Post(CommitStatus status)
        {
            var data = status.ToJson();
            var buffer = Encoding.UTF8.GetBytes(data);
            var url = StatusUrl.Replace("{sha}", Sha);

            var request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.KeepAlive = false;
            request.ContentType = "application/json";
            request.ContentLength = buffer.Length;
            request.UserAgent = "Photon.Server";

            using (var requestStream = request.GetRequestStream()) {
                await requestStream.WriteAsync(buffer, 0, buffer.Length);
            }

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream()) {
                if (responseStream == null) return null;

                using (var reader = new StreamReader(responseStream))
                using (var jsonReader = new JsonTextReader(reader)) {
                    return JsonSettings.Serializer.Deserialize<CommitStatusResponse>(jsonReader);
                }
            }
        }
    }
}
