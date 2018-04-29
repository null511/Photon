﻿using Newtonsoft.Json;
using Photon.Framework;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Photon.Server.Internal.GitHub
{
    public class CommitStatusUpdater
    {
        public string Username {get; set;}
        public string Password {get; set;}
        public string StatusUrl {get; set;}
        public string Sha {get; set;}


        public async Task<CommitStatusResponse> Post(CommitStatus status)
        {
            var data = status.ToJson();
            var buffer = Encoding.UTF8.GetBytes(data);
            var url = NetPath.Combine(StatusUrl, Sha);

            var request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.KeepAlive = false;
            request.ContentType = "application/json";
            request.ContentLength = buffer.Length;
            request.UserAgent = "Photon.Server";

            var hasUsername = !string.IsNullOrEmpty(Username);
            var hasPassword = !string.IsNullOrEmpty(Password);

            if (hasUsername || hasPassword) {
                var encoding = Encoding.GetEncoding("ISO-8859-1");
                var authBuffer = encoding.GetBytes($"{Username}:{Password}");
                var authString = Convert.ToBase64String(authBuffer);
                request.Headers.Add("Authorization", $"Basic {authString}");
            }

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
