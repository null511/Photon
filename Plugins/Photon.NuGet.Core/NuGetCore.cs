﻿using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using Photon.Framework.Domain;
using Photon.Framework.Extensions;
using Photon.Framework.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.NuGet.CorePlugin
{
    public class NuGetCore : IDisposable
    {
        private static readonly Regex existsExp;

        private SourceRepository sourceRepository;

        public PackageSource PackageSource {get; private set;}
        public string SourceUrl {get; set;}
        public bool EnableV2 {get; set;}
        public bool EnableV3 {get; set;}
        public SourceCacheContext Cache {get; set;}
        public ILogger Logger {get; set;}
        public DomainOutput Output {get; set;}
        public string ApiKey {get; set;}
        public int PushTimeout {get; set;}


        static NuGetCore()
        {
            existsExp = new Regex(@":\s*409\s*\(A package with ID '\S+' and version '\S+' already exists", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public NuGetCore(IDomainContext context = null)
        {
            //this.context = context;
            Output = context?.Output;

            SourceUrl = "https://api.nuget.org/v3/index.json";
            PushTimeout = 60;
            EnableV3 = true;
            Logger = new NullLogger();

            Cache = new SourceCacheContext();
        }

        public void Dispose()
        {
            Cache?.Dispose();
        }

        public void Initialize()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();

            if (EnableV2)
                providers.AddRange(Repository.Provider.GetCoreV2());

            if (EnableV3)
                providers.AddRange(Repository.Provider.GetCoreV3());

            PackageSource = new PackageSource(SourceUrl);
            sourceRepository = new SourceRepository(PackageSource, providers);
        }

        public async Task<string[]> GetAllPackageVersions(string packageId, CancellationToken token = default(CancellationToken))
        {
            var searchResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(token);

            if (searchResource == null) throw new ApplicationException("Unable to retrieve package locator resource!");

            var versionList = (await searchResource.GetAllVersionsAsync(packageId, Cache, Logger, token))?.ToArray();

            if (versionList == null) throw new ApplicationException("Unable to retrieve version list!");

            return versionList.Select(x => x.ToString()).ToArray();
        }

        public void Pack(string nuspecFilename, string packageFilename)
        {
            if (string.IsNullOrEmpty(nuspecFilename)) throw new ArgumentNullException(nameof(nuspecFilename));
            if (string.IsNullOrEmpty(packageFilename)) throw new ArgumentNullException(nameof(packageFilename));

            var nuspecName = Path.GetFileName(nuspecFilename);
            var packageName = Path.GetFileName(packageFilename);

            Output?.WriteLine($"Parsing package definition '{nuspecName}'...", ConsoleColor.Gray);

            Manifest nuspec;
            try {
                using (var nuspecStream = File.Open(nuspecFilename, FileMode.Open, FileAccess.Read)) {
                    nuspec = Manifest.ReadFrom(nuspecStream, true);
                }
            }
            catch (FileNotFoundException) {
                Output?.WriteBlock()
                    .Write("Package definition ", ConsoleColor.DarkYellow)
                    .Write(packageName, ConsoleColor.Yellow)
                    .WriteLine(" not found!", ConsoleColor.DarkYellow)
                    .Post();

                throw;
            }
            catch (Exception error) {
                Output?.WriteBlock()
                    .Write("Failed to load package definition ", ConsoleColor.DarkRed)
                    .Write(packageName, ConsoleColor.Red)
                    .WriteLine("!", ConsoleColor.DarkRed)
                    .WriteLine(error.UnfoldMessages(), ConsoleColor.DarkYellow)
                    .Post();

                throw;
            }

            Output?.WriteLine($"Creating Package '{packageName}'...", ConsoleColor.Gray);

            try {
                var outputPath = Path.GetDirectoryName(packageFilename);

                PathEx.CreatePath(outputPath);

                var builder = new PackageBuilder();
                builder.Populate(nuspec.Metadata);
                // TODO: Apply pack properties

                using (var packageStream = File.Open(packageFilename, FileMode.Create, FileAccess.Write)) {
                    builder.Save(packageStream);
                }

                Output?.WriteBlock()
                    .Write("Package ", ConsoleColor.DarkGreen)
                    .Write(packageName, ConsoleColor.Green)
                    .WriteLine(" created successfully.", ConsoleColor.DarkGreen)
                    .Post();
            }
            catch (Exception error) {
                Output?.WriteBlock()
                    .Write("Failed to create package ", ConsoleColor.DarkRed)
                    .Write(packageName, ConsoleColor.Red)
                    .WriteLine("!", ConsoleColor.DarkRed)
                    .WriteLine(error.UnfoldMessages(), ConsoleColor.DarkYellow)
                    .Post();

                throw;
            }
        }

        public async Task PushAsync(string packageFilename, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(packageFilename)) throw new ArgumentNullException(nameof(packageFilename));

            var packageName = Path.GetFileName(packageFilename);

            Output?.WriteLine($"Publishing Package '{packageName}'...", ConsoleColor.Gray);

            try {
                var updateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>(token);
                await updateResource.Push(packageFilename, null, PushTimeout, false, x => ApiKey, null, Logger);

                Output?.WriteBlock()
                    .Write("Package ", ConsoleColor.DarkGreen)
                    .Write(packageName, ConsoleColor.Green)
                    .WriteLine(" published successfully.", ConsoleColor.DarkGreen)
                    .Post();
            }
            catch (HttpRequestException error) when (existsExp.IsMatch(error.Message)) {
                Output?.WriteBlock()
                    .Write("Package ", ConsoleColor.DarkYellow)
                    .Write(packageName, ConsoleColor.Yellow)
                    .WriteLine(" already exists.", ConsoleColor.DarkYellow)
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
