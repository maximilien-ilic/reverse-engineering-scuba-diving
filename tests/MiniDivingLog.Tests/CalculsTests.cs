using MiniDivingLog.Core.Calculs;
using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Tests;

public class CalculsTests
{
    [Fact]
    public void Mod_Nitrox32() =>
        // Nitrox 32 à PpO2 1,4 : MOD = 33,75 m
        Assert.Equal(33.75, Nitrox.Mod(0.32), 2);

    [Fact]
    public void MeilleurMelange_A30m() =>
        Assert.Equal(0.35, Nitrox.MeilleurMelange(30), 3);

    [Fact]
    public void Ead_AirEgaleProfondeur() =>
        Assert.Equal(30, Nitrox.Ead(0.21, 30), 6);

    [Fact]
    public void Ead_Nitrox36_A30m() =>
        Assert.Equal(22.41, Nitrox.Ead(0.36, 30), 2);

    [Fact]
    public void End_Trimix() =>
        // Trimix 21/35 à 60 m : 7 × 0,65 × 10 − 10 = 35,5 m
        Assert.Equal(35.5, Nitrox.End(0.35, 60), 6);

    [Fact]
    public void Sac_Exemple() =>
        // 200 → 50 bar sur un 12 L, 45 min à 20 m de moyenne : 1800 L / 45 / 3
        Assert.Equal(13.33, Sac.Calculer(200, 50, 12, 45, 20), 2);

    [Fact]
    public void Sac_DureeNulle_Refusee() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Sac.Calculer(200, 50, 12, 0, 20));

    [Fact]
    public void Plongee_CalculeSonSac()
    {
        var p = new Plongee
        {
            Numero = 1,
            Date = new DateTime(2026, 7, 14, 10, 0, 0),
            Lieu = "Port-Cros",
            DureeMin = 45,
            ProfondeurMaxM = 28,
            ProfondeurMoyM = 20,
            Blocs = [new Bloc(12, 200, 50)],
        };

        Assert.Equal(13.33, p.CalculerSac()!.Value, 2);
    }

    [Fact]
    public void Plongee_SansBloc_PasDeSac()
    {
        var p = new Plongee
        {
            Numero = 2, Date = DateTime.Today, Lieu = "Fosse",
            DureeMin = 30, ProfondeurMaxM = 20, ProfondeurMoyM = 12,
        };

        Assert.Null(p.CalculerSac());
    }
}
