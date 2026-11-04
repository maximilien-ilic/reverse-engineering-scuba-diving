namespace MiniDivingLog.Core.Calculs.Deco;

/// <summary>
/// Coefficients du modèle Bühlmann ZH-L16C, tels que publiés par A. A. Bühlmann
/// (« Tauchmedizin »). Ce sont des données scientifiques publiques : on retrouve
/// les mêmes champs dans la classe ZHL16 de Diving Log (an2, bn2, ahe, bhe, halftn, halfth).
/// 16 compartiments, du plus rapide (5 min) au plus lent (635 min) pour l'azote.
/// </summary>
public static class ZHL16C
{
    public static readonly double[] DemiVieN2 =
        [5.0, 8.0, 12.5, 18.5, 27.0, 38.3, 54.3, 77.0, 109.0, 146.0, 187.0, 239.0, 305.0, 390.0, 498.0, 635.0];

    public static readonly double[] AN2 =
        [1.1696, 1.0, 0.8618, 0.7562, 0.62, 0.5043, 0.441, 0.4, 0.375, 0.35, 0.3295, 0.3065, 0.2835, 0.261, 0.248, 0.2327];

    public static readonly double[] BN2 =
        [0.5578, 0.6514, 0.7222, 0.7825, 0.8126, 0.8434, 0.8693, 0.8910, 0.9092, 0.9222, 0.9319, 0.9403, 0.9477, 0.9544, 0.9602, 0.9653];

    public static readonly double[] DemiVieHe =
        [1.88, 3.02, 4.72, 6.99, 10.21, 14.48, 20.53, 29.11, 41.20, 55.19, 70.69, 90.34, 115.29, 147.42, 188.24, 240.03];

    public static readonly double[] AHe =
        [1.6189, 1.383, 1.1919, 1.0458, 0.922, 0.8205, 0.7305, 0.6502, 0.595, 0.5545, 0.5333, 0.5189, 0.5181, 0.5176, 0.5172, 0.5119];

    public static readonly double[] BHe =
        [0.4770, 0.5747, 0.6527, 0.7223, 0.7582, 0.7957, 0.8279, 0.8553, 0.8757, 0.8903, 0.8997, 0.9073, 0.9122, 0.9171, 0.9217, 0.9267];

    public const int NombreCompartiments = 16;
}
