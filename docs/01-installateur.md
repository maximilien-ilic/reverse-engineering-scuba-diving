# Fiche 01 : l'installateur de Diving Log 6.0

Fichier étudié : `Divinglog60.exe` (41,6 Mo)
SHA-256 : `923d10cc76c23a8df8c084f89b373aa322aaffeb8f9ce6235b84cc02dabd340a`

Analyse **statique** uniquement : le fichier n'a jamais été exécuté.

## 1. Quel type de fichier ?

`file` répond : `PE32 executable (GUI) Intel 80386, for MS Windows`.
C'est un exécutable Windows 32 bits. Un PE est découpé en *sections* :

| Section | Taille | Rôle |
|---|---|---|
| `.text` | 171 Ko | le code machine du programme d'installation |
| `.rdata` / `.data` | 23 Ko | constantes et variables |
| `.rsrc` | 19 Ko | ressources (icônes, boîtes de dialogue) |

Les sections s'arrêtent à l'octet `0x35600` (≈ 218 Ko), alors que le fichier fait
41,6 Mo. Les ~41 Mo restants sont un **overlay** : des données collées à la fin de
l'exe, que Windows ne charge pas lui-même. C'est là qu'est rangée l'appli.

## 2. Qui a fabriqué l'installateur ?

`strings` (et `strings -el` pour l'UTF-16 de Windows) trouve :

- `Astrum Installer` / `Astrum InstallWizard`, version `2.29.60` : un outil
  d'installation commercial peu courant (pas Inno Setup ni NSIS, donc pas
  d'extracteur tout fait comme `innoextract`).
- `SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full` : l'installateur vérifie
  que **.NET Framework 4** est présent. Indice fort : Diving Log est une appli
  **.NET**, donc décompilable presque jusqu'au code source (C#), et REA sait
  l'analyser (« .NET assemblies »).

## 3. Le début de l'overlay est brouillé

Les premiers octets de l'overlay ressemblent à
`b8 da fc 1e 40 62 84 a6 c8 ea …` : chaque octet vaut le précédent **+ 0x22**.
Quand des données « normales » contiennent beaucoup de zéros, ce motif trahit un
chiffrement très simple : `octet_chiffré = octet_clair + clé(i)` avec
`clé(i) = (0x52 + 0x22 × i) mod 256`.

En soustrayant cette clé, l'en-tête devient lisible : clé de registre
`Software\Divinglog\Diving Log 6.0`, dossier `<ProgramFiles>\Diving Log 6.0`, et
la liste des fichiers installés :

- `Divelog.exe` : l'application elle-même
- `FileAssociation.exe` : associe les extensions de fichiers à l'appli
- `Uninstaller.exe`
- `cmll1501.chm` : aide du « Report Designer » (composant List & Label, pour
  imprimer le carnet)

## 4. Ce qui bloque

Après l'en-tête (vers `+0x7ED`), les données ne sont plus brouillées mais
**compressées** avec un format propre à Astrum (ni zip, ni gzip, ni LZMA
reconnus). Écrire un décompresseur demanderait de désassembler le code de
l'installateur : faisable, mais long, et ce n'est pas notre objectif.

**Solution choisie** : installer l'appli normalement avec Wine sur le PC, puis
analyser directement `Divelog.exe`.

## Ce qu'on a appris

- Lire la structure d'un PE et repérer un overlay.
- Identifier un installateur avec `strings`.
- Repérer et casser un chiffrement additif à pas constant.
- Savoir s'arrêter quand le coût dépasse l'intérêt.
