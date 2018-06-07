﻿using Photon.Server.ViewModels.Deployment;
using PiServerLite.Http.Handlers;
using PiServerLite.Http.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Server.ViewHandlers.Deployment
{
    [Secure]
    [HttpHandler("/deployment/new")]
    internal class DeploymentNewHandler : HttpHandlerAsync
    {
        public override Task<HttpHandlerResult> GetAsync(CancellationToken token)
        {
            var vm = new DeploymentNewVM {
                ProjectId = GetQuery("project"),
                PackageId = GetQuery("package"),
                PackageVersion = GetQuery("version"),
            };

            try {
                vm.Build();
            }
            catch (Exception error) {
                vm.Errors.Add(error);
            }

            return Response.View("Deployment\\New.html", vm).AsAsync();
        }

        public override async Task<HttpHandlerResult> PostAsync(CancellationToken token)
        {
            var vm = new DeploymentNewVM();

            try {
                vm.Restore(Request.FormData());
                await vm.StartDeployment();

                return Response.Redirect("session/details", new {
                    id = vm.ServerSessionId,
                });
            }
            catch (Exception error) {
                vm.Errors.Add(error);
                return Response.View("Build\\New.html", vm);
            }
        }
    }
}
