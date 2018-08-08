﻿using log4net;
using Photon.Agent.Internal;
using Photon.Agent.Internal.Security;
using Photon.Library.HttpSecurity;
using PiServerLite.Http.Handlers;
using PiServerLite.Http.Security;
using System;

namespace Photon.Agent.ApiHandlers.Security
{
    [Secure]
    [RequiresRoles(GroupRole.SecurityEdit)]
    [HttpHandler("api/security/enabled")]
    internal class SecurityEnabledHandler : HttpHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SecurityEnabledHandler));


        public override HttpHandlerResult Get()
        {
            var config = PhotonAgent.Instance.AgentConfiguration;
            var value = config.Value.Security.Enabled;

            return Response.Ok().SetText(value.ToString());
        }

        public override HttpHandlerResult Post()
        {
            var value = GetQuery("value", true);

            try {
                var httpSecurity = (HttpSecurityManager)PhotonAgent.Instance.HttpContext.SecurityMgr;
                httpSecurity.Restricted = value;

                var config = PhotonAgent.Instance.AgentConfiguration;
                config.Value.Security.Enabled = value;
                config.Save();

                return Response.Ok();
            }
            catch (Exception error) {
                Log.Error("Failed to modify security-enabled value!", error);
                return Response.Exception(error);
            }
        }
    }
}