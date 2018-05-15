﻿using Photon.Framework.Agent;
using Photon.Framework.Extensions;
using Photon.Framework.Tasks;
using System.Linq;
using System.Reflection;

namespace Photon.Framework.Domain
{
    public class AgentDomainAgent : DomainAgentBase
    {
        private readonly BuildTaskRegistry buildTaskRegistry;
        private readonly DeployTaskRegistry deployTaskRegistry;


        public AgentDomainAgent()
        {
            buildTaskRegistry = new BuildTaskRegistry();
            deployTaskRegistry = new DeployTaskRegistry();
        }

        protected override void OnAssemblyLoaded(Assembly assembly)
        {
            base.OnAssemblyLoaded(assembly);

            buildTaskRegistry.ScanAssembly(assembly);
            deployTaskRegistry.ScanAssembly(assembly);
        }

        public string[] GetBuildTasks()
        {
            return buildTaskRegistry.AllNames.ToArray();
        }

        public string[] GetDeployTasks()
        {
            return deployTaskRegistry.AllNames.ToArray();
        }

        public void RunBuildTask(IAgentBuildContext context, RemoteTaskCompletionSource completeEvent)
        {
            buildTaskRegistry.ExecuteTask(context, completeEvent.Token)
                .ContinueWith(completeEvent.FromTask);
        }

        public void RunDeployTask(IAgentDeployContext context, RemoteTaskCompletionSource completeEvent)
        {
            deployTaskRegistry.ExecuteTask(context, completeEvent.Token)
                .ContinueWith(completeEvent.FromTask);
        }
    }
}
