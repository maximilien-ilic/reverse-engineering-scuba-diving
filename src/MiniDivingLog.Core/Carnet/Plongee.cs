using MiniDivingLog.Core.Calculs;

namespace MiniDivingLog.Core.Carnet;

/// <summary>Une plongée du carnet (version simplifiée).</summary>
public class Plongee
{
    public required int Numero { get; init; }
    public required DateTime Date { get; init; }
    public required string Lieu { get; init; }
    public required double DureeMin { get; init; }
    public required double ProfondeurMaxM { get; init; }
    public required double ProfondeurMoyM { get; init; }
    public double? TemperatureEauC { get; init; }
    public string Binome { get; init; } = "";
    public string Commentaires { get; init; } = "";
    public List<Bloc> Blocs { get; init; } = [];

    /// <summary>SAC calculé sur le premier bloc, ou null s'il n'y en a pas.</summary>
    public double? CalculerSac()
    {
        if (Blocs.Count == 0)
            return null;

        Bloc b = Blocs[0];
        return Sac.Calculer(b.PressionDebutBar, b.PressionFinBar, b.VolumeL, DureeMin, ProfondeurMoyM);
    }
}
