using GestionNotes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GestionNotes.Services
{
    public class UpdateService
    {
        public async Task<UpdateManifest?> CheckForUpdateAsync()
        {
            string manifestPath = Path.Combine(
                UpdateConfig.DepotPath,
                "version.json"
            );

            string json = await File.ReadAllTextAsync(manifestPath);

            UpdateManifest? manifest =
                JsonSerializer.Deserialize<UpdateManifest>(json);

            if (manifest == null ||
                string.IsNullOrWhiteSpace(manifest.Version) ||
                string.IsNullOrWhiteSpace(manifest.Fichier))
            {
                throw new InvalidDataException(
                    "Le manifeste de mise à jour est invalide.");
            }

            if (!System.Version.TryParse(manifest.Version, out var remoteVersion))
            {
                throw new InvalidDataException(
                    "Le numéro de version est invalide.");
            }

            if (remoteVersion > UpdateConfig.CurrentVersion)
            {
                return manifest;
            }

            return null;
        }

        public async Task<string> DownloadUpdateAsync(UpdateManifest manifest)
        {
            string depot = Path.GetFullPath(UpdateConfig.DepotPath);

            // Empêche le manifeste de pointer hors du dépôt.
            string fileName = manifest.Fichier;

            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != Path.GetFileName(fileName) ||
                !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Le nom de l'archive est invalide.");
            }

            string sourcePath = Path.Combine(depot, fileName);

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "L'archive de mise à jour est introuvable.",
                    sourcePath);
            }

            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "GestionNotes-Updates",
                Guid.NewGuid().ToString("N")
            );

            Directory.CreateDirectory(tempDirectory);

            string destinationPath = Path.Combine(tempDirectory, fileName);

            try
            {
                await using (var source = new FileStream(
                    sourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 81920, useAsync: true))
                await using (var destination = new FileStream(
                    destinationPath, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 81920, useAsync: true))
                {
                    await source.CopyToAsync(destination);
                }

                long sourceLength = new FileInfo(sourcePath).Length;
                long destinationLength = new FileInfo(destinationPath).Length;

                if (sourceLength == 0 || sourceLength != destinationLength)
                {
                    throw new IOException(
                        "La copie de l'archive est incomplète.");
                }

                return destinationPath;
            }
            catch
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);

                throw;
            }
        }
    }
}
