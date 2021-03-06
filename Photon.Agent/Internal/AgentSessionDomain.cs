﻿using log4net;
using Photon.Framework.Agent;
using Photon.Framework.Domain;
using Photon.Framework.Tasks;
using Photon.Library.Session;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Agent.Internal
{
    internal class AgentSessionDomain : SessionDomainBase<AgentDomainAgent>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AgentSessionDomain));


        public TaskDescription[] GetBuildTasks()
        {
            return Agent.GetBuildTasks();
        }

        public TaskDescription[] GetDeployTasks()
        {
            return Agent.GetDeployTasks();
        }

        public async Task RunBuildTask(AgentBuildContext context, CancellationToken token = default(CancellationToken))
        {
            if (!Agent.IsBuildTaskInRole(context)) return;

            Log.Debug($"Running build Task '{context.TaskName}'...");

            using (var completeEvent = new RemoteTaskCompletionSource())
            using (token.Register(completeEvent.SetCancelled)) {
                try {
                    Agent.RunBuildTask(context, completeEvent);
                    await completeEvent.Task;

                    Log.Info($"Build Task '{context.TaskName}' complete.");
                }
                catch (Exception error) {
                    Log.Error($"Build Task '{context.TaskName}' failed!", error);
                    throw;
                }
            }
        }

        public async Task RunDeployTask(AgentDeployContext context, CancellationToken token = default(CancellationToken))
        {
            if (!Agent.IsDeployTaskInRole(context)) return;

            Log.Debug($"Running deployment Task '{context.TaskName}'...");

            using (var completeEvent = new RemoteTaskCompletionSource()) 
            using (token.Register(completeEvent.SetCancelled)) {
                try {
                    Agent.RunDeployTask(context, completeEvent);
                    await completeEvent.Task;

                    Log.Info($"Deployment Task '{context.TaskName}' complete.");
                }
                catch (Exception error) {
                    Log.Error($"Deployment Task '{context.TaskName}' failed!", error);
                    throw;
                }
            }
        }
    }
}
