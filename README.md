# Remise_Deploiement
# Auto-Update2026 — GestionNotes

## Table des matières

- [IMPORTANT](#important-instrucation-pour-le-prof)
- [Présentation](#1-présentation-du-projet)
- [Architecture](#2-architecture)
- [Prérquis](#3-prérequis)
- [Configuration réseau](#4-configuration-du-partage-réseau)
- [Publication](#5-publication-dune-nouvelle-version)
- [Tests](#6-tests)
- [Remise](#7-remise)

## IMPORTANT Instrucation pour le prof
1. Se connecter à ton partage SMB depuis son PC.
2. Installer GestionNotes 1.0 dans C:\GestionNotes-Installation.
3. Installer Updater dans C:\GestionNotes-Updater.
4. Vérifier que version.json annonce la version 1.1.
5. Lancer GestionNotes et accepter la mise à jour.
6. Modifier le manifeste sur ta VM pour annoncer la version 2.0, à condition d'avoir les droits d'écriture.
7. Relancer GestionNotes et vérifier la deuxième mise à jour.
Information de connection à la VM : USER: .\Administrateur IP: 10.77.64.44 Partage: C:\GestionNotes-Depot donc \\10.77.64.44\GestionNotes-Depot

## 1. Présentation du projet
Ce laboratoire consiste à développer un mécanisme de mise à jour automatique pour GestionNotes, une application WPF en C# (.NET 10), sans utiliser ClickOnce ni MSIX.

L'application vérifie au démarrage si une version plus récente est disponible, propose à l'utilisateur de l'installer, copie l'archive ZIP localement, puis lance un programme indépendant (Updater.exe) qui remplace les fichiers et redémarre GestionNotes.
Versions utilisées
Version	Description
1.0	Version initiale avec un bogue dans le calcul de la moyenne
1.1	Correction du calcul de la moyenne
2.0	Amélioration de l'interface graphique

Le mécanisme de mise à jour est intégré aux publications utilisées pour permettre les mises à jour successives 1.0 → 1.1 → 2.0.  

## 2. Architecture

Auto-Update2026/
├── GestionNotes/       # Code source de GestionNotes
├── Updater/            # Code source de l'utilitaire de mise à jour
├── Depot/              # Exemple de manifeste et archives ZIP de test
├── Screenshots/        # Captures des tests
├── .gitignore
└── README.md
Le dossier Depot/ versionné dans Git est un ensemble de fichiers de distribution. Il ne remplace pas le partage réseau Windows demandé pour le laboratoire.

## 3. Prérequis

- Windows et Visual Studio avec prise en charge de .NET 10 et de WPF.
- Accès au partage réseau SMB de la machine virtuelle Windows du cégep.
- Droits de lecture sur le partage pour les utilisateurs de GestionNotes ; droits d'écriture pour la personne qui publie les mises à jour.
- Runtime .NET adapté aux exécutables si les applications sont publiées en mode dépendant du framework.

## 4. Configuration du dépôt réseau SMB

Le dépôt de mises à jour est hébergé sur la machine virtuelle Windows du cégep et accessible depuis le poste client avec le chemin UNC :
\\10.77.64.44\GestionNotes-Depot
Le dossier partagé contient le manifeste et les archives de publication :
GestionNotes-Depot/
├── version.json
├── GestionNotes-1.1.zip
└── GestionNotes-2.0.zip


### 4.1 Mise en place du partage

1. Sur la VM Windows, créer le dossier GestionNotes-Depot et activer son partage SMB sous le nom GestionNotes-Depot.
2. Accorder les droits de lecture aux utilisateurs qui doivent installer les mises à jour et les droits de modification au compte qui publie les versions. Vérifier les autorisations de partage et NTFS.
3. Depuis le poste client, ouvrir \\10.77.64.44\GestionNotes-Depot dans l'Explorateur Windows et vérifier l'accès à version.json et aux ZIP.
4. Configurer GestionNotes/Services/UpdateConfig.cs avec le chemin UNC :
   public static string DepotPath = @"\\10.77.64.44\GestionNotes-Depot";
5. Publier les applications avec cette configuration et exécuter les tests depuis le poste client.
L'adresse IP de la VM dépend de l'environnement réseau. Si elle change, adapter le chemin UNC puis republier les applications concernées. Le dépôt Depot/ présent dans Git contient les fichiers à placer dans le partage ; il ne constitue pas lui-même un partage SMB.

### 4.2 Développement local
Pour les essais hors réseau du cégep, le même mécanisme peut être testé avec C:\GestionNotes-Depot en configurant DepotPath en conséquence. Les essais locaux ne remplacent pas la vérification de l'accès SMB.
Manifeste version.json
Le manifeste indique la dernière version proposée et le nom de son archive :
{
  "version": "2.0",
  "fichier": "GestionNotes-2.0.zip"
}
À chaque nouvelle publication, le développeur dépose l'archive correspondante dans le dépôt, puis met à jour ces deux champs. L'application installée lit automatiquement ce manifeste au démarrage.

La version installée est déterminée à partir de l'assembly de GestionNotes et comparée avec System.Version. Le numéro de version est défini dans le fichier .csproj de chaque publication, par exemple :
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion> 

### 4.3 Fonctionnement du mécanisme

1. Vérification : au démarrage, GestionNotes lit version.json avec UpdateService.
2. Comparaison : la version annoncée est comparée à la version installée avec System.Version.
3. Confirmation : si une version plus récente existe, GestionNotes demande l'accord de l'utilisateur. En cas de refus, l'application reste ouverte.
4. Copie locale : si l'utilisateur accepte, l'archive ZIP est copiée depuis le dépôt vers un dossier temporaire. Le code vérifie notamment la présence du fichier et la taille de la copie.
5. Lancement : GestionNotes démarre Updater.exe en lui transmettant le chemin du ZIP local, le dossier d'installation et l'identifiant du processus GestionNotes.
6. Fermeture : GestionNotes appelle Shutdown() ; Updater attend la fin du processus avant de remplacer les fichiers.
7. Installation : Updater extrait le ZIP, sauvegarde l'installation existante, copie les nouveaux fichiers et tente une restauration si une erreur de copie survient.
8. Redémarrage : Updater relance GestionNotes.exe.

Pourquoi utiliser Updater.exe ?

Une application en cours d'exécution ne peut pas remplacer de manière fiable ses propres fichiers exécutables et bibliothèques : certains peuvent être verrouillés par Windows. Le programme auxiliaire attend donc la fermeture de GestionNotes avant de remplacer ses fichiers, puis redémarre l'application.

Pourquoi copier le ZIP localement ?

La copie locale permet de vérifier que l'archive a bien été récupérée avant de fermer GestionNotes. Si le réseau est inaccessible ou si le fichier est absent, l'application reste utilisable. Une fois le ZIP copié, l'installation ne dépend plus de la connexion au dépôt pendant l'extraction et le remplacement.

Limites actuelles

La vérification de la copie repose notamment sur la taille du fichier, pas sur une empreinte cryptographique. La restauration en cas d'échec est une protection de base et non une transaction atomique garantie en toutes circonstances. L'utilitaire Updater est actuellement attendu à un emplacement configuré dans le code (C:\GestionNotes-Updater\Updater.exe) : cet emplacement doit exister sur la machine de test.

## 5. Publier une nouvelle version

1. Ouvrir le projet source de la nouvelle version dans Visual Studio.
2. Conserver les services de mise à jour et la logique de démarrage dans cette version.
3. Définir Version, AssemblyVersion et FileVersion dans le .csproj (ex. 2.1.0, 2.1.0.0).
4. Vérifier que UpdateConfig.DepotPath pointe vers le dépôt prévu pour les clients.
5. Compiler et publier GestionNotes en configuration Release, avec une cible Windows compatible.
6. Compresser le contenu du dossier de publication dans GestionNotes-2.1.zip : GestionNotes.exe doit se trouver directement à la racine du ZIP.
7. Copier le ZIP dans le dossier de dépôt accessible aux clients.
8. Mettre à jour version.json :
   {
     "version": "2.1",
     "fichier": "GestionNotes-2.1.zip"
   }
9. Lancer une version antérieure de GestionNotes pour vérifier la détection et l'installation.
Il n'est pas nécessaire de modifier manuellement l'installation de chaque utilisateur. En revanche, chaque nouvelle publication doit continuer à inclure le mécanisme de mise à jour.

## 6. Tests

### 6.1 Installer et reproduire les tests

1. Publier une version de départ (par exemple GestionNotes 1.0) dans un dossier distinct, tel que C:\GestionNotes-Installation.
2. Publier le projet Updater dans C:\GestionNotes-Updater et vérifier la présence de Updater.exe.
3. Accéder au partage réseau \\10.77.64.44\GestionNotes-Depot depuis le poste client et vérifier les permissions de lecture.
4. Y placer l'archive GestionNotes-1.1.zip et un manifeste annonçant 1.1.
5. Lancer C:\GestionNotes-Installation\GestionNotes.exe, accepter la mise à jour et vérifier le redémarrage en version 1.1.
6. Remplacer le manifeste pour annoncer 2.0, déposer GestionNotes-2.0.zip, puis vérifier le passage de 1.1 à 2.0.
7. Relancer GestionNotes v2.0 avec un manifeste annonçant 2.0 : aucune nouvelle mise à jour ne doit être proposée.
Pour refaire le parcours complet, il faut disposer d'une publication de départ en version 1.0 ou 1.1. Le seul code source v2.0 ne suffit pas à reconstruire les anciennes versions à l'identique.

## 6.2 Scénarios de validation

Scénario	        ;            Résultat observé

Mise à jour 1.0 → 1.1   ;   Installation et redémarrage réussis

Mise à jour 1.1 → 2.0   ;   Installation et redémarrage réussis

Version déjà à jour ;   Aucune mise à jour proposée

Dépôt inaccessible  ;   Erreur affichée ; GestionNotes reste utilisable

Manifeste invalide ou absent    ;   Erreur gérée sans fermeture forcée

Archive ZIP absente	Erreur affichée ; GestionNotes reste utilisable

Mise à jour refusée ;   GestionNotes continue de fonctionner


Les résultats du tableau proviennent des essais fonctionnels effectués avec un dépôt local. La procédure ci-dessus permet de reproduire les mêmes scénarios depuis le partage SMB du cégep ; la validation réseau doit être constatée sur place.


## 7. Remise
Le dépôt Git contient le code source de GestionNotes et d'Updater, le manifeste, les archives de distribution nécessaires aux tests, les captures d'écran et ce README. Les dossiers générés (bin/, obj/, .vs/) ne doivent pas être versionnés.

Pour la démonstration, utiliser le partage SMB indiqué dans la section de configuration et vérifier les autorisations, le téléchargement, l’installation et le redémarrage sur le poste client.