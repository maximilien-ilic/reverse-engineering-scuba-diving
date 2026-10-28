namespace MiniDivingLog.Core.Calculs;

/// <summary>
/// Calculs Nitrox : formules publiques de plongée.
/// Convention : eau de mer, 10 m de profondeur = 1 bar de plus.
/// Les fractions de gaz sont entre 0 et 1 (0.32 pour un Nitrox 32).
/// </summary>
public static class Nitrox
{
    public const double FractionN2Air = 0.79;

    /// <summary>Pression absolue (bar) à une profondeur donnée.</summary>
    public static double PressionAbsolue(double profondeurM) => profondeurM / 10 + 1;

    /// <summary>Profondeur maximale d'utilisation (m).</summary>
    public static double Mod(double fo2, double ppo2Max = 1.4) => (ppo2Max / fo2 - 1) * 10;

    /// <summary>Fraction d'O2 la plus riche utilisable à cette profondeur.</summary>
    public static double MeilleurMelange(double profondeurM, double ppo2Max = 1.4) =>
        Math.Min(1.0, ppo2Max / PressionAbsolue(profondeurM));

    /// <summary>Pression partielle d'O2 (bar) respirée à cette profondeur.</summary>
    public static double PpO2(double fo2, double profondeurM) => fo2 * PressionAbsolue(profondeurM);

    /// <summary>Profondeur air équivalente (m) : même PpN2 qu'à l'air.</summary>
    public static double Ead(double fo2, double profondeurM) =>
        PressionAbsolue(profondeurM) * (1 - fo2) / FractionN2Air * 10 - 10;

    /// <summary>Profondeur narcotique équivalente (m), O2 et N2 comptés narcotiques.</summary>
    public static double End(double fhe, double profondeurM) =>
        PressionAbsolue(profondeurM) * (1 - fhe) * 10 - 10;
}
