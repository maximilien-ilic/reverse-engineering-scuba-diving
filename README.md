# Reverse engineering scuba diving

Projet d'apprentissage : comprendre le fonctionnement d'une application de plongée
existante, puis en recoder une version à nous, de zéro.

## Démarche

1. **Choisir l'appli cible** (carnet de plongée, planificateur de décompression,
   appli d'ordinateur de plongée…).
2. **L'étudier avec [REA](https://github.com/morluto/rea)**, un outil qui analyse un
   logiciel sans son code source (applis Electron/JavaScript, APK Android, binaires
   natifs via Ghidra, sites web…). On note ce qu'on apprend dans `docs/`.
3. **Recoder chaque fonctionnalité nous-mêmes**, étape par étape, avec des tests.
   On s'inspire du *comportement* observé, sans copier le code ni les ressources
   (logos, textes, images) de l'appli d'origine.

## Organisation

- `docs/` : notes d'analyse, une fiche par étape (01 installateur, 02 architecture, 03 format UDDF).
- `src/MiniDivingLog.Core/` : notre propre code en C# (.NET 10).
  - `Carnet/` : le modèle de données (`Plongee`, `Bloc`, `Profil`).
  - `Calculs/` : Nitrox (MOD, EAD, END) et consommation (SAC).
  - `Calculs/Deco/` : planificateur de décompression Bühlmann ZH-L16C avec facteurs de gradient.
  - `Import/` : lecture des fichiers UDDF exportés par Diving Log, Subsurface…
  - `Stockage/` : le carnet enregistré dans un fichier SQLite (`CarnetSqlite`).
  - `Statistiques/` : records et moyennes du carnet (`StatistiquesCarnet`).
- `src/MiniDivingLog.App/` : l'interface graphique (Avalonia), avec deux onglets :
  - Carnet : liste des plongées, détails, profil dessiné, import UDDF, saisie et modification à la main ;
  - Planificateur : paliers de déco, limite sans palier et profil prévu.
- `tests/MiniDivingLog.Tests/` : les tests xUnit.

> ⚠️ **Projet d'apprentissage.** Les calculs de décompression ne sont pas validés :
> ne jamais s'en servir pour préparer une vraie plongée.

## Lancer les tests

Sur Arch : `sudo pacman -S dotnet-sdk`, puis à la racine du repo :

```
dotnet test
```

## Lancer l'application

```
dotnet run --project src/MiniDivingLog.App
```

Le carnet est enregistré dans `~/.local/share/MiniDivingLog/carnet.db`.
Pour essayer, importe `tests/MiniDivingLog.Tests/Exemples/carnet.uddf`.
