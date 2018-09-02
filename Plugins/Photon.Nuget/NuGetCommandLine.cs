﻿using Photon.Framework.Domain;
using Photon.Framework.Extensions;
using Photon.Framework.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Photon.NuGetPlugin
{
    public class NuGetCommandLine
    {
        private static readonly Regex existsExp;
        private readonly IDomainContext context;

        public string ExeFilename {get; set;}
        public string SourceUrl {get; set;}
        public DomainOutput Output {get; set;}
        public string ApiKey {get; set;}


        static NuGetCommandLine()
        {
            existsExp = new Regex(@":\s*409\s*\(A package with ID '\S+' and version '\S+' already exists", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public NuGetCommandLine(IDomainContext context = null)
        {
            this.context = context;

            Output = context?.Output;
            SourceUrl = "https://api.nuget.org/v3/index.json";
        }

        public void Pack(string nuspecFilename, string outputPath, IDictionary<string, string> properties, CancellationToken cancelToken = default(CancellationToken))
        {
            var path = Path.GetDirectoryName(nuspecFilename);
            var nuspecName = Path.GetFileName(nuspecFilename);

            Output?.WriteBlock()
                .Write("Creating Package from nuspec ", ConsoleColor.DarkCyan)
                .Write(nuspecName, ConsoleColor.Cyan)
                .WriteLine("...", ConsoleColor.DarkCyan)
                .Post();

            try {
                var args = new List<string>(new[] {
                    "pack", $"\"{nuspecName}\"", "-NonInteractive",
                    $"-OutputDirectory \"{outputPath}\"",
                });

                args.AddRange(properties.Select(prop => $"-Prop {prop.Key}=\"{prop.Value}\""));

                var argStr = string.Join(" ", args);

                var runInfo = new ProcessRunInfo {
                    Filename = ExeFilename,
                    Arguments = argStr,
                    WorkingDirectory = path,
                };

                var result = new ProcessRunner(context) {
                    Output = Output,
                }.Run(runInfo, cancelToken);

                if (result.ExitCode != 0)
                    throw new ApplicationException($"NuGet Pack failed with exit code {result.ExitCode}!");

                Output?.WriteLine("Package created successfully.", ConsoleColor.DarkGreen);
            }
            catch (Exception error) {
                Output?.WriteBlock()
                    .WriteLine("Failed to create package!", ConsoleColor.DarkRed)
                    .WriteLine(error.UnfoldMessages(), ConsoleColor.DarkYellow)
                    .Post();

                throw;
            }
        }

        public void Push(string packageFilename, CancellationToken cancelToken = default(CancellationToken))
        {
            var packageName = Path.GetFileName(packageFilename);

            Output?.WriteBlock()
                .Write("Publishing Package ", ConsoleColor.DarkCyan)
                .Write(packageName, ConsoleColor.Cyan)
                .WriteLine("...", ConsoleColor.DarkCyan)
                .Post();

            try {
                var name = Path.GetFileName(packageFilename);
                var path = Path.GetDirectoryName(packageFilename);

                var args = string.Join(" ",
                    "push", $"\"{name}\"", "-NonInteractive",
                    $"-Source \"{SourceUrl}\"",
                    $"-ApiKey \"{ApiKey}\"");

                var runInfo = new ProcessRunInfo {
                    Filename = ExeFilename,
                    Arguments = args,
                    WorkingDirectory = path,
                };

                var result = new ProcessRunner(context) {
                    Output = Output,
                }.Run(runInfo, cancelToken);

                if (result.ExitCode != 0) {
                    if (existsExp.IsMatch(result.Error)) {
                        Output?.WriteBlock()
                            .Write("Package ", ConsoleColor.DarkGreen)
                            .Write(packageName, ConsoleColor.Green)
                            .WriteLine(" already exists.", ConsoleColor.DarkYellow)
                            .Post();

                        return;
                    }

                    throw new ApplicationException($"NuGet Push failed with exit code {result.ExitCode}!");
                }

                Output?.WriteBlock()
                    .Write("Package ", ConsoleColor.DarkGreen)
                    .Write(packageName, ConsoleColor.Green)
                    .WriteLine(" published successfully.", ConsoleColor.DarkGreen)
                    .Post();
            }
            catch (Exception error) {
                Output?.WriteBlock()
                    .Write("Failed to publish package ", ConsoleColor.DarkRed)
                    .Write(packageName, ConsoleColor.Red)
                    .WriteLine("!", ConsoleColor.DarkRed)
                    .WriteLine(error.UnfoldMessages(), ConsoleColor.DarkYellow)
                    .Post();

                throw;
            }
        }
    }
}
