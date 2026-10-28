namespace MiniDivingLog.Core.Carnet;

/// <summary>Un bloc de plongée (simplifié).</summary>
public record Bloc(
    double VolumeL,
    double PressionDebutBar,
    double PressionFinBar,
    double FractionO2 = 0.21,
    double FractionHe = 0.0);
