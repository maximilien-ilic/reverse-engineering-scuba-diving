using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MiniDivingLog.Core.Calculs;
using MiniDivingLog.Core.Calculs.Deco;
using MiniDivingLog.Core.Carnet;
using MiniDivingLog.Core.Import;
using MiniDivingLog.Core.Statistiques;
using MiniDivingLog.Core.Stockage;

namespace MiniDivingLog.App;

/// <summary>
/// La fenêtre principale. Elle ne fait aucun calcul elle-même : elle affiche
/// ce que renvoient les classes de MiniDivingLog.Core (étapes 1 à 5).
/// </summary>
public partial class MainWindow : Window
{
    private readonly CarnetSqlite _carnet;

    /// <summary>~/.local/share/MiniDivingLog/carnet.db sous Linux.</summary>
    public static string CheminCarnetParDefaut()
    {
        string dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniDivingLog");
        Directory.CreateDirectory(dossier);
        return Path.Combine(dossier, "carnet.db");
    }

    // Constructeur sans paramètre requis par le concepteur XAML.
    public MainWindow() : this(":memory:") { }

    public MainWindow(string cheminCarnet)
    {
        InitializeComponent();
        _carnet = new CarnetSqlite(cheminCarnet);
        Closed += (_, _) => _carnet.Dispose();
        Rafraichir();
        Calculer_Click(null, new RoutedEventArgs());
    }

    private void Rafraichir(int? numeroASelectionner = null)
    {
        var plongees = _carnet.Lister();
        ListePlongees.ItemsSource = plongees;
        ListePlongees.SelectedItem =
            plongees.FirstOrDefault(p => p.Numero == numeroASelectionner) ?? plongees.LastOrDefault();

        var s = new StatistiquesCarnet(plongees);
        Stats.Text = s.Nombre == 0
            ? "Carnet vide : importe un fichier UDDF pour commencer."
            : $"{s.Nombre} plongées · {s.TempsTotal.TotalHours:0.#} h sous l'eau · " +
              $"record : {s.ProfondeurMax!.Valeur:0.#} m (n°{s.ProfondeurMax.NumeroPlongee}) · " +
              (s.SacMoyen is double sac ? $"SAC moyen {sac:0.0} L/min" : "SAC moyen : —");
    }

    private void Liste_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ListePlongees.SelectedItem is not Plongee p)
        {
            Titre.Text = Details.Text = "";
            Profil.Profil = null;
            return;
        }

        Titre.Text = string.IsNullOrEmpty(p.Lieu) ? $"Plongée n°{p.Numero}" : $"Plongée n°{p.Numero} — {p.Lieu}";
        var lignes = new List<string>
        {
            $"{p.Date:dddd d MMMM yyyy, HH:mm} · {p.DureeMin:0} min · max {p.ProfondeurMaxM:0.#} m · moyenne {p.ProfondeurMoyM:0.#} m",
        };
        if (p.TemperatureEauC is double t)
            lignes.Add($"Eau : {t:0.#} °C");
        foreach (var b in p.Blocs)
            lignes.Add($"Bloc {b.VolumeL:0} L, {b.PressionDebutBar:0} → {b.PressionFinBar:0} bar, " +
                       $"O2 {b.FractionO2:P0} (MOD {Nitrox.Mod(b.FractionO2):0} m à 1,4 bar)");
        if (p.CalculerSac() is double sac)
            lignes.Add($"SAC : {sac:0.0} L/min");

        Profil.Profil = _carnet.ChargerProfil(p.Numero);
        if (Profil.Profil is { } profil && profil.RemonteesRapides().Count > 0)
            lignes.Add($"⚠ Remontée trop rapide : jusqu'à {profil.VitesseRemonteeMax:0} m/min (en rouge sur le profil)");
        if (!string.IsNullOrWhiteSpace(p.Commentaires))
            lignes.Add(p.Commentaires);

        Details.Text = string.Join(Environment.NewLine, lignes);
    }

    private async void Importer_Click(object? sender, RoutedEventArgs e)
    {
        var fichiers = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choisir un fichier UDDF",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Fichiers UDDF") { Patterns = ["*.uddf", "*.xml"] }],
        });
        if (fichiers.Count == 0 || fichiers[0].TryGetLocalPath() is not string chemin)
            return;

        try
        {
            int ajoutees = 0, ignorees = 0;
            int? derniere = null;
            foreach (var (plongee, profil) in ImportUddf.LireFichier(chemin))
            {
                if (_carnet.Trouver(plongee.Numero) is not null)
                {
                    ignorees++;  // déjà dans le carnet : on ne l'écrase pas
                    continue;
                }
                _carnet.Ajouter(plongee);
                if (profil is not null)
                    _carnet.EnregistrerProfil(plongee.Numero, profil);
                ajoutees++;
                derniere = plongee.Numero;
            }

            Message.Text = $"{ajoutees} plongée(s) importée(s)" + (ignorees > 0 ? $", {ignorees} déjà présente(s)" : "");
            Rafraichir(derniere);
        }
        catch (Exception ex) when (ex is FormatException or System.Xml.XmlException)
        {
            Message.Text = $"Fichier illisible : {ex.Message}";
        }
    }

    private void Supprimer_Click(object? sender, RoutedEventArgs e)
    {
        if (ListePlongees.SelectedItem is not Plongee p)
            return;
        _carnet.Supprimer(p.Numero);
        Message.Text = $"Plongée n°{p.Numero} supprimée";
        Rafraichir();
    }

    private void Calculer_Click(object? sender, RoutedEventArgs e)
    {
        double profondeur = (double)(ChampProfondeur.Value ?? 0);
        double duree = (double)(ChampDuree.Value ?? 0);
        var gaz = new Melange((double)(ChampO2.Value ?? 21) / 100, (double)(ChampHe.Value ?? 0) / 100);
        var planificateur = new Planificateur
        {
            GfBas = (double)(ChampGfBas.Value ?? 30) / 100,
            GfHaut = (double)(ChampGfHaut.Value ?? 70) / 100,
        };

        if (gaz.FractionN2 < 0)
        {
            AfficherErreurPlan("O2 + hélium ne peuvent pas dépasser 100 %.");
            return;
        }

        try
        {
            PlanDeco plan = planificateur.Planifier(profondeur, duree, gaz);
            var lignes = new List<string>
            {
                $"Gaz : {gaz.FractionO2:P0} O2, {gaz.FractionHe:P0} He",
                $"Limite sans palier à {profondeur:0} m : {planificateur.LimiteSansPalier(profondeur, gaz)} min",
                "",
            };

            // Vérifications Nitrox (étape 1).
            if (Nitrox.PpO2(gaz.FractionO2, profondeur) > 1.4)
                lignes.Add($"⚠ PpO2 au fond : {Nitrox.PpO2(gaz.FractionO2, profondeur):0.00} bar, au-delà de 1,4 " +
                           $"(MOD de ce gaz : {Nitrox.Mod(gaz.FractionO2):0} m)");
            if (gaz.FractionHe > 0)
                lignes.Add($"Profondeur narcotique équivalente : {Nitrox.End(gaz.FractionHe, profondeur):0} m");

            if (plan.SansPalier)
                lignes.Add("Pas de palier obligatoire.");
            else
            {
                lignes.Add("Paliers :");
                lignes.AddRange(plan.Paliers.Select(p => $"   {p.ProfondeurM,3:0} m   {p.DureeMin,3} min"));
            }
            lignes.Add("");
            lignes.Add($"Durée totale : {plan.DureeTotaleMin:0} min");

            ResultatPlan.Text = string.Join(Environment.NewLine, lignes);
            ProfilPlan.Profil = plan.Deroulement;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            AfficherErreurPlan(ex.Message);
        }
    }

    private void AfficherErreurPlan(string message)
    {
        ResultatPlan.Text = $"⚠ {message}";
        ProfilPlan.Profil = null;
    }
}
