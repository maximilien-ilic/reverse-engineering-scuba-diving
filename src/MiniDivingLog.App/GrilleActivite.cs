using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using MiniDivingLog.Core.Statistiques;

namespace MiniDivingLog.App;

/// <summary>
/// La grille d'activité « façon GitHub » : une case par jour sur un an, colorée selon
/// le nombre de plongées (5 nuances de bleu), avec les mois, une légende et le total.
/// Les calculs viennent de ActiviteAnnuelle (MiniDivingLog.Core), ici on ne fait que dessiner.
/// </summary>
public class GrilleActivite : UserControl
{
    private const double Case = 13, Ecart = 3, Pas = Case + Ecart, MargeGauche = 34, MargeHaut = 20;

    public static readonly IBrush[] Nuances =
    [
        new SolidColorBrush(Color.Parse("#DCE8F3")),  // 0 : aucune plongée
        new SolidColorBrush(Color.Parse("#90CAF9")),
        new SolidColorBrush(Color.Parse("#42A5F5")),
        new SolidColorBrush(Color.Parse("#1565C0")),
        new SolidColorBrush(Color.Parse("#0A2F6B")),  // 4 : le jour le plus chargé
    ];

    private static readonly string[] Mois =
        ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."];

    public void Afficher(ActiviteAnnuelle activite)
    {
        var dessin = new Canvas
        {
            Width = MargeGauche + activite.Semaines.Count * Pas,
            Height = MargeHaut + 7 * Pas,
        };

        // Les jours de la semaine, une ligne sur deux comme sur GitHub.
        foreach (var (ligne, nom) in new[] { (0, "lun."), (2, "mer."), (4, "ven.") })
            Ajouter(dessin, Texte(nom), 0, MargeHaut + ligne * Pas - 2);

        int moisPrecedent = -1;
        for (int s = 0; s < activite.Semaines.Count; s++)
        {
            var semaine = activite.Semaines[s];

            // Le nom du mois au-dessus de la première semaine qui le contient.
            int mois = semaine[0].Date.Month;
            if (mois != moisPrecedent && s < activite.Semaines.Count - 1)
                Ajouter(dessin, Texte(Mois[mois - 1]), MargeGauche + s * Pas, 0);
            moisPrecedent = mois;

            for (int j = 0; j < 7; j++)
            {
                var jour = semaine[j];
                if (jour.Date > activite.Fin)
                    continue;  // les jours après aujourd'hui restent vides

                var carre = new Rectangle
                {
                    Width = Case,
                    Height = Case,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = Nuances[jour.Niveau],
                };
                ToolTip.SetTip(carre, Bulle(jour));
                Ajouter(dessin, carre, MargeGauche + s * Pas, MargeHaut + j * Pas);
            }
        }

        // En bas : le total à gauche, la légende « Moins ■■■■■ Plus » à droite.
        var legende = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, HorizontalAlignment = HorizontalAlignment.Right };
        legende.Children.Add(Texte("Moins "));
        foreach (var nuance in Nuances)
            legende.Children.Add(new Rectangle { Width = Case, Height = Case, RadiusX = 2, RadiusY = 2, Fill = nuance });
        legende.Children.Add(Texte(" Plus"));

        var bas = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 8, 0, 0) };
        var total = Texte($"{activite.Total} plongée{(activite.Total > 1 ? "s" : "")} sur les 12 derniers mois");
        total.FontWeight = FontWeight.SemiBold;
        bas.Children.Add(total);
        Grid.SetColumn(legende, 1);
        bas.Children.Add(legende);

        Content = new StackPanel
        {
            Width = dessin.Width,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { dessin, bas },
        };
    }

    private static string Bulle(JourActivite jour)
    {
        string date = jour.Date.ToString("dddd d MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
        return jour.Nombre switch
        {
            0 => $"Aucune plongée le {date}",
            1 => $"1 plongée le {date}",
            var n => $"{n} plongées le {date}",
        };
    }

    private static TextBlock Texte(string texte) =>
        new() { Text = texte, FontSize = 11, Foreground = Brushes.DimGray, VerticalAlignment = VerticalAlignment.Center };

    private static void Ajouter(Canvas dessin, Control controle, double x, double y)
    {
        Canvas.SetLeft(controle, x);
        Canvas.SetTop(controle, y);
        dessin.Children.Add(controle);
    }
}
