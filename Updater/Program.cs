
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

if (args.Length < 3)
{
    Console.WriteLine(
        "Usage : Updater.exe <zipPath> <installPath> <processId>");
    return;
}

string zipPath = args[0];
string installPath = Path.GetFullPath(args[1]);

if (!int.TryParse(args[2], out int processId) || processId <= 0)
{
    Console.WriteLine("Identifiant de processus invalide.");
    return;
}

string tempPath = Path.Combine(
    Path.GetTempPath(),
    "GestionNotes-Extraction",
    Guid.NewGuid().ToString("N"));

string backupPath = Path.Combine(
    Path.GetTempPath(),
    "GestionNotes-Backup",
    Guid.NewGuid().ToString("N"));

bool backupCreated = false;
bool updateSucceeded = false;

try
{
    // 1. Attendre la fermeture de GestionNotes
    Console.WriteLine("Attente de GestionNotes...");

    try
    {
        using Process process = Process.GetProcessById(processId);
        process.WaitForExit();
    }
    catch (ArgumentException)
    {
        // Le processus est déjà fermé.
    }

    if (!File.Exists(zipPath))
        throw new FileNotFoundException("Archive introuvable.", zipPath);

    // 2. Extraire l'archive avant de modifier l'installation
    Directory.CreateDirectory(tempPath);

    Console.WriteLine("Extraction de l'archive...");
    ZipFile.ExtractToDirectory(zipPath, tempPath);

    if (!File.Exists(Path.Combine(tempPath, "GestionNotes.exe")))
        throw new InvalidDataException(
            "L'archive ne contient pas GestionNotes.exe.");

    // 3. Sauvegarder l'installation actuelle
    Console.WriteLine("Sauvegarde de l'ancienne version...");

    CopyDirectory(installPath, backupPath);
    backupCreated = true;

    // 4. Installer la nouvelle version
    Console.WriteLine("Installation de la nouvelle version...");

    CopyDirectory(tempPath, installPath);

    updateSucceeded = true;

    Console.WriteLine("Mise à jour réussie !");
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur : {ex.Message}");

    if (backupCreated)
    {
        try
        {
            Console.WriteLine("Restauration de l'ancienne version...");

            CopyDirectory(backupPath, installPath);

            Console.WriteLine("Restauration terminée.");
        }
        catch (Exception restoreEx)
        {
            Console.WriteLine(
                $"ERREUR DE RESTAURATION : {restoreEx.Message}");
        }
    }
}
finally
{
    // Ne pas supprimer la sauvegarde si une restauration
    // a échoué : elle peut être nécessaire manuellement.
    try
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Nettoyage impossible : {ex.Message}");
    }
}

// 5. Redémarrer l'application
try
{
    string exePath = Path.Combine(
        installPath, "GestionNotes.exe");

    if (File.Exists(exePath))
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = installPath,
            UseShellExecute = true
        });

        Console.WriteLine("GestionNotes redémarré.");
    }
    else
    {
        Console.WriteLine("GestionNotes.exe est introuvable.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Redémarrage impossible : {ex.Message}");
}

// 6. Nettoyage de la sauvegarde après succès uniquement
if (updateSucceeded && Directory.Exists(backupPath))
{
    try
    {
        Directory.Delete(backupPath, true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Nettoyage sauvegarde : {ex.Message}");
    }
}

// Méthode de copie récursive pour éviter les problèmes liés à Directory.Move et aux fichiers en cours d'utilisation.
static void CopyDirectory(string source, string destination)
{
    Directory.CreateDirectory(destination);

    foreach (string file in Directory.GetFiles(source))
    {
        string target = Path.Combine(
            destination,
            Path.GetFileName(file));

        File.Copy(file, target, true);
    }

    foreach (string directory in Directory.GetDirectories(source))
    {
        string target = Path.Combine(
            destination,
            Path.GetFileName(directory));

        CopyDirectory(directory, target);
    }
}