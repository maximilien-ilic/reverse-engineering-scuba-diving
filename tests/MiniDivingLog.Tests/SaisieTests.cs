using Microsoft.Data.Sqlite;
using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Stockage;

namespace MiniDivingLog.Tests;

public class SaisieTests
{
    private static Plongee Exemple(int numero = 1, string lieu = "Port-Cros", double pmax = 28,
        double pmoy = 20, double duree = 45, Bloc? bloc = null) => new()
    {
        Numero = numero,
        Date = new DateTime(2026, 7, 14, 10, 30, 0),
        Lieu = lieu,
        DureeMin = duree,
        ProfondeurMaxM = pmax,
        ProfondeurMoyM = pmoy,
        Blocs = [bloc ?? new Bloc(12, 200, 50)],
    };

    [Fact]
    public void PlongeeCorrecte_AucuneErreur() =>
        Assert.Empty(ValidationPlongee.Verifier(Exemple()));

    [Fact]
    public void Erreurs_SontToutesSignalees()
    {
        var p = Exemple(numero: 0, lieu: " ", pmax: 20, pmoy: 25, duree: 0, bloc: new Bloc(12, 50, 200, 0.05));

        var erreurs = ValidationPlongee.Verifier(p);

        Assert.Contains("Le numéro doit être positif.", erreurs);
        Assert.Contains("Le lieu est obligatoire.", erreurs);
        Assert.Contains("La durée doit être positive.", erreurs);
        Assert.Contains(erreurs, e => e.StartsWith("La profondeur moyenne"));
        Assert.Contains(erreurs, e => e.Contains("pression de fin"));
        Assert.Contains(erreurs, e => e.Contains("l'O2"));
    }

    [Fact]
    public void Modifier_RemplaceLesInfosEtLesBlocs()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(1));

        carnet.Modifier(1, Exemple(1, lieu: "Marseille", bloc: new Bloc(15, 220, 60, 0.32)));

        Plongee relue = carnet.Trouver(1)!;
        Assert.Equal("Marseille", relue.Lieu);
        Assert.Equal(new Bloc(15, 220, 60, 0.32), Assert.Single(relue.Blocs));
    }

    [Fact]
    public void Modifier_PeutChangerLeNumeroEtGardeLeProfil()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(1));
        carnet.EnregistrerProfil(1, new Profil([new PointProfil(TimeSpan.Zero, 0), new PointProfil(TimeSpan.FromMinutes(1), 5)]));

        carnet.Modifier(1, Exemple(7));

        Assert.Null(carnet.Trouver(1));
        Assert.NotNull(carnet.ChargerProfil(7));
    }

    [Fact]
    public void Modifier_VersUnNumeroDejaPris_EstRefuse()
    {
        using var carnet = new CarnetSqlite(":memory:");
        carnet.Ajouter(Exemple(1));
        carnet.Ajouter(Exemple(2));

        Assert.Throws<SqliteException>(() => carnet.Modifier(2, Exemple(1)));
        Assert.Equal([1, 2], carnet.Lister().Select(p => p.Numero));
    }

    [Fact]
    public void ProchainNumero()
    {
        using var carnet = new CarnetSqlite(":memory:");
        Assert.Equal(1, carnet.ProchainNumero());

        carnet.Ajouter(Exemple(12));
        Assert.Equal(13, carnet.ProchainNumero());
    }
}
