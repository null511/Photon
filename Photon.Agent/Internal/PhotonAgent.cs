﻿using log4net;
using Newtonsoft.Json;
using Photon.Framework;
using Photon.Framework.Communication;
using Photon.Framework.Extensions;
using PiServerLite.Http;
using PiServerLite.Http.Content;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Photon.Agent.Internal
{
    internal class PhotonAgent : IDisposable
    {
        private static ILog Log = LogManager.GetLogger(typeof(PhotonAgent));
        public static PhotonAgent Instance {get;} = new PhotonAgent();

        private readonly MessageProcessor messageProcessor;
        private readonly MessageListener messageListener;
        private HttpReceiver receiver;
        private bool isStarted;

        public string WorkDirectory {get;}
        public AgentSessionManager Sessions {get;}
        public AgentDefinition Definition {get; private set;}


        public PhotonAgent()
        {
            Sessions = new AgentSessionManager();

            messageProcessor = new MessageProcessor();
            messageProcessor.Scan(Assembly.GetExecutingAssembly());

            messageListener = new MessageListener(messageProcessor);

            WorkDirectory = Configuration.WorkDirectory;
        }

        public void Dispose()
        {
            //if (isStarted) Stop();

            Sessions?.Dispose();
            receiver?.Dispose();
            receiver = null;
        }

        public void Start()
        {
            if (isStarted) throw new Exception("Agent has already been started!");
            isStarted = true;

            // Load existing or default agent configuration
            Definition = ParseAgentDefinition() ?? new AgentDefinition {
                Http = {
                    Host = "localhost",
                    Port = 80,
                    Path = "/photon/agent",
                },
            };

            var context = new HttpReceiverContext {
                //SecurityMgr = new Internal.Security.SecurityManager(),
                ListenerPath = Definition.Http.Path,
                ContentDirectories = {
                    new ContentDirectory {
                        DirectoryPath = Path.Combine(Configuration.AssemblyPath, "Content"),
                        UrlPath = "/Content/",
                    }
                },
            };

            var viewPath = Path.Combine(Configuration.AssemblyPath, "Views");
            context.Views.AddFolderFromExternal(viewPath);

            var httpPrefix = $"http://+:{Definition.Http.Port}/";
            if (!string.IsNullOrEmpty(Definition.Http.Path))
                httpPrefix = NetPath.Combine(httpPrefix, Definition.Http.Path);

            if (!httpPrefix.EndsWith("/"))
                httpPrefix += "/";

            receiver = new HttpReceiver(context);
            receiver.Routes.Scan(Assembly.GetExecutingAssembly());
            receiver.AddPrefix(httpPrefix);

            try {
                receiver.Start();
            }
            catch (Exception error) {
                Log.Error("Failed to start HTTP Receiver!", error);
            }

            Sessions.Start();
            messageProcessor.Start();
            messageListener.Listen(IPAddress.Any, 10933);
        }

        public void Stop()
        {
            if (!isStarted) return;
            isStarted = false;

            messageListener.Stop()
                .GetAwaiter().GetResult();

            messageProcessor.StopAsync()
                .GetAwaiter().GetResult();

            Sessions.Stop();
            receiver.Stop();
        }

        private AgentDefinition ParseAgentDefinition()
        {
            var file = Configuration.DefinitionPath ?? "agent.json";
            var path = Path.Combine(Configuration.AssemblyPath, file);
            path = Path.GetFullPath(path);

            Log.Debug($"Loading Agent Definition: {path}");

            if (!File.Exists(path)) {
                Log.Warn($"Agent Definition not found! {path}");
                return null;
            }

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<AgentDefinition>(stream);
            }
        }
    }
}
