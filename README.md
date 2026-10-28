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

- `docs/` : notes d'analyse, une fiche par fonctionnalité étudiée.
- `src/` : notre propre code (à venir).
