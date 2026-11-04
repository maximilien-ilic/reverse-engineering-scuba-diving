using System.Globalization;
using System.Xml.Linq;
using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Core.Import;

/// <summary>Une plongée lue dans un fichier, avec son profil s'il y en a un.</summary>
public record PlongeeImportee(Plongee Plongee, Profil? Profil);

/// <summary>
/// Lit un fichier UDDF (Universal Dive Data Format), le format XML d'échange
/// que savent exporter Diving Log, Subsurface et la plupart des carnets.
///
/// Particularité du format : tout est en unités SI.
/// Pressions en pascals, températures en kelvins, volumes en m³, durées en secondes.
///
/// Les éléments se référencent par identifiant : un bloc pointe vers son mélange
/// (&lt;link ref="mix1"/&gt;), une plongée vers son site, etc.
/// </summary>
public static class ImportUddf
{
    public static List<PlongeeImportee> LireFichier(string chemin) => Lire(XDocument.Load(chemin));

    public static List<PlongeeImportee> LireTexte(string xml) => Lire(XDocument.Parse(xml));

    private static List<PlongeeImportee> Lire(XDocument doc)
    {
        XElement racine = doc.Root ?? throw new FormatException("Fichier UDDF vide.");
        if (racine.Name.LocalName != "uddf")
            throw new FormatException($"Ce n'est pas un fichier UDDF (racine <{racine.Name.LocalName}>).");

        // Les index « id → valeur » pour résoudre les <link ref="..."/>.
        var melanges = Elements(racine, "gasdefinitions", "mix").ToDictionary(
            m => (string)m.Attribute("id")!,
            m => (O2: Nombre(m, "o2") ?? 0.21, He: Nombre(m, "he") ?? 0));

        var sites = Elements(racine, "divesite", "site").ToDictionary(
            s => (string)s.Attribute("id")!,
            s => Texte(s, "name") ?? "");

        var resultat = new List<PlongeeImportee>();
        int numeroParDefaut = 1;

        foreach (XElement plongee in racine.Descendants().Where(e => e.Name.LocalName == "dive"))
        {
            XElement? avant = Enfant(plongee, "informationbeforedive");
            XElement? apres = Enfant(plongee, "informationafterdive");

            string? refSite = Liens(avant).FirstOrDefault(sites.ContainsKey);
            Profil? profil = LireProfil(plongee);

            double dureeMin = Nombre(apres, "diveduration") is double s
                ? s / 60
                : profil?.Duree.TotalMinutes ?? 0;

            var p = new Plongee
            {
                Numero = (int?)Nombre(avant, "divenumber") ?? numeroParDefaut,
                Date = DateTime.Parse(Texte(avant, "datetime") ?? throw new FormatException("Plongée sans <datetime>."),
                                      CultureInfo.InvariantCulture),
                Lieu = refSite is null ? "" : sites[refSite],
                DureeMin = dureeMin,
                ProfondeurMaxM = Nombre(apres, "greatestdepth") ?? profil?.ProfondeurMax ?? 0,
                ProfondeurMoyM = Nombre(apres, "averagedepth") ?? profil?.ProfondeurMoyenne ?? 0,
                TemperatureEauC = KelvinVersCelsius(Nombre(apres, "lowesttemperature")) ?? profil?.TemperatureMin,
                Commentaires = Texte(Enfant(apres, "notes"), "para") ?? "",
                Blocs = plongee.Elements().Where(e => e.Name.LocalName == "tankdata")
                               .Select(t => LireBloc(t, melanges)).ToList(),
            };

            resultat.Add(new PlongeeImportee(p, profil));
            numeroParDefaut = p.Numero + 1;
        }

        return resultat;
    }

    private static Bloc LireBloc(XElement tank, Dictionary<string, (double O2, double He)> melanges)
    {
        var (o2, he) = Liens(tank).Where(melanges.ContainsKey).Select(r => melanges[r])
                                  .DefaultIfEmpty((0.21, 0)).First();

        return new Bloc(
            VolumeL: (Nombre(tank, "tankvolume") ?? 0) * 1000,                 // m³ → litres
            PressionDebutBar: (Nombre(tank, "tankpressurebegin") ?? 0) / 1e5,  // Pa → bar
            PressionFinBar: (Nombre(tank, "tankpressureend") ?? 0) / 1e5,
            FractionO2: o2,
            FractionHe: he);
    }

    private static Profil? LireProfil(XElement plongee)
    {
        var points = Elements(plongee, "samples", "waypoint")
            .Where(w => Nombre(w, "divetime") is not null && Nombre(w, "depth") is not null)
            .Select(w => new PointProfil(
                TimeSpan.FromSeconds(Nombre(w, "divetime")!.Value),
                Nombre(w, "depth")!.Value,
                KelvinVersCelsius(Nombre(w, "temperature"))))
            .ToList();

        return points.Count == 0 ? null : new Profil(points);
    }

    private static double? KelvinVersCelsius(double? kelvin) =>
        kelvin is double k ? Math.Round(k - 273.15, 2) : null;

    // Petits utilitaires : on compare les noms locaux pour accepter
    // toutes les versions du format (3.0, 3.1, 3.2…), qui changent d'espace de noms.

    private static XElement? Enfant(XElement? parent, string nom) =>
        parent?.Elements().FirstOrDefault(e => e.Name.LocalName == nom);

    private static IEnumerable<XElement> Elements(XElement parent, string conteneur, string nom) =>
        parent.Elements().Where(e => e.Name.LocalName == conteneur)
              .SelectMany(c => c.Elements().Where(e => e.Name.LocalName == nom));

    private static IEnumerable<string> Liens(XElement? parent) =>
        parent?.Elements().Where(e => e.Name.LocalName == "link")
              .Select(l => (string?)l.Attribute("ref")).OfType<string>() ?? [];

    private static string? Texte(XElement? parent, string nom) => Enfant(parent, nom)?.Value.Trim();

    private static double? Nombre(XElement? parent, string nom) =>
        Texte(parent, nom) is string t ? double.Parse(t, CultureInfo.InvariantCulture) : null;
}
