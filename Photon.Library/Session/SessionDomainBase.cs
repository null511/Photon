﻿using Photon.Framework.Domain;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Library.Session
{
    public abstract class SessionDomainBase<T> : IDisposable
        where T : DomainAgentBase
    {
        private bool isUnloaded;
        private AppDomain domain;
        //protected ClientSponsor Sponsor;
        protected T Agent;


        public virtual void Dispose()
        {
            if (!isUnloaded) Unload(false).GetAwaiter().GetResult();

            //if (Agent != null) {
            //    Agent.Dispose();
            //    Agent = null;
            //}
        }

        public void Initialize(string assemblyFilename)
        {
            var assemblyName = Path.GetFileName(assemblyFilename);
            var assemblyPath = Path.GetDirectoryName(assemblyFilename);

            if (string.IsNullOrEmpty(assemblyName))
                throw new ApplicationException("Assembly filename is empty!");

            var domainSetup = new AppDomainSetup {
                ApplicationBase = assemblyPath,
                ConfigurationFile = $"{assemblyFilename}.config",
            };

            //Sponsor = new ClientSponsor {
            //    RenewalTime = TimeSpan.FromMinutes(2),
            //};
            //var lease = Sponsor.InitializeLifetimeService();

            domain = AppDomain.CreateDomain(assemblyName, null, domainSetup);

            var agentType = typeof(T);
            Agent = (T)domain.CreateInstanceAndUnwrap(agentType.Assembly.FullName, agentType.FullName);
            Agent.LoadAssembly(assemblyFilename);

            //var leaseX = (ILease)Agent.GetLifetimeService();
            //Sponsor.Register(Agent);

            //var lease = (ILease)RemotingServices.GetLifetimeService(Agent);
            //lease.Register(Sponsor);
        }

        public async Task Unload(bool wait)
        {
            // A ThreadAbortException will be called
            // if we immediately close the AppDomain.
            //Sponsor.Close();

            if (Agent != null) {
                Agent.Dispose();
                Agent = null;
            }

            try {
                if (wait) await Task.Delay(200);

                AppDomain.Unload(domain);
                domain = null;
            }
            catch (ThreadAbortException) {
                Thread.ResetAbort();
            }
            catch (Exception) {
                //Log.Warn("", error);
            }
            finally {
                isUnloaded = true;
            }

            if (wait) await Task.Delay(400);
        }
    }
}
