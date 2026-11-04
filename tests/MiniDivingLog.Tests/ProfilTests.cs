using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Tests;

public class ProfilTests
{
    private static PointProfil P(double minutes, double profondeur, double? temp = null) =>
        new(TimeSpan.FromMinutes(minutes), profondeur, temp);

    // Descente à 20 m en 2 min, 20 min au fond, remontée en 3 min.
    private static readonly Profil Carre = new([P(0, 0), P(2, 20), P(22, 20), P(25, 0)]);

    [Fact]
    public void Duree_EtProfondeurMax()
    {
        Assert.Equal(TimeSpan.FromMinutes(25), Carre.Duree);
        Assert.Equal(20, Carre.ProfondeurMax);
    }

    [Fact]
    public void ProfondeurMoyenne_PondereeParLeTemps()
    {
        // Aire : 2×10 + 20×20 + 3×10 = 450 m·min, sur 25 min = 18 m
        Assert.Equal(18, Carre.ProfondeurMoyenne, 6);
    }

    [Fact]
    public void VitesseRemonteeMax()
    {
        // 20 m en 3 min = 6,67 m/min
        Assert.Equal(6.67, Carre.VitesseRemonteeMax, 2);
        Assert.Empty(Carre.RemonteesRapides());
    }

    [Fact]
    public void RemonteeTropRapide_EstDetectee()
    {
        var profil = new Profil([P(0, 0), P(2, 30), P(20, 30), P(21, 15), P(26, 0)]);

        var rapide = Assert.Single(profil.RemonteesRapides());
        Assert.Equal(TimeSpan.FromMinutes(20), rapide.Debut);
        Assert.Equal(15, rapide.VitesseMParMin, 6);
    }

    [Fact]
    public void TemperatureMin_IgnoreLesPointsSansTemperature()
    {
        var profil = new Profil([P(0, 0, 22), P(5, 18), P(10, 18, 16), P(15, 0, 21)]);

        Assert.Equal(16, profil.TemperatureMin);
    }

    [Fact]
    public void PointsDansLeDesordre_SontRefuses() =>
        Assert.Throws<ArgumentException>(() => new Profil([P(5, 10), P(1, 5)]));
}
