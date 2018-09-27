﻿using Photon.Framework.Projects;
using Photon.Framework.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Server.Internal.AgentConnections
{
    internal class ServerAgentSelector
    {
        //private readonly ServerContextBase context;
        private readonly AgentConnectionManager connectionMgr;

        public Project Project {get; set;}
        public ServerAgent[] Agents {get; set;}


        public ServerAgentSelector(AgentConnectionManager connectionMgr)
        {
            this.connectionMgr = connectionMgr;
            //this.context = context;
        }

        public Task<ServerConnectionCollection> GetAnyAsync(string[] roles, CancellationToken token = default)
        {
            var agents = GetAllAgents();
            var roleAgents = AgentsInRoles(agents, roles);
            var agent = GetRandomAgent(roleAgents);

            //PrintFoundAgents(new[] {agent});

            return ConnectTo(new[] {agent}, token);
            //context.agentConnections.Add(handle);
            //return handle;
        }

        public Task<ServerConnectionCollection> GetAllAsync()
        {
            var agents = GetAllAgents().ToArray();

            //PrintFoundAgents(agents);
            return ConnectTo(agents);
        }

        public Task<ServerConnectionCollection> GetAllAsync(params string[] roles)
        {
            var agents = GetAllAgents();
            var roleAgents = AgentsInRoles(agents, roles).ToArray();

            //PrintFoundAgents(roleAgents);
            return ConnectTo(roleAgents);
        }

        public Task<ServerConnectionCollection> ByEnvironmentAsync(string name)
        {
            var agents = GetAllAgents();
            var environmentAgents = AgentsInEnvironment(agents, name).ToArray();

            //PrintFoundAgents(environmentAgents);
            return ConnectTo(environmentAgents);
        }

        public Task<ServerConnectionCollection> ByEnvironmentAsync(string name, params string[] roles)
        {
            var allAgents = GetAllAgents();
            var environmentAgents = AgentsInEnvironment(allAgents, name);
            var roleAgents = AgentsInRoles(environmentAgents, roles).ToArray();

            //PrintFoundAgents(roleAgents);
            return ConnectTo(roleAgents);
        }

        private async Task<ServerConnectionCollection> ConnectTo(IEnumerable<ServerAgent> agents, CancellationToken token = default)
        {
            var connectionTasks = agents.Select(agent => {
                var connection = connectionMgr.Create(agent);
                //context.agentConnections.Add(connection);

                return Task.Run(async () => {
                    await connection.BeginAsync(token);
                    return connection;
                }, token);
            }).ToArray();

            await Task.WhenAll(connectionTasks);

            var connections = connectionTasks.Select(task => task.Result).ToArray();
            return new ServerConnectionCollection(connections);
        }

        private IEnumerable<ServerAgent> GetAllAgents()
        {
            if (Agents?.Any() ?? false) return Agents;

            //context.Output.WriteLine("No agents have been defined!", ConsoleColor.DarkRed);
            throw new ApplicationException("No agents have been defined!");
        }

        private IEnumerable<ServerAgent> AgentsInRoles(IEnumerable<ServerAgent> agents, params string[] roles)
        {
            var roleAgents = agents.Where(a => a.MatchesRoles(roles)).ToArray();

            if (roleAgents.Any()) return roleAgents;

            //context.Output.Write("No agents were found in roles ", ConsoleColor.DarkYellow);

            //var i = 0;
            //foreach (var role in roles) {
            //    if (i > 0) context.Output.Write(", ", ConsoleColor.DarkYellow);
            //    i++;

            //    context.Output.Write(role, ConsoleColor.Yellow);
            //}

            //context.Output.WriteLine("!", ConsoleColor.DarkYellow);

            throw new ApplicationException($"No agents were found in roles '{string.Join(", ", roles)}'!");
        }

        private IEnumerable<ServerAgent> AgentsInEnvironment(IEnumerable<ServerAgent> agents, string environmentName)
        {
            var environmentList = Project?.Environments;

            if (!(environmentList?.Any() ?? false)) {
                //context.Output.WriteLine("No environments have been defined!", ConsoleColor.DarkRed);
                throw new ApplicationException("No environments have been defined!");
            }

            var environment = environmentList.FirstOrDefault(x =>
                string.Equals(x.Name, environmentName, StringComparison.OrdinalIgnoreCase));

            if (environment == null) {
                //context.Output.WriteLine($"Environment '{environmentName}' was not found!", ConsoleColor.DarkYellow);
                throw new ApplicationException($"Environment '{environmentName}' was not found!");
            }

            var environmentAgents = agents
                .Where(a => environment.AgentIdList.Contains(a.Id, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (environmentAgents.Any())
                return environmentAgents;

            //context.Output.WriteLine($"No agents were found in environment '{environmentName}'!", ConsoleColor.DarkYellow);
            throw new ApplicationException($"No agents were found in environment '{environmentName}'!");
        }

        private static ServerAgent GetRandomAgent(IEnumerable<ServerAgent> agents)
        {
            var _agentArray = agents as ServerAgent[] ?? agents.ToArray();

            if (_agentArray.Length <= 1)
                return _agentArray.FirstOrDefault();

            var random = new Random();
            return _agentArray[random.Next(_agentArray.Length)];
        }

        //private void PrintFoundAgents(IEnumerable<ServerAgent> agents)
        //{
        //    var agentNames = agents.Select(x => x.Name);
        //    context.Output.Write("Found Agents: ", ConsoleColor.DarkCyan);

        //    var i = 0;
        //    foreach (var name in agentNames) {
        //        if (i > 0) context.Output.Write("; ", ConsoleColor.DarkCyan);
        //        i++;

        //        context.Output.Write(name, ConsoleColor.Cyan);
        //    }

        //    context.Output.WriteLine(".", ConsoleColor.DarkCyan);
        //}
    }
}