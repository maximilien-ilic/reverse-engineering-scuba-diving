# Fiche 03 : le format UDDF

## Ce que REA nous a appris

Dans `Divelog.exe`, le namespace `Divinglog.Export.UDDF32` contient **163 types**
(`waypointType`, `tankdataType`, `informationafterdiveType`, `mixType`…).
Leurs champs suivent toujours le même motif :

```
depthField   depthFieldSpecified
divetimeField   divetimeFieldSpecified
tankpressurebeginField   tankvolumeField   tankvolumeFieldSpecified
```

Ce motif `xxxField` + `xxxFieldSpecified` est la signature de **`xsd.exe`**, l'outil
Microsoft qui génère des classes C# à partir d'un schéma XML (`.xsd`). Conclusion :
Diving Log n'a pas écrit son export UDDF à la main, il a généré ces classes depuis
le schéma officiel UDDF 3.2. Les noms des champs sont donc directement les noms des
balises XML.

On a aussi `Divinglog.Import.UDDF` (la lecture) et `eUDDFVersion` (plusieurs
versions du format gérées).

## Le format

UDDF (Universal Dive Data Format) est un format XML ouvert, documenté publiquement,
que lisent et écrivent Diving Log, Subsurface et beaucoup d'ordinateurs de plongée.

```
uddf
├── gasdefinitions / mix (id)       → o2, he
├── divesite / site (id)            → name
└── profiledata / repetitiongroup
    └── dive
        ├── informationbeforedive   → link(site), divenumber, datetime
        ├── tankdata                → link(mix), tankvolume, tankpressurebegin/end
        ├── samples / waypoint      → divetime, depth, temperature
        └── informationafterdive    → greatestdepth, averagedepth, diveduration,
                                      lowesttemperature, notes/para
```

Deux pièges :

1. **Unités SI partout** : pressions en pascals (200 bar = 20 000 000 Pa),
   températures en kelvins (19 °C = 292,15 K), volumes en m³ (12 L = 0,012),
   durées en secondes.
2. **Références par identifiant** : un bloc ne contient pas son mélange, il le
   désigne par `<link ref="ean32"/>`. Il faut d'abord indexer les `mix` et les
   `site`, puis résoudre les liens.

## Notre implémentation

`src/MiniDivingLog.Core/Import/ImportUddf.cs` lit le fichier avec `System.Xml.Linq`,
compare les noms **locaux** des balises (pour accepter les versions 3.0, 3.1, 3.2,
qui changent d'espace de noms), convertit les unités et, s'il manque des infos
(durée, profondeur moyenne…), les recalcule à partir du profil.
