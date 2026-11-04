using MiniDivingLog.Core.Calculs.Deco;

namespace MiniDivingLog.Tests;

public class DecoTests
{
    [Fact]
    public void Haldane_ApresUneDemiVie_LaTensionAFaitLaMoitieDuChemin()
    {
        // Tension 1 bar, gaz inspiré à 3 bar, profondeur constante (R = 0) :
        // après une demi-vie, la tension doit valoir 2 bar.
        Assert.Equal(2.0, Tissus.Schreiner(1.0, 3.0, 0, 5.0, 5.0), 9);
    }

    [Fact]
    public void Schreiner_DeuxDemiSegments_EgaleUnSegmentComplet()
    {
        var enUneFois = new Tissus();
        enUneFois.Segment(0, 30, 2, Melange.Air);

        var enDeuxFois = new Tissus();
        enDeuxFois.Segment(0, 15, 1, Melange.Air);
        enDeuxFois.Segment(15, 30, 1, Melange.Air);

        for (int i = 0; i < ZHL16C.NombreCompartiments; i++)
            Assert.Equal(enUneFois.TensionN2[i], enDeuxFois.TensionN2[i], 9);
    }

    [Fact]
    public void EnSurface_SansAvoirPlonge_PasDePlafond() =>
        Assert.Equal(0, new Tissus().Plafond(1.0));

    [Fact]
    public void ApresUneLonguePlongee_PlafondNonNul()
    {
        var tissus = new Tissus();
        tissus.Segment(40, 40, 60, Melange.Air);

        Assert.True(tissus.Plafond(1.0) > 0);
    }

    [Fact]
    public void LimiteSansPalier_Air30m_OrdreDeGrandeur()
    {
        // Bühlmann pur (GF 100/100) : une quinzaine de minutes à 30 m à l'air.
        var pur = new Planificateur { GfBas = 1.0, GfHaut = 1.0 };

        Assert.InRange(pur.LimiteSansPalier(30, Melange.Air), 12, 22);
    }

    [Fact]
    public void LimiteSansPalier_DiminueAvecLaProfondeur()
    {
        var plan = new Planificateur();

        Assert.True(plan.LimiteSansPalier(18, Melange.Air) > plan.LimiteSansPalier(30, Melange.Air));
        Assert.True(plan.LimiteSansPalier(30, Melange.Air) > plan.LimiteSansPalier(40, Melange.Air));
    }

    [Fact]
    public void LeNitrox_AllongeLaLimiteSansPalier()
    {
        var plan = new Planificateur();

        Assert.True(plan.LimiteSansPalier(30, new Melange(0.32)) > plan.LimiteSansPalier(30, Melange.Air));
    }

    [Fact]
    public void PlongeeCourte_SansPalier()
    {
        var plan = new Planificateur().Planifier(12, 20, Melange.Air);

        Assert.True(plan.SansPalier);
    }

    [Fact]
    public void Plongee40m25min_PaliersCoherents()
    {
        var plan = new Planificateur().Planifier(40, 25, Melange.Air);

        Assert.False(plan.SansPalier);
        Assert.All(plan.Paliers, p => Assert.Equal(0, p.ProfondeurM % 3));
        Assert.Equal(3, plan.Paliers[^1].ProfondeurM);
        // Les paliers sont de moins en moins profonds.
        Assert.Equal(plan.Paliers.Select(p => p.ProfondeurM).OrderByDescending(p => p), plan.Paliers.Select(p => p.ProfondeurM));
    }

    [Fact]
    public void GfPlusPrudent_DecoPlusLongue()
    {
        var prudent = new Planificateur { GfBas = 0.30, GfHaut = 0.70 }.Planifier(40, 25, Melange.Air);
        var agressif = new Planificateur { GfBas = 0.90, GfHaut = 0.90 }.Planifier(40, 25, Melange.Air);

        Assert.True(prudent.DureeTotaleMin > agressif.DureeTotaleMin);
    }

    [Fact]
    public void Trimix_HeliumPrisEnCompte()
    {
        var plan = new Planificateur().Planifier(60, 20, new Melange(0.18, 0.45));

        Assert.False(plan.SansPalier);
        Assert.True(plan.Paliers[0].ProfondeurM > 9);
    }

    [Fact]
    public void Deroulement_DecritToutLePlan()
    {
        var plan = new Planificateur().Planifier(40, 25, Melange.Air);
        var points = plan.Deroulement.Points;

        Assert.Equal(0, points[0].ProfondeurM);
        Assert.Equal(0, points[^1].ProfondeurM);
        Assert.Equal(40, plan.Deroulement.ProfondeurMax);
        Assert.Equal(plan.DureeTotaleMin, plan.Deroulement.Duree.TotalMinutes, 6);
        // Le plan respecte lui-même la vitesse de remontée.
        Assert.Empty(plan.Deroulement.RemonteesRapides());
    }
}
