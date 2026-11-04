using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Statistiques;

namespace MiniDivingLog.Tests;

public class ActiviteTests
{
    private static Plongee Le(int annee, int mois, int jour, int heure = 10) => new()
    {
        Numero = mois * 100 + jour * 2 + heure, Date = new DateTime(annee, mois, jour, heure, 0, 0),
        Lieu = "Port-Cros", DureeMin = 45, ProfondeurMaxM = 20, ProfondeurMoyM = 12,
    };

    private static readonly DateOnly Aujourdhui = new(2026, 11, 4); // un mercredi

    [Fact]
    public void Grille_CommenceUnLundiEtFinitAujourdhui()
    {
        var a = new ActiviteAnnuelle([], Aujourdhui);

        Assert.Equal(DayOfWeek.Monday, a.Debut.DayOfWeek);
        Assert.All(a.Semaines, s => Assert.Equal(7, s.Count));
        Assert.Contains(a.Semaines[^1], j => j.Date == Aujourdhui);
        Assert.InRange(a.Semaines.Count, 53, 54);
    }

    [Fact]
    public void CompteLesPlongeesParJour_EtLeTotal()
    {
        var a = new ActiviteAnnuelle([Le(2026, 7, 14), Le(2026, 7, 14, 15), Le(2026, 8, 1)], Aujourdhui);

        var jours = a.Semaines.SelectMany(s => s).ToDictionary(j => j.Date);
        Assert.Equal(2, jours[new DateOnly(2026, 7, 14)].Nombre);
        Assert.Equal(1, jours[new DateOnly(2026, 8, 1)].Nombre);
        Assert.Equal(3, a.Total);
        Assert.Equal(2, a.MaxParJour);
    }

    [Fact]
    public void PlongeesHorsDeLAnnee_SontIgnorees()
    {
        var a = new ActiviteAnnuelle([Le(2024, 7, 14), Le(2026, 12, 25)], Aujourdhui);

        Assert.Equal(0, a.Total);
    }

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 1)]
    [InlineData(2, 4, 2)]
    [InlineData(3, 4, 3)]
    [InlineData(4, 4, 4)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 4)]
    public void Niveau_RelatifAuJourLePlusCharge(int nombre, int max, int attendu) =>
        Assert.Equal(attendu, ActiviteAnnuelle.Niveau(nombre, max));

    [Fact]
    public void MaxCommun_RendLesGrillesComparables()
    {
        var a = new ActiviteAnnuelle([Le(2026, 7, 14)], Aujourdhui, maxCommun: 4);

        Assert.Equal(1, a.Semaines.SelectMany(s => s).Single(j => j.Nombre == 1).Niveau);
    }
}
