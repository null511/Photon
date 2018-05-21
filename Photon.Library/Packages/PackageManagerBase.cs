﻿using Photon.Framework.Packages;
using Photon.Framework.Tools;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Photon.Library.Packages
{
    [Serializable]
    public abstract class PackageManagerBase
    {
        public string PackageDirectory {get; set;}


        public bool TryGet(string packageId, string packageVersion, out string packageFilename)
        {
            packageFilename = GetPackageFilename(packageId, packageVersion);
            return File.Exists(packageFilename);
        }

        protected async Task Add(string filename, IPackageMetadata metadata)
        {
            var packageFilename = GetPackageFilename(metadata.Id, metadata.Version);

            if (File.Exists(packageFilename))
                throw new Exception($"Package '{metadata.Id}.{metadata.Version}' already exists!");

            await Task.Run(() => {
                PathEx.CreateFilePath(packageFilename);

                File.Copy(filename, packageFilename);
            });
        }

        protected string GetPackageFilename(string packageId, string packageVersion)
        {
            var filename = $"{packageId}.{packageVersion}.zip";
            return Path.Combine(PackageDirectory, packageId, filename);
        }
    }
}
