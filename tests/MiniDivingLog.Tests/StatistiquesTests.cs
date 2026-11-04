using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Statistiques;

namespace MiniDivingLog.Tests;

public class StatistiquesTests
{
    private static Plongee Plongee(int numero, string lieu, double duree, double pmax,
        double? temp = null, Bloc? bloc = null) => new()
    {
        Numero = numero,
        Date = new DateTime(2026, 7, numero, 10, 0, 0),
        Lieu = lieu,
        DureeMin = duree,
        ProfondeurMaxM = pmax,
        ProfondeurMoyM = 20,
        TemperatureEauC = temp,
        Blocs = bloc is null ? [] : [bloc],
    };

    private static readonly StatistiquesCarnet Stats = new([
        Plongee(1, "Port-Cros", 45, 28, temp: 19, bloc: new Bloc(12, 200, 50)),
        Plongee(2, "Port-Cros", 60, 35, temp: 17),
        Plongee(3, "Marseille", 30, 18, temp: 21, bloc: new Bloc(12, 200, 80)),
    ]);

    [Fact]
    public void NombreEtTempsTotal()
    {
        Assert.Equal(3, Stats.Nombre);
        Assert.Equal(TimeSpan.FromMinutes(135), Stats.TempsTotal);
        Assert.Equal(45, Stats.DureeMoyenneMin);
    }

    [Fact]
    public void Records_IndiquentLaPlongee()
    {
        Assert.Equal(new RecordCarnet(35, 2), Stats.ProfondeurMax);
        Assert.Equal(new RecordCarnet(60, 2), Stats.PlusLonguePlongee);
        Assert.Equal(new RecordCarnet(17, 2), Stats.EauLaPlusFroide);
        Assert.Equal(new RecordCarnet(21, 3), Stats.EauLaPlusChaude);
    }

    [Fact]
    public void SacMoyen_SurLesPlongeesAvecBloc()
    {
        // Plongée 1 : 1800 L / 45 / 3 = 13,33 ; plongée 3 : 1440 L / 30 / 3 = 16
        Assert.Equal(14.67, Stats.SacMoyen!.Value, 2);
    }

    [Fact]
    public void PremiereEtDerniere()
    {
        Assert.Equal(1, Stats.Premiere!.Numero);
        Assert.Equal(3, Stats.Derniere!.Numero);
    }

    [Fact]
    public void ParLieu_DuPlusFrequent()
    {
        Assert.Equal([("Port-Cros", 2), ("Marseille", 1)], Stats.ParLieu());
    }

    [Fact]
    public void CarnetVide_PasDeRecord()
    {
        var vide = new StatistiquesCarnet([]);

        Assert.Equal(0, vide.Nombre);
        Assert.Null(vide.ProfondeurMax);
        Assert.Null(vide.SacMoyen);
        Assert.Null(vide.DureeMoyenneMin);
    }
}
