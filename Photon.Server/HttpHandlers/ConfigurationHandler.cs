﻿using Photon.Library;
using PiServerLite.Http.Handlers;

namespace Photon.Server.HttpHandlers
{
    [HttpHandler("/configuration")]
    internal class ConfigurationHandler : HttpHandler
    {
        public override HttpHandlerResult Get()
        {
            var vm = new ViewModelBase();
            vm.Build();

            return View("Configuration.html", vm);
        }
    }
}
