namespace MiniDivingLog.Core.Calculs.Deco;

/// <summary>Un gaz respiré : fractions d'oxygène et d'hélium, le reste est de l'azote.</summary>
public record Melange(double FractionO2, double FractionHe = 0)
{
    public static readonly Melange Air = new(0.21);

    public double FractionN2 => 1 - FractionO2 - FractionHe;
}
