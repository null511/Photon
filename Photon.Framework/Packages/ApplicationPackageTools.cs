﻿using Photon.Framework.Tools;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Photon.Framework.Packages
{
    public class ApplicationPackageTools
    {
        /// <summary>
        /// Creates an Application Package using the specified
        /// definition file.
        /// </summary>
        /// <param name="version">The version of the package to create.</param>
        /// <param name="outputFilename">The file name of the output package.</param>
        public static async Task CreatePackage(ApplicationPackageDefinition definition, string rootPath, string version, string outputFilename)
        {
            var outputFilenameFull = Path.GetFullPath(outputFilename);
            var outputPath = Path.GetDirectoryName(outputFilenameFull);

            if (string.IsNullOrEmpty(outputPath))
                throw new ApplicationException("Empty package output path!");

            PathEx.CreatePath(outputPath);

            await PackageTools.WriteArchive(outputFilename, async archive => {
                AppendMetadata(archive, definition, version);

                foreach (var fileDefinition in definition.Files) {
                    var destPath = Path.Combine("bin", fileDefinition.Destination);

                    await PackageTools.AddFiles(archive, rootPath, fileDefinition.Path, destPath, fileDefinition.Exclude?.ToArray());
                }
            });
        }

        public static async Task<ApplicationPackage> GetMetadataAsync(string filename)
        {
            ApplicationPackage package = null;

            await PackageTools.ReadArchive(filename, async archive => {
                package = await PackageTools.ParseMetadataAsync<ApplicationPackage>(archive);
            });

            return package;
        }

        public static async Task<ApplicationPackage> UnpackAsync(string filename, string path)
        {
            PathEx.CreatePath(path);

            ApplicationPackage metadata = null;

            await PackageTools.ReadArchive(filename, async archive => {
                metadata = await PackageTools.ParseMetadataAsync<ApplicationPackage>(archive);

                await PackageTools.UnpackBin(archive, path);
            });

            return metadata;
        }

        private static void AppendMetadata(ZipArchive archive, ApplicationPackageDefinition definition, string version)
        {
            var metadata = new ApplicationPackage {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                Version = version,
            };

            PackageTools.AppendMetadata(archive, metadata, version);
        }
    }
}
