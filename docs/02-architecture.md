# Fiche 02 : l'architecture de Diving Log 6.0

Analyse **statique** du dossier installé (via Wine), avec REA 6.8.0 :
`rea inspect-managed-artifact` puis `rea inspect-managed-members`.
Aucun code de Diving Log n'a été exécuté ni recopié ici : seulement des **noms**
(types, champs, méthodes) lus dans les métadonnées .NET.

## 1. Ce qu'il y a dans le dossier (85 fichiers, 125 Mo)

| Fichier(s) | Rôle |
|---|---|
| `Divelog.exe` (6,8 Mo) | **l'application**, en .NET Framework 4 (CIL pur, x86) |
| `DevExpress.*.dll` | bibliothèque commerciale d'interface WPF (grilles, ruban, graphiques) |
| `System.Data.SQLite.dll`, `dao360.dll`, `Units.mdb` | bases de données : SQLite et Microsoft Access |
| `libdivecomputer-0.dll`, `libusb`, `libhidapi` | dialogue avec les ordinateurs de plongée (**libdivecomputer est open source, la même que Subsurface**) |
| `GMap.NET.*` | cartes des sites de plongée |
| `Fit.dll` | format FIT de Garmin |
| `combit.ListLabel15.dll`, `cm*.dll` | impression du carnet (List & Label) |
| `Newtonsoft.Json.dll`, `Ionic.Zip`, `vCard`, `ThoughtWorks.QRCode` | utilitaires classiques |

Leçon : une grosse appli « fermée » est souvent **un petit cœur maison + beaucoup
de bibliothèques connues**. Le travail original est dans `Divelog.exe`.

## 2. Identité de `Divelog.exe` (inspect-managed-artifact)

- Format PE32, en-tête CLI présent : c'est du **CIL** (bytecode .NET), pas du code
  machine. On peut donc retrouver les noms de toutes les classes et méthodes.
- Runtime .NET Framework (v4.0.30319), 32 bits obligatoire.
- 1 193 types, 17 701 méthodes, 8 566 champs : une grosse appli.
- Pas d'obfuscation : les noms sont lisibles (`Divinglog.Logbook.Dive`…).

## 3. Les namespaces (= les grands modules)

| Namespace | Types | Rôle deviné d'après les noms |
|---|---|---|
| `Divinglog.Logbook` | 36 | **le modèle de données** : Dive, Tank, Profile, Buddy, Location, Equipment… |
| `Divinglog.Calculations` (+ `.Deco`) | 11 | **les calculs** : Nitrox, SAC, Deco (ZHL16), unités |
| `Divinglog.Import` | 73 | un importeur par logiciel concurrent (Aladin, APD, Suunto…) |
| `Divinglog.Export` (+ `.UDDF32`) | 179 | exports : CSV, Excel, HTML, SQLite, UDDF (format XML standard de plongée) |
| `Divinglog.Download` | 5 | téléchargement depuis l'ordinateur de plongée (OSTC, stockage USB) |
| `Divinglog.Statistics` | 18 | graphiques et statistiques |
| `Divinglog.Sync` | 29 | synchronisation (mobile, DAN) |
| `Divinglog` (racine) | 66 | les fenêtres `frm*` (frmMain, frmLogbook, frmDeco…) et `DBConn` |

C'est une architecture classique **modèle / calculs / entrées-sorties / interface**.

## 4. Zoom sur quelques classes

### `Divinglog.Logbook.Dive` : ce qu'est une plongée
74 champs, entre autres : `Number`, `Divedate`, `Entrytime`, `Surfint`
(intervalle surface), `Divetime`, `Depth`, `DepthAvg`, `Country/City/Place`,
`Buddy`, `Water`, `Entry`, `Divetype`, `Tanktype`, `Tanksize`, `PresS`/`PresE`
(pression début/fin), `O2`, `He`, `Airtemp`, `Watertemp`, `Weight`, `Visibility`,
`Deco`, `Decostops`, `CNS`, `Profile` (+ Profile2…5), `TankList`, `UtcOffset`,
`NoFlyTime`, `DesaturationTime`.

### `Divinglog.Calculations.Nitrox`
Entrées : `PO2`, `FO2`, `Depth`. Résultats : `MOD` (profondeur max), `BestMix`,
`EAD` (profondeur air équivalente), `PO2`. Méthodes `GetMOD`, `GetEAD`, `GetEND`.
Ce sont des formules publiques de plongée qu'on peut réécrire nous-mêmes.

### `Divinglog.Calculations.SAC`
`GetSAC`, `GetMetricSAC`, `GetImperialSAC`, `GetMultipleTankSAC` : la
consommation d'air en surface (litres/min ramenés à 1 bar).

### `Divinglog.Calculations.Deco.ZHL16`
Champs `an2`, `bn2`, `ahe`, `bhe`, `halftn`, `halfth`, `M0n2`, `M0he` : ce sont
exactement les coefficients a/b et les demi-vies azote/hélium du **modèle de
Bühlmann ZH-L16**, publié dans des livres de plongée. `DecoCalc`, `DecoPlan` et
`DivePlan` s'en servent pour planifier les paliers.

### `Divinglog.DBConn`
Gère **4 moteurs de base** : Access (OLE DB), MySQL, SQL Server et SQLite
(`m_ConnStrOle`, `m_ConnStrMySql`, `m_ConnStrSql`, `m_ConnStrSqlite`), avec
`OpenDB`, `LoadData`, `UpdateData`, `GetNextID`, `Compact`…

## 5. Limites de cette analyse

- On a les **noms et signatures**, pas encore le contenu des méthodes.
  REA a refusé l'inventaire complet de l'exe d'un coup (métadonnées trop
  grosses) ; il faut l'interroger type par type (`--type`).
- Les noms suggèrent le rôle, ils ne le prouvent pas.

## 6. Ce qu'on va recoder (notre « mini Diving Log »)

1. Le modèle `Dive` / `Tank` (simplifié) + stockage **SQLite**.
2. Les calculs **Nitrox** (MOD, EAD, END) et **SAC**, avec des tests.
3. Le profil de plongée et des statistiques simples.
4. Un planificateur de déco **Bühlmann ZH-L16**, à partir des publications.
5. Plus tard : import UDDF, interface.
