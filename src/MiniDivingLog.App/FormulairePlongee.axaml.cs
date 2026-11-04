using Avalonia.Controls;
using Avalonia.Interactivity;
using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.App;

/// <summary>
/// Fenêtre de saisie d'une plongée. Elle renvoie la plongée saisie
/// (ou null si on annule) à la fenêtre qui l'a ouverte, via ShowDialog.
/// </summary>
public partial class FormulairePlongee : Window
{
    private readonly IReadOnlyList<Bloc> _autresBlocs = [];

    /// <summary>
    /// Une fonction fournie par la fenêtre principale pour enregistrer la plongée.
    /// Elle renvoie un message d'erreur, ou null si tout s'est bien passé.
    /// </summary>
    public Func<Plongee, string?>? Enregistreur { get; init; }

    // Constructeur sans paramètre requis par le concepteur XAML.
    public FormulairePlongee() : this(null, 1) { }

    /// <param name="plongee">La plongée à modifier, ou null pour une nouvelle.</param>
    /// <param name="numeroPropose">Le numéro proposé pour une nouvelle plongée.</param>
    public FormulairePlongee(Plongee? plongee, int numeroPropose)
    {
        InitializeComponent();
        Title = plongee is null ? "Nouvelle plongée" : $"Modifier la plongée n°{plongee.Numero}";

        DateTime date = plongee?.Date ?? DateTime.Today.AddHours(10);
        ChampNumero.Value = plongee?.Numero ?? numeroPropose;
        ChampDate.SelectedDate = date.Date;
        ChampHeure.SelectedTime = date.TimeOfDay;
        ChampLieu.Text = plongee?.Lieu ?? "";
        ChampDuree.Value = (decimal)(plongee?.DureeMin ?? 45);
        ChampProfMax.Value = (decimal)(plongee?.ProfondeurMaxM ?? 20);
        ChampProfMoy.Value = (decimal)(plongee?.ProfondeurMoyM ?? 12);
        ChampTemperature.Value = (decimal)(plongee?.TemperatureEauC ?? 18);
        CaseTemperature.IsChecked = plongee is null || plongee.TemperatureEauC is not null;
        ChampBinome.Text = plongee?.Binome ?? "";
        ChampCommentaires.Text = plongee?.Commentaires ?? "";

        // On édite le premier bloc ; les éventuels autres sont conservés tels quels.
        Bloc? bloc = plongee?.Blocs.FirstOrDefault();
        _autresBlocs = plongee?.Blocs.Skip(1).ToList() ?? [];
        CaseBloc.IsChecked = plongee is null || bloc is not null;
        ChampVolume.Value = (decimal)(bloc?.VolumeL ?? 12);
        ChampPDebut.Value = (decimal)(bloc?.PressionDebutBar ?? 200);
        ChampPFin.Value = (decimal)(bloc?.PressionFinBar ?? 50);
        ChampO2.Value = (decimal)Math.Round((bloc?.FractionO2 ?? 0.21) * 100);
        ChampHe.Value = (decimal)Math.Round((bloc?.FractionHe ?? 0) * 100);
    }

    private Plongee LireFormulaire()
    {
        var blocs = new List<Bloc>();
        if (CaseBloc.IsChecked == true)
            blocs.Add(new Bloc(Nombre(ChampVolume), Nombre(ChampPDebut), Nombre(ChampPFin),
                               Nombre(ChampO2) / 100, Nombre(ChampHe) / 100));
        blocs.AddRange(_autresBlocs);

        return new Plongee
        {
            Numero = (int)Nombre(ChampNumero),
            Date = (ChampDate.SelectedDate ?? DateTime.Today).Date + (ChampHeure.SelectedTime ?? TimeSpan.Zero),
            Lieu = ChampLieu.Text?.Trim() ?? "",
            DureeMin = Nombre(ChampDuree),
            ProfondeurMaxM = Nombre(ChampProfMax),
            ProfondeurMoyM = Nombre(ChampProfMoy),
            TemperatureEauC = CaseTemperature.IsChecked == true ? Nombre(ChampTemperature) : null,
            Binome = ChampBinome.Text?.Trim() ?? "",
            Commentaires = ChampCommentaires.Text?.Trim() ?? "",
            Blocs = blocs,
        };
    }

    private static double Nombre(NumericUpDown champ) => (double)(champ.Value ?? 0);

    private void Enregistrer_Click(object? sender, RoutedEventArgs e)
    {
        Plongee p = LireFormulaire();

        // 1. Validation des valeurs (classe ValidationPlongee, testée dans le Core).
        var erreurs = ValidationPlongee.Verifier(p);

        // 2. Enregistrement : peut encore échouer, par exemple si le numéro est déjà pris.
        if (erreurs.Count == 0 && Enregistreur?.Invoke(p) is string erreur)
            erreurs.Add(erreur);

        if (erreurs.Count > 0)
        {
            Erreurs.Text = string.Join(Environment.NewLine, erreurs.Select(m => "• " + m));
            Erreurs.IsVisible = true;
            return;
        }

        Close(p);
    }

    private void Annuler_Click(object? sender, RoutedEventArgs e) => Close(null);
}
