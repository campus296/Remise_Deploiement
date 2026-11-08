using GestionNotes.Services;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace GestionNotes
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var updateService = new UpdateService();

            try
            {
                var update = await updateService.CheckForUpdateAsync();

                if (update != null)
                {
                    var result = MessageBox.Show(
                        $"La version {update.Version} est disponible.\n" +
                        "Voulez-vous télécharger la mise à jour ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        string zipPath =
                            await updateService.DownloadUpdateAsync(update);

                        string installPath = AppContext.BaseDirectory;

                        string updaterPath =
                            @"C:\GestionNotes-Updater\Updater.exe";

                        if (!File.Exists(updaterPath))
                        {
                            throw new FileNotFoundException(
                                "Updater.exe est introuvable.",
                                updaterPath);
                        }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updaterPath,
                            ArgumentList =
                            {
                                zipPath,
                                installPath,
                                Environment.ProcessId.ToString()
                            },
                            WorkingDirectory = Path.GetDirectoryName(updaterPath)!,
                            UseShellExecute = false
                        });

                        // Fermer GestionNotes pour permettre le remplacement
                        Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible de vérifier les mises à jour :\n{ex.Message}",
                    "Erreur de mise à jour",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }
    }
}