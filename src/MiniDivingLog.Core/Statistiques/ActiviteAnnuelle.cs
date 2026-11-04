using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Core.Statistiques;

/// <summary>Une case de la grille : un jour, son nombre de plongées et son niveau de couleur (0 à 4).</summary>
public record JourActivite(DateOnly Date, int Nombre, int Niveau);

/// <summary>
/// La grille d'activité « façon GitHub » : une colonne par semaine (du lundi au dimanche),
/// une case par jour, sur l'année qui se termine à la date donnée.
/// Le niveau de couleur de chaque case est relatif au jour le plus chargé.
/// </summary>
public class ActiviteAnnuelle
{
    public const int NombreNiveaux = 5;

    /// <summary>Les semaines, de la plus ancienne à la plus récente ; chacune a 7 jours (lundi → dimanche).</summary>
    public IReadOnlyList<IReadOnlyList<JourActivite>> Semaines { get; }

    public DateOnly Debut { get; }
    public DateOnly Fin { get; }
    public int Total { get; }
    public int MaxParJour { get; }

    /// <param name="maxCommun">
    /// Pour comparer plusieurs grilles, passe-leur le même maximum : leurs couleurs seront comparables.
    /// </param>
    public ActiviteAnnuelle(IEnumerable<Plongee> plongees, DateOnly fin, int? maxCommun = null)
    {
        Fin = fin;
        // On remonte d'un an, puis jusqu'au lundi précédent pour commencer une semaine complète.
        DateOnly unAnAvant = fin.AddYears(-1).AddDays(1);
        Debut = unAnAvant.AddDays(-JourDeSemaine(unAnAvant));

        var parJour = plongees
            .Select(p => DateOnly.FromDateTime(p.Date))
            .Where(d => d >= unAnAvant && d <= fin)
            .GroupBy(d => d)
            .ToDictionary(g => g.Key, g => g.Count());

        Total = parJour.Values.Sum();
        MaxParJour = maxCommun ?? (parJour.Count == 0 ? 0 : parJour.Values.Max());

        var semaines = new List<IReadOnlyList<JourActivite>>();
        for (DateOnly lundi = Debut; lundi <= fin; lundi = lundi.AddDays(7))
        {
            semaines.Add(Enumerable.Range(0, 7)
                .Select(i => lundi.AddDays(i))
                .Select(d => new JourActivite(d, parJour.GetValueOrDefault(d), Niveau(parJour.GetValueOrDefault(d), MaxParJour)))
                .ToList());
        }
        Semaines = semaines;
    }

    /// <summary>
    /// 0 = aucune plongée ; sinon 1 à 4 selon la part du maximum
    /// (1 = jusqu'à 25 % du jour le plus chargé, 4 = plus de 75 %).
    /// </summary>
    public static int Niveau(int nombre, int max)
    {
        if (nombre <= 0 || max <= 0)
            return 0;
        return Math.Clamp((int)Math.Ceiling(4.0 * nombre / max), 1, NombreNiveaux - 1);
    }

    /// <summary>Lundi = 0 … dimanche = 6 (la semaine française, pas l'américaine).</summary>
    public static int JourDeSemaine(DateOnly d) => ((int)d.DayOfWeek + 6) % 7;
}
