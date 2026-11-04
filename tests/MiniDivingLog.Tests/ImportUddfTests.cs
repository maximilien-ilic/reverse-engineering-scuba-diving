using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Import;
using MiniDivingLog.Core.Stockage;

namespace MiniDivingLog.Tests;

public class ImportUddfTests
{
    private static readonly string Exemple = Path.Combine(AppContext.BaseDirectory, "Exemples", "carnet.uddf");

    [Fact]
    public void LitLesDeuxPlongees()
    {
        var plongees = ImportUddf.LireFichier(Exemple);

        Assert.Equal(2, plongees.Count);
    }

    [Fact]
    public void PremierePlongee_InfosEtUnitesConverties()
    {
        Plongee p = ImportUddf.LireFichier(Exemple)[0].Plongee;

        Assert.Equal(12, p.Numero);
        Assert.Equal(new DateTime(2026, 7, 14, 10, 30, 0), p.Date);
        Assert.Equal("Port-Cros", p.Lieu);         // résolu via <link ref="portcros"/>
        Assert.Equal(25, p.DureeMin, 6);           // 1500 s
        Assert.Equal(20, p.ProfondeurMaxM);
        Assert.Equal(19, p.TemperatureEauC);       // 292,15 K
        Assert.Equal("Mérous et posidonies.", p.Commentaires);
    }

    [Fact]
    public void Bloc_EnLitresBarEtAvecSonMelange()
    {
        Bloc b = Assert.Single(ImportUddf.LireFichier(Exemple)[0].Plongee.Blocs);

        Assert.Equal(12, b.VolumeL, 6);            // 0,012 m³
        Assert.Equal(200, b.PressionDebutBar, 6);  // 20 000 000 Pa
        Assert.Equal(50, b.PressionFinBar, 6);
        Assert.Equal(0.32, b.FractionO2);          // résolu via <link ref="ean32"/>
    }

    [Fact]
    public void Profil_EstLuEtSertDeSecours()
    {
        // La 2e plongée n'a ni <informationafterdive> ni numéro :
        // on déduit durée et profondeurs du profil, et le numéro suit la précédente.
        var (p, profil) = ImportUddf.LireFichier(Exemple)[1];

        Assert.NotNull(profil);
        Assert.Equal(4, profil.Points.Count);
        Assert.Equal(13, p.Numero);
        Assert.Equal(32, p.DureeMin, 6);
        Assert.Equal(12, p.ProfondeurMaxM);
        Assert.Equal("", p.Lieu);
    }

    [Fact]
    public void ProfondeurMoyenne_CalculeeDepuisLeProfil()
    {
        // Pas de <averagedepth> : 1re plongée = carré 0→20 m en 2 min, 20 min au fond, 3 min de remontée.
        Assert.Equal(18, ImportUddf.LireFichier(Exemple)[0].Plongee.ProfondeurMoyM, 6);
    }

    [Fact]
    public void ImportPuisSauvegardeDansLeCarnet()
    {
        using var carnet = new CarnetSqlite(":memory:");
        foreach (var (plongee, _) in ImportUddf.LireFichier(Exemple))
            carnet.Ajouter(plongee);

        Assert.Equal([12, 13], carnet.Lister().Select(p => p.Numero));
        // 150 bar × 12 L = 1800 L, sur 25 min à 18 m de moyenne (2,8 bar) : 25,71 L/min
        Assert.Equal(25.71, carnet.Trouver(12)!.CalculerSac()!.Value, 2);
    }

    [Fact]
    public void PasUnFichierUddf_EstRefuse() =>
        Assert.Throws<FormatException>(() => ImportUddf.LireTexte("<carnet/>"));

    [Fact]
    public void PlongeeSansDate_EstIgnoree()
    {
        // Cas réel : l'export d'AP DiveSight commence par une plongée vide
        // qui ne contient que l'intervalle surface.
        const string xml = """
            <uddf xmlns="http://www.streit.cc/uddf/3.2/" version="3.2.0">
              <profiledata><repetitiongroup id="rg">
                <dive id="precedente">
                  <informationbeforedive><surfaceintervalbeforedive><infinity/></surfaceintervalbeforedive></informationbeforedive>
                </dive>
                <dive id="vraie">
                  <informationbeforedive><divenumber>942</divenumber><datetime>2022-04-24T10:30:58</datetime></informationbeforedive>
                  <informationafterdive><greatestdepth>18</greatestdepth><diveduration>2400</diveduration></informationafterdive>
                </dive>
              </repetitiongroup></profiledata>
            </uddf>
            """;

        var (p, _) = Assert.Single(ImportUddf.LireTexte(xml));
        Assert.Equal(942, p.Numero);
        Assert.Equal(40, p.DureeMin, 6);
    }

    [Fact]
    public void PointsEnDoubleOuDansLeDesordre_SontRemisEnOrdre()
    {
        // Cas réel (AP DiveSight) : deux échantillons au même instant, et un qui recule d'une seconde.
        const string xml = """
            <uddf><profiledata><repetitiongroup><dive>
              <informationbeforedive><datetime>2022-04-24T10:30:58</datetime></informationbeforedive>
              <samples>
                <waypoint><depth>0</depth><divetime>0</divetime></waypoint>
                <waypoint><depth>1.2</depth><divetime>0</divetime></waypoint>
                <waypoint><depth>10</depth><divetime>60</divetime></waypoint>
                <waypoint><depth>9</depth><divetime>59</divetime></waypoint>
                <waypoint><depth>0</depth><divetime>120</divetime></waypoint>
              </samples>
            </dive></repetitiongroup></profiledata></uddf>
            """;

        var (_, profil) = Assert.Single(ImportUddf.LireTexte(xml));

        Assert.Equal([0, 59, 60, 120], profil!.Points.Select(p => p.Temps.TotalSeconds));
    }
}
