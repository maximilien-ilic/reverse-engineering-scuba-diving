using Microsoft.Data.Sqlite;
using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Stockage;

namespace MiniDivingLog.Tests;

public class CarnetSqliteTests
{
    private static Plongee Exemple(int numero = 1) => new()
    {
        Numero = numero,
        Date = new DateTime(2026, 7, 14, 10, 30, 0),
        Lieu = "Port-Cros",
        DureeMin = 45,
        ProfondeurMaxM = 28,
        ProfondeurMoyM = 20,
        TemperatureEauC = 19,
        Binome = "Léa",
        Blocs = [new Bloc(12, 200, 50, 0.32)],
    };

    [Fact]
    public void AjouterPuisRelire_RedonneLaMemePlongee()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple());

        Plongee? relue = carnet.Trouver(1);

        Assert.NotNull(relue);
        Assert.Equal("Port-Cros", relue.Lieu);
        Assert.Equal(new DateTime(2026, 7, 14, 10, 30, 0), relue.Date);
        Assert.Equal(19, relue.TemperatureEauC);
        Assert.Equal("Léa", relue.Binome);
        Assert.Equal(new Bloc(12, 200, 50, 0.32), Assert.Single(relue.Blocs));
    }

    [Fact]
    public void Lister_TrieParNumero()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(3));
        carnet.Ajouter(Exemple(1));
        carnet.Ajouter(Exemple(2));

        Assert.Equal([1, 2, 3], carnet.Lister().Select(p => p.Numero));
    }

    [Fact]
    public void TemperatureAbsente_ResteNulle()
    {
        using var carnet = new CarnetSqlite(":memory:");
        var p = Exemple();
        carnet.Ajouter(new Plongee
        {
            Numero = p.Numero, Date = p.Date, Lieu = p.Lieu, DureeMin = p.DureeMin,
            ProfondeurMaxM = p.ProfondeurMaxM, ProfondeurMoyM = p.ProfondeurMoyM,
        });

        Assert.Null(carnet.Trouver(1)!.TemperatureEauC);
    }

    [Fact]
    public void NumeroEnDouble_EstRefuse()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(1));

        Assert.Throws<SqliteException>(() => carnet.Ajouter(Exemple(1)));
        Assert.Single(carnet.Lister());
    }

    [Fact]
    public void Supprimer_EnleveLaPlongee()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(1));

        Assert.True(carnet.Supprimer(1));
        Assert.Null(carnet.Trouver(1));
        Assert.False(carnet.Supprimer(1));
    }

    [Fact]
    public void LeCarnetSurvitALaFermeture()
    {
        string fichier = Path.Combine(Path.GetTempPath(), $"carnet-{Guid.NewGuid()}.db");
        try
        {
            using (var carnet = new CarnetSqlite(fichier))
                carnet.Ajouter(Exemple());

            using (var rouvert = new CarnetSqlite(fichier))
                Assert.Equal(13.33, rouvert.Trouver(1)!.CalculerSac()!.Value, 2);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(fichier);
        }
    }
}
