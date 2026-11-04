using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MiniDivingLog.App;

/// <summary>Une « tache » de couleur du fond.</summary>
/// <param name="X">Position du centre, en fraction de la largeur (0 à 1).</param>
/// <param name="Y">Position du centre, en fraction de la hauteur (0 à 1).</param>
/// <param name="Rayon">Rayon, en fraction de la plus grande dimension.</param>
/// <param name="Opacite">Opacité au centre de la tache, de 0 à 255.</param>
public record TacheAurora(Color Couleur, double X, double Y, double Rayon, byte Opacite);

/// <summary>
/// Fond « aurora » fixe, dessiné à la main en trois couches :
/// 1. un dégradé en diagonale, du bleu abyssal (en haut à gauche) au bleu lagon (en bas à droite) ;
/// 2. de grandes taches floues, claires et foncées, qui cassent l'uniformité ;
/// 3. quelques rayons de lumière qui tombent de la surface, et un léger grain.
///
/// Le flou est obtenu avec des dégradés radiaux qui vont de la couleur au transparent :
/// c'est bien moins coûteux qu'un vrai filtre de flou, et le rendu est le même.
/// Rien ne bouge : le fond n'est redessiné que quand la fenêtre change de taille.
/// On le place en premier dans un Panel : les contrôles suivants s'affichent par-dessus.
/// </summary>
public class FondAurora : Control
{
    public static readonly StyledProperty<double> GrainProperty =
        AvaloniaProperty.Register<FondAurora, double>(nameof(Grain), defaultValue: 0.05);

    /// <summary>Opacité du grain, de 0 (aucun) à 1.</summary>
    public double Grain
    {
        get => GetValue(GrainProperty);
        set => SetValue(GrainProperty, value);
    }

    /// <summary>Couche 1 : du plus profond au plus clair, en diagonale.</summary>
    private static readonly IBrush Degrade = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#031B3D"), 0.00),  // bleu abyssal
            new GradientStop(Color.Parse("#0B3D91"), 0.30),  // bleu nuit
            new GradientStop(Color.Parse("#1565C0"), 0.55),  // bleu franc
            new GradientStop(Color.Parse("#29B6F6"), 0.85),  // bleu ciel
            new GradientStop(Color.Parse("#B3E5FC"), 1.00),  // bleu lagon très clair
        },
    };

    /// <summary>Couche 2 : les taches, des bleus clairs et foncés, comme l'eau vue de dessous.</summary>
    public List<TacheAurora> Taches { get; } =
    [
        new(Color.Parse("#4FC3F7"), 0.88, 0.08, 0.45, 190),  // halo clair en haut à droite
        new(Color.Parse("#010E24"), 0.02, 0.95, 0.55, 220),  // fosse sombre en bas à gauche
        new(Color.Parse("#1E88E5"), 0.40, 0.30, 0.35, 140),  // bleu vif au centre-haut
        new(Color.Parse("#81D4FA"), 0.70, 0.70, 0.30, 150),  // reflet clair au centre-bas
        new(Color.Parse("#0D47A1"), 0.98, 0.55, 0.30, 170),  // bleu profond à droite
        new(Color.Parse("#E1F5FE"), 1.00, 1.00, 0.25, 200),  // presque blanc dans le coin
    ];

    /// <summary>Couche 3 : les rayons de lumière (position du haut, en fraction de la largeur, et largeur).</summary>
    private static readonly (double X, double Largeur)[] Rayons = [(0.30, 0.06), (0.48, 0.10), (0.72, 0.05)];

    private WriteableBitmap? _grain;

    static FondAurora()
    {
        AffectsRender<FondAurora>(GrainProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<FondAurora>(false);  // les clics passent au travers
    }

    public override void Render(DrawingContext ctx)
    {
        var zone = new Rect(Bounds.Size);
        double grand = Math.Max(zone.Width, zone.Height);

        // 1. Le dégradé de base.
        ctx.FillRectangle(Degrade, zone);

        // 2. Les taches floues.
        foreach (var tache in Taches)
        {
            var c = tache.Couleur;
            var degrade = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(tache.Opacite, c.R, c.G, c.B), 0),
                    new GradientStop(Color.FromArgb((byte)(tache.Opacite * 0.45), c.R, c.G, c.B), 0.5),
                    new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1),
                },
            };
            double rayon = tache.Rayon * grand;
            ctx.DrawEllipse(degrade, null, new Point(tache.X * zone.Width, tache.Y * zone.Height), rayon, rayon);
        }

        // 3a. Les rayons : des bandes inclinées, blanches en haut, qui s'effacent en descendant.
        var lumiere = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(36, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.85),
            },
        };
        foreach (var (x, largeur) in Rayons)
        {
            double haut = x * zone.Width, l = largeur * zone.Width, decalage = 0.25 * zone.Width;
            var bande = new PolylineGeometry(
            [
                new Point(haut, 0), new Point(haut + l, 0),
                new Point(haut + l * 2.5 - decalage, zone.Height), new Point(haut - decalage, zone.Height),
            ], isFilled: true);
            ctx.DrawGeometry(lumiere, null, bande);
        }

        // 3b. Le grain, qui évite les dégradés « en escalier ».
        if (Grain > 0)
        {
            _grain ??= CreerGrain(128);
            var tuiles = new ImageBrush(_grain)
            {
                TileMode = TileMode.Tile,
                DestinationRect = new RelativeRect(0, 0, 128, 128, RelativeUnit.Absolute),
                Opacity = Grain,
            };
            ctx.FillRectangle(tuiles, zone);
        }
    }

    /// <summary>Une petite image de bruit aléatoire, répétée en mosaïque sur tout le fond.</summary>
    private static WriteableBitmap CreerGrain(int taille)
    {
        var image = new WriteableBitmap(new PixelSize(taille, taille), new Vector(96, 96),
                                        PixelFormat.Bgra8888, AlphaFormat.Premul);
        var hasard = new Random(42);
        using var verrou = image.Lock();
        var pixels = new byte[taille * taille * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte gris = (byte)hasard.Next(256);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = gris;
            pixels[i + 3] = 255;
        }
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, verrou.Address, pixels.Length);
        return image;
    }
}
