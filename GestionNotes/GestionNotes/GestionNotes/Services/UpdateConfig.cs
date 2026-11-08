using System;
using System.Collections.Generic;
using System.Text;

namespace GestionNotes.Services
{
    public static class UpdateConfig
    {
        // À remplacer au cegep par le chemin du dépôt de l'application sur le réseau du cégep
        // Au cégep : @"\\10.77.64.44\GestionNotes-Depot"
        public static string DepotPath =@"C:\GestionNotes-Depot";

        public static Version CurrentVersion => typeof(App).Assembly.GetName().Version ?? new Version(1, 0);
    }
}
