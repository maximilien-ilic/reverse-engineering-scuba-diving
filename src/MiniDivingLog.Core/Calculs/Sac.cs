namespace MiniDivingLog.Core.Calculs;

/// <summary>Consommation d'air ramenée à la surface (SAC), en litres par minute.</summary>
public static class Sac
{
    public static double Calculer(double pressionDebutBar, double pressionFinBar,
        double volumeBlocL, double dureeMin, double profondeurMoyM)
    {
        if (dureeMin <= 0)
            throw new ArgumentOutOfRangeException(nameof(dureeMin), "La durée doit être positive.");

        double litres = (pressionDebutBar - pressionFinBar) * volumeBlocL;
        return litres / dureeMin / Nitrox.PressionAbsolue(profondeurMoyM);
    }
}
