using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.App;

/// <summary>
/// Dessine le profil d'une plongée : le temps vers la droite, la profondeur vers le bas
/// (comme le « Profile » de Diving Log). Les remontées trop rapides sont en rouge.
/// </summary>
public class VueProfil : Control
{
    public static readonly StyledProperty<Profil?> ProfilProperty =
        AvaloniaProperty.Register<VueProfil, Profil?>(nameof(Profil));

    static VueProfil() => AffectsRender<VueProfil>(ProfilProperty);

    public Profil? Profil
    {
        get => GetValue(ProfilProperty);
        set => SetValue(ProfilProperty, value);
    }

    private static readonly IBrush Fond = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFB));
    private static readonly IBrush Eau = new SolidColorBrush(Color.FromArgb(0x60, 0x1E, 0x88, 0xE5));
    private static readonly IPen Courbe = new Pen(new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1)), 2);
    private static readonly IPen Alerte = new Pen(Brushes.Red, 3);
    private static readonly IPen Grille = new Pen(new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)), 1);

    private const double MargeGauche = 44, MargeBas = 24, MargeHaut = 8, MargeDroite = 12;

    public override void Render(DrawingContext ctx)
    {
        var zone = new Rect(MargeGauche, MargeHaut,
            Math.Max(0, Bounds.Width - MargeGauche - MargeDroite),
            Math.Max(0, Bounds.Height - MargeHaut - MargeBas));
        ctx.FillRectangle(Fond, zone);

        if (Profil is not { Points.Count: >= 2 } profil)
        {
            Ecrire(ctx, "Pas de profil pour cette plongée", zone.Center, centre: true);
            return;
        }

        double dureeMin = profil.Duree.TotalMinutes;
        double profMax = Math.Ceiling(profil.ProfondeurMax / 5) * 5;  // échelle arrondie à 5 m
        Point Ecran(PointProfil p) => new(
            zone.Left + p.Temps.TotalMinutes / dureeMin * zone.Width,
            zone.Top + p.ProfondeurM / profMax * zone.Height);

        // Grille : une ligne tous les 5 m et toutes les 5 minutes, avec les graduations.
        for (double m = 0; m <= profMax; m += 5)
        {
            double y = zone.Top + m / profMax * zone.Height;
            ctx.DrawLine(Grille, new Point(zone.Left, y), new Point(zone.Right, y));
            Ecrire(ctx, $"{m:0} m", new Point(4, y - 8));
        }
        for (double t = 0; t <= dureeMin; t += 5)
        {
            double x = zone.Left + t / dureeMin * zone.Width;
            ctx.DrawLine(Grille, new Point(x, zone.Top), new Point(x, zone.Bottom));
            Ecrire(ctx, $"{t:0}'", new Point(x - 6, zone.Bottom + 4));
        }

        // La courbe, remplie jusqu'à la surface.
        var forme = new StreamGeometry();
        using (var g = forme.Open())
        {
            g.BeginFigure(new Point(zone.Left, zone.Top), isFilled: true);
            foreach (var p in profil.Points)
                g.LineTo(Ecran(p));
            g.LineTo(new Point(Ecran(profil.Points[^1]).X, zone.Top));
            g.EndFigure(isClosed: true);
        }
        ctx.DrawGeometry(Eau, null, forme);

        for (int i = 1; i < profil.Points.Count; i++)
            ctx.DrawLine(Courbe, Ecran(profil.Points[i - 1]), Ecran(profil.Points[i]));

        // Les remontées trop rapides (calculées par la classe Profil de l'étape 3).
        var parDebut = profil.Points.ToDictionary(p => p.Temps);
        foreach (var r in profil.RemonteesRapides())
        {
            var fin = profil.Points.First(p => p.Temps == r.Fin);
            ctx.DrawLine(Alerte, Ecran(parDebut[r.Debut]), Ecran(fin));
        }
    }

    private static void Ecrire(DrawingContext ctx, string texte, Point position, bool centre = false)
    {
        var ft = new FormattedText(texte, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.DimGray);
        if (centre)
            position = new Point(position.X - ft.Width / 2, position.Y - ft.Height / 2);
        ctx.DrawText(ft, position);
    }
}
