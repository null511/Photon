﻿using Photon.Framework;
using Photon.Framework.Agent;
using Photon.Framework.Domain;
using Photon.Framework.Process;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.MSBuild
{
    /// <summary>
    /// Adapter for using MSBuild from the command line.
    /// </summary>
    public class MSBuildCommand
    {
        private readonly IAgentContext context;

        /// <summary>
        /// Gets or sets the writer to print to process output to.
        /// </summary>
        public DomainOutput Output {get; set;}

        /// <summary>
        /// Gets or sets the executable to use when running MSBuild.
        /// </summary>
        public string Exe {get; set;}

        /// <summary>
        /// Gets or sets the optional working directory for executing MSBuild.
        /// </summary>
        public string WorkingDirectory {get; set;}

        /// <summary>
        /// Gets or sets whether the command run is echoed to the task output stream.
        /// </summary>
        public bool EchoCommand {get; set;}


        public MSBuildCommand(IAgentContext context = null)
        {
            this.context = context;

            Exe = "msbuild";
            EchoCommand = true;
            Output = context?.Output;
        }

        public ProcessResult Run(MSBuildArguments arguments, CancellationToken cancelToken = default(CancellationToken))
        {
            var argumentList = arguments.GetArguments().ToArray();
            return Execute(string.Join(" ", argumentList), cancelToken);
        }

        public async Task<ProcessResult> RunAsync(MSBuildArguments arguments, CancellationToken cancelToken = default(CancellationToken))
        {
            var argumentList = arguments.GetArguments().ToArray();
            return await ExecuteAsync(string.Join(" ", argumentList), cancelToken);
        }

        public ProcessResult Run(string arguments, CancellationToken cancelToken = default(CancellationToken))
        {
            return Execute(arguments, cancelToken);
        }

        public async Task<ProcessResult> RunAsync(string arguments, CancellationToken cancelToken = default(CancellationToken))
        {
            return await ExecuteAsync(arguments, cancelToken);
        }

        private ProcessResult Execute(string arguments, CancellationToken cancelToken)
        {
            var info = GetProcessArgs(arguments);

            var result = new ProcessRunner(context) {
                Output = Output,
            }.Run(info, cancelToken);

            if (result.ExitCode != 0) throw new ApplicationException($"MSBuild process returned a non-zero exit code! [{result.ExitCode}]");
            return result;
        }

        private async Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancelToken)
        {
            var info = GetProcessArgs(arguments);

            var result = await new ProcessRunner(context) {
                Output = Output,
            }.RunAsync(info, cancelToken);

            if (result.ExitCode != 0) throw new ApplicationException($"MSBuild process returned a non-zero exit code! [{result.ExitCode}]");
            return result;
        }

        private ProcessRunInfo GetProcessArgs(string arguments)
        {
            var info = new ProcessRunInfo {
                Filename = Exe,
                Arguments = arguments,
                EchoCommand = EchoCommand,
            };

            if (!string.IsNullOrEmpty(WorkingDirectory))
                info.WorkingDirectory = WorkingDirectory;

            return info;
        }
    }
}