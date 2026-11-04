using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Core.Statistiques;

/// <summary>Une valeur record et le numéro de la plongée où elle a été atteinte.</summary>
public record RecordCarnet(double Valeur, int NumeroPlongee);

/// <summary>
/// Statistiques générales du carnet, sur le modèle de Divinglog.Statistics.General
/// (nombre de plongées, temps total, records min/max « at » telle plongée, SAC…).
/// </summary>
public class StatistiquesCarnet
{
    private readonly IReadOnlyList<Plongee> _plongees;

    public StatistiquesCarnet(IEnumerable<Plongee> plongees) => _plongees = plongees.ToList();

    public int Nombre => _plongees.Count;

    public TimeSpan TempsTotal => TimeSpan.FromMinutes(_plongees.Sum(p => p.DureeMin));

    public double? DureeMoyenneMin => _plongees.Count == 0 ? null : _plongees.Average(p => p.DureeMin);

    public RecordCarnet? ProfondeurMax => Max(p => p.ProfondeurMaxM);

    public RecordCarnet? PlusLonguePlongee => Max(p => p.DureeMin);

    public RecordCarnet? EauLaPlusFroide => Min(p => p.TemperatureEauC);

    public RecordCarnet? EauLaPlusChaude => Max(p => p.TemperatureEauC);

    /// <summary>SAC moyen sur les plongées qui ont au moins un bloc.</summary>
    public double? SacMoyen
    {
        get
        {
            var sacs = _plongees.Select(p => p.CalculerSac()).OfType<double>().ToList();
            return sacs.Count == 0 ? null : sacs.Average();
        }
    }

    public Plongee? Premiere => _plongees.MinBy(p => p.Date);

    public Plongee? Derniere => _plongees.MaxBy(p => p.Date);

    /// <summary>Nombre de plongées par lieu, du plus fréquent au moins fréquent.</summary>
    public List<(string Lieu, int Nombre)> ParLieu() =>
        _plongees.GroupBy(p => p.Lieu)
                 .Select(g => (g.Key, g.Count()))
                 .OrderByDescending(x => x.Item2)
                 .ThenBy(x => x.Key)
                 .ToList();

    private RecordCarnet? Max(Func<Plongee, double?> valeur) => Choisir(valeur, (a, b) => a > b);

    private RecordCarnet? Min(Func<Plongee, double?> valeur) => Choisir(valeur, (a, b) => a < b);

    private RecordCarnet? Choisir(Func<Plongee, double?> valeur, Func<double, double, bool> meilleur)
    {
        RecordCarnet? record = null;
        foreach (var p in _plongees)
        {
            if (valeur(p) is not double v)
                continue;
            if (record is null || meilleur(v, record.Valeur))
                record = new RecordCarnet(v, p.Numero);
        }
        return record;
    }
}
