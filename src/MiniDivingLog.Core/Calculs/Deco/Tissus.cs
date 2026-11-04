namespace MiniDivingLog.Core.Calculs.Deco;

/// <summary>
/// L'état des 16 compartiments (« tissus ») : la tension d'azote et d'hélium dissous dans chacun.
/// Les pressions sont en bar ; 10 m d'eau = 1 bar.
/// </summary>
public class Tissus
{
    /// <summary>Pression atmosphérique au niveau de la mer.</summary>
    public const double PressionSurface = 1.01325;

    /// <summary>Vapeur d'eau dans les poumons (37 °C), à retirer de la pression inspirée.</summary>
    public const double VapeurEau = 0.0627;

    public double[] TensionN2 { get; } = new double[ZHL16C.NombreCompartiments];
    public double[] TensionHe { get; } = new double[ZHL16C.NombreCompartiments];

    /// <summary>Tissus saturés à l'air en surface : l'état d'un plongeur qui n'a pas plongé.</summary>
    public Tissus()
    {
        double n2Surface = (PressionSurface - VapeurEau) * Melange.Air.FractionN2;
        Array.Fill(TensionN2, n2Surface);
    }

    public Tissus Copier()
    {
        var copie = new Tissus();
        TensionN2.CopyTo(copie.TensionN2, 0);
        TensionHe.CopyTo(copie.TensionHe, 0);
        return copie;
    }

    public static double PressionAmbiante(double profondeurM) => PressionSurface + profondeurM / 10;

    /// <summary>
    /// Fait évoluer les tissus pendant un segment où la profondeur varie linéairement
    /// (équation de Schreiner). Si la profondeur est constante, cela redonne l'équation de Haldane.
    /// </summary>
    public void Segment(double profondeurDebutM, double profondeurFinM, double dureeMin, Melange gaz)
    {
        if (dureeMin <= 0)
            return;

        double pInspDebut = PressionAmbiante(profondeurDebutM) - VapeurEau;
        double vitesseBarParMin = (profondeurFinM - profondeurDebutM) / 10 / dureeMin;

        for (int i = 0; i < ZHL16C.NombreCompartiments; i++)
        {
            TensionN2[i] = Schreiner(TensionN2[i], pInspDebut * gaz.FractionN2,
                vitesseBarParMin * gaz.FractionN2, dureeMin, ZHL16C.DemiVieN2[i]);
            TensionHe[i] = Schreiner(TensionHe[i], pInspDebut * gaz.FractionHe,
                vitesseBarParMin * gaz.FractionHe, dureeMin, ZHL16C.DemiVieHe[i]);
        }
    }

    /// <summary>
    /// P(t) = Pi0 + R(t − 1/k) − (Pi0 − P0 − R/k)·e^(−kt), avec k = ln2 / demi-vie.
    /// P0 : tension de départ, Pi0 : pression inspirée de départ, R : variation de la pression inspirée par minute.
    /// </summary>
    public static double Schreiner(double p0, double pInsp0, double r, double t, double demiVie)
    {
        double k = Math.Log(2) / demiVie;
        return pInsp0 + r * (t - 1 / k) - (pInsp0 - p0 - r / k) * Math.Exp(-k * t);
    }

    /// <summary>
    /// Pression ambiante minimale tolérée, avec un facteur de gradient gf (1 = Bühlmann pur).
    /// Pour chaque compartiment : Ptol = (P − a·gf) / (gf/b + 1 − gf), et on garde le plus contraignant.
    /// </summary>
    public double PressionTolereeMin(double gf)
    {
        double max = 0;
        for (int i = 0; i < ZHL16C.NombreCompartiments; i++)
        {
            double pN2 = TensionN2[i], pHe = TensionHe[i], total = pN2 + pHe;
            // Avec de l'hélium, a et b sont la moyenne des deux gaz pondérée par leur tension.
            double a = (ZHL16C.AN2[i] * pN2 + ZHL16C.AHe[i] * pHe) / total;
            double b = (ZHL16C.BN2[i] * pN2 + ZHL16C.BHe[i] * pHe) / total;
            double tolere = (total - a * gf) / (gf / b + 1 - gf);
            max = Math.Max(max, tolere);
        }
        return max;
    }

    /// <summary>Le plafond (m) : la profondeur au-dessus de laquelle il ne faut pas remonter. 0 = surface autorisée.</summary>
    public double Plafond(double gf) => Math.Max(0, (PressionTolereeMin(gf) - PressionSurface) * 10);
}
