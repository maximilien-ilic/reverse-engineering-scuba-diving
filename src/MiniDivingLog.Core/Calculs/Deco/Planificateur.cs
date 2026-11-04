using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Core.Calculs.Deco;

/// <summary>Un palier de décompression.</summary>
public record Palier(double ProfondeurM, int DureeMin);

/// <summary>Le résultat d'une planification.</summary>
/// <param name="Deroulement">Le profil prévu (descente, fond, remontée, paliers), pour le dessiner.</param>
public record PlanDeco(IReadOnlyList<Palier> Paliers, double DureeTotaleMin, Profil Deroulement)
{
    public bool SansPalier => Paliers.Count == 0;
}

/// <summary>
/// Planificateur Bühlmann ZH-L16C avec facteurs de gradient (GF bas / GF haut).
/// OUTIL D'APPRENTISSAGE : ne jamais l'utiliser pour préparer une vraie plongée.
/// </summary>
public class Planificateur
{
    public double GfBas { get; init; } = 0.30;
    public double GfHaut { get; init; } = 0.70;
    public double VitesseDescente { get; init; } = 18;  // m/min
    public double VitesseRemontee { get; init; } = 9;   // m/min
    public double EcartPaliers { get; init; } = 3;      // m

    /// <summary>
    /// Planifie une plongée « carrée » : descente, fond (durée comptée depuis le début),
    /// puis remontée avec les paliers nécessaires.
    /// </summary>
    public PlanDeco Planifier(double profondeurM, double dureeFondMin, Melange gaz)
    {
        var tissus = new Tissus();
        double tempsDescente = profondeurM / VitesseDescente;
        if (dureeFondMin < tempsDescente)
            throw new ArgumentException("La durée de fond doit inclure la descente.", nameof(dureeFondMin));

        tissus.Segment(0, profondeurM, tempsDescente, gaz);
        tissus.Segment(profondeurM, profondeurM, dureeFondMin - tempsDescente, gaz);

        // On note chaque changement de profondeur pour pouvoir dessiner le plan.
        var deroulement = new List<PointProfil> { Point(0, 0) };
        if (tempsDescente > 0)
            deroulement.Add(Point(tempsDescente, profondeurM));
        if (dureeFondMin > tempsDescente)
            deroulement.Add(Point(dureeFondMin, profondeurM));

        // Premier palier : le plafond calculé avec le GF bas, arrondi au multiple de 3 m supérieur.
        double premierPalier = ArrondirAuPalier(tissus.Plafond(GfBas));
        var paliers = new List<Palier>();
        double temps = dureeFondMin;
        double profondeur = profondeurM;

        while (profondeur > 0)
        {
            double cible = Math.Max(0, profondeur <= premierPalier ? profondeur - EcartPaliers : premierPalier);
            if (premierPalier == 0)
                cible = 0;

            // Si on est à un palier, on y attend minute par minute jusqu'à pouvoir monter au suivant.
            if (profondeur <= premierPalier && premierPalier > 0)
            {
                int attente = 0;
                while (tissus.Plafond(Gf(cible, premierPalier)) > cible + 1e-9)
                {
                    tissus.Segment(profondeur, profondeur, 1, gaz);
                    attente++;
                    if (attente > 999)
                        throw new InvalidOperationException("Décompression trop longue pour ce modèle simplifié.");
                }
                if (attente > 0)
                {
                    paliers.Add(new Palier(profondeur, attente));
                    temps += attente;
                    deroulement.Add(Point(temps, profondeur));
                }
            }

            double dureeRemontee = (profondeur - cible) / VitesseRemontee;
            tissus.Segment(profondeur, cible, dureeRemontee, gaz);
            temps += dureeRemontee;
            profondeur = cible;
            deroulement.Add(Point(temps, profondeur));
        }

        return new PlanDeco(paliers, temps, new Profil(deroulement));
    }

    /// <summary>
    /// Limite de non-décompression (min) : le temps de fond maximal pour remonter
    /// directement en surface, en respectant le GF haut.
    /// </summary>
    public int LimiteSansPalier(double profondeurM, Melange gaz)
    {
        var tissus = new Tissus();
        double tempsDescente = profondeurM / VitesseDescente;
        tissus.Segment(0, profondeurM, tempsDescente, gaz);
        if (tissus.Plafond(GfHaut) > 0)
            return 0;

        int minutes = 0;
        while (minutes < 999)
        {
            var essai = tissus.Copier();
            essai.Segment(profondeurM, profondeurM, 1, gaz);
            if (essai.Plafond(GfHaut) > 0)
                break;
            tissus = essai;
            minutes++;
        }
        return (int)Math.Floor(tempsDescente) + minutes;
    }

    private static PointProfil Point(double minutes, double profondeurM) =>
        new(TimeSpan.FromMinutes(minutes), profondeurM);

    /// <summary>Le GF varie linéairement : GF bas au premier palier, GF haut en surface.</summary>
    private double Gf(double profondeurM, double premierPalierM) =>
        premierPalierM <= 0 ? GfHaut : GfHaut + (GfBas - GfHaut) * profondeurM / premierPalierM;

    private double ArrondirAuPalier(double plafondM) =>
        plafondM <= 0 ? 0 : Math.Ceiling(plafondM / EcartPaliers) * EcartPaliers;
}
