namespace MiniDivingLog.Core.Carnet;

/// <summary>Un échantillon enregistré par l'ordinateur de plongée.</summary>
public record PointProfil(TimeSpan Temps, double ProfondeurM, double? TemperatureC = null);

/// <summary>Une remontée trop rapide entre deux échantillons.</summary>
public record RemonteeRapide(TimeSpan Debut, TimeSpan Fin, double VitesseMParMin);

/// <summary>
/// Le profil d'une plongée : la courbe profondeur / temps.
/// Les points doivent être triés par temps croissant.
/// </summary>
public class Profil
{
    public IReadOnlyList<PointProfil> Points { get; }

    public Profil(IEnumerable<PointProfil> points)
    {
        Points = points.ToList();
        for (int i = 1; i < Points.Count; i++)
            if (Points[i].Temps <= Points[i - 1].Temps)
                throw new ArgumentException("Les points doivent être triés par temps strictement croissant.", nameof(points));
    }

    public TimeSpan Duree => Points.Count == 0 ? TimeSpan.Zero : Points[^1].Temps - Points[0].Temps;

    public double ProfondeurMax => Points.Count == 0 ? 0 : Points.Max(p => p.ProfondeurM);

    /// <summary>
    /// Profondeur moyenne pondérée par le temps : l'aire sous la courbe divisée par la durée.
    /// Entre deux points, on suppose une variation linéaire (méthode des trapèzes).
    /// </summary>
    public double ProfondeurMoyenne
    {
        get
        {
            if (Points.Count < 2)
                return Points.Count == 1 ? Points[0].ProfondeurM : 0;

            double aire = 0;
            for (int i = 1; i < Points.Count; i++)
            {
                double dt = (Points[i].Temps - Points[i - 1].Temps).TotalMinutes;
                aire += (Points[i].ProfondeurM + Points[i - 1].ProfondeurM) / 2 * dt;
            }
            return aire / Duree.TotalMinutes;
        }
    }

    public double? TemperatureMin => Points.Where(p => p.TemperatureC.HasValue).Min(p => p.TemperatureC);

    /// <summary>Vitesse de remontée (m/min) entre chaque paire de points ; positive quand on remonte.</summary>
    public IEnumerable<(TimeSpan Debut, TimeSpan Fin, double Vitesse)> VitessesRemontee()
    {
        for (int i = 1; i < Points.Count; i++)
        {
            double dt = (Points[i].Temps - Points[i - 1].Temps).TotalMinutes;
            yield return (Points[i - 1].Temps, Points[i].Temps,
                          (Points[i - 1].ProfondeurM - Points[i].ProfondeurM) / dt);
        }
    }

    public double VitesseRemonteeMax =>
        VitessesRemontee().Select(v => v.Vitesse).DefaultIfEmpty(0).Max();

    /// <summary>Les passages où l'on remonte plus vite que la limite (10 m/min par défaut).</summary>
    public List<RemonteeRapide> RemonteesRapides(double limiteMParMin = 10) =>
        VitessesRemontee()
            .Where(v => v.Vitesse > limiteMParMin)
            .Select(v => new RemonteeRapide(v.Debut, v.Fin, v.Vitesse))
            .ToList();
}
