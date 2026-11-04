using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MiniDivingLog.App;

/// <summary>Une « tache » de couleur du fond aurora.</summary>
/// <param name="X">Position du centre, en fraction de la largeur (0 à 1).</param>
/// <param name="Y">Position du centre, en fraction de la hauteur (0 à 1).</param>
/// <param name="Rayon">Rayon, en fraction de la plus grande dimension.</param>
/// <param name="Periode">Durée d'un aller-retour de la dérive, en secondes.</param>
public record TacheAurora(Color Couleur, double X, double Y, double Rayon, double Periode);

/// <summary>
/// Fond « aurora » recréé à la main, sans WebGL : quelques grandes taches de couleur très floues
/// qui dérivent et respirent lentement, plus un léger grain qui casse les dégradés en escalier.
///
/// Le flou est obtenu avec des dégradés radiaux qui vont de la couleur au transparent :
/// c'est bien moins coûteux qu'un vrai filtre de flou, et le rendu est le même.
/// On le place en premier dans un Panel : les contrôles suivants s'affichent par-dessus.
/// </summary>
public class FondAurora : Control
{
    public static readonly StyledProperty<bool> AnimeProperty =
        AvaloniaProperty.Register<FondAurora, bool>(nameof(Anime), defaultValue: true);

    public static readonly StyledProperty<double> GrainProperty =
        AvaloniaProperty.Register<FondAurora, double>(nameof(Grain), defaultValue: 0.06);

    /// <summary>false = les taches restent immobiles (pour ceux que le mouvement gêne).</summary>
    public bool Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }

    /// <summary>Opacité du grain, de 0 (aucun) à 1.</summary>
    public double Grain
    {
        get => GetValue(GrainProperty);
        set => SetValue(GrainProperty, value);
    }

    /// <summary>Les taches : des bleus clairs et foncés, comme l'eau vue de dessous.</summary>
    public List<TacheAurora> Taches { get; } =
    [
        new(Color.Parse("#0D47A1"), 0.15, 0.20, 0.55, 46),
        new(Color.Parse("#1E88E5"), 0.80, 0.15, 0.50, 38),
        new(Color.Parse("#4FC3F7"), 0.55, 0.75, 0.45, 52),
        new(Color.Parse("#81D4FA"), 0.10, 0.85, 0.40, 41),
        new(Color.Parse("#01579B"), 0.90, 0.80, 0.50, 57),
    ];

    private static readonly IBrush CouleurDeFond = new SolidColorBrush(Color.Parse("#E3F2FD"));
    private readonly DateTime _depart = DateTime.Now;
    private WriteableBitmap? _grain;

    static FondAurora()
    {
        AffectsRender<FondAurora>(AnimeProperty, GrainProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<FondAurora>(false);  // les clics passent au travers
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        DemanderImageSuivante();
    }

    private void DemanderImageSuivante()
    {
        // À chaque image affichée par l'écran, on redessine : c'est ce qui fait bouger les taches.
        if (Anime && TopLevel.GetTopLevel(this) is { } fenetre)
            fenetre.RequestAnimationFrame(_ =>
            {
                InvalidateVisual();
                DemanderImageSuivante();
            });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AnimeProperty && Anime)
            DemanderImageSuivante();
    }

    public override void Render(DrawingContext ctx)
    {
        var zone = new Rect(Bounds.Size);
        ctx.FillRectangle(CouleurDeFond, zone);

        double t = Anime ? (DateTime.Now - _depart).TotalSeconds : 0;
        double grand = Math.Max(zone.Width, zone.Height);

        foreach (var tache in Taches)
        {
            // Dérive lente : un petit mouvement circulaire, différent pour chaque tache,
            // et une « respiration » de la taille (± 12 %).
            double angle = 2 * Math.PI * t / tache.Periode;
            double cx = (tache.X + 0.08 * Math.Sin(angle)) * zone.Width;
            double cy = (tache.Y + 0.06 * Math.Cos(angle * 0.8)) * zone.Height;
            double rayon = tache.Rayon * grand * (1 + 0.12 * Math.Sin(angle * 1.3));

            var degrade = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(150, tache.Couleur.R, tache.Couleur.G, tache.Couleur.B), 0),
                    new GradientStop(Color.FromArgb(60, tache.Couleur.R, tache.Couleur.G, tache.Couleur.B), 0.5),
                    new GradientStop(Color.FromArgb(0, tache.Couleur.R, tache.Couleur.G, tache.Couleur.B), 1),
                },
            };
            ctx.DrawEllipse(degrade, null, new Point(cx, cy), rayon, rayon);
        }

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
