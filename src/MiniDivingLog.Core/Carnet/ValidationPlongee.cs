namespace MiniDivingLog.Core.Carnet;

/// <summary>
/// Vérifie qu'une plongée saisie à la main est cohérente avant de l'enregistrer.
/// Renvoie la liste des problèmes trouvés (vide si tout va bien).
/// </summary>
public static class ValidationPlongee
{
    public static List<string> Verifier(Plongee p)
    {
        var erreurs = new List<string>();

        if (p.Numero <= 0)
            erreurs.Add("Le numéro doit être positif.");
        if (string.IsNullOrWhiteSpace(p.Lieu))
            erreurs.Add("Le lieu est obligatoire.");
        if (p.Date > DateTime.Now.AddDays(1))
            erreurs.Add("La date est dans le futur.");
        if (p.DureeMin <= 0)
            erreurs.Add("La durée doit être positive.");
        if (p.ProfondeurMaxM <= 0)
            erreurs.Add("La profondeur maximale doit être positive.");
        if (p.ProfondeurMoyM < 0 || p.ProfondeurMoyM > p.ProfondeurMaxM)
            erreurs.Add("La profondeur moyenne doit être comprise entre 0 et la profondeur maximale.");
        if (p.TemperatureEauC is < -2 or > 40)
            erreurs.Add("La température de l'eau doit être entre -2 et 40 °C.");

        for (int i = 0; i < p.Blocs.Count; i++)
        {
            Bloc b = p.Blocs[i];
            string nom = p.Blocs.Count == 1 ? "Bloc" : $"Bloc {i + 1}";
            if (b.VolumeL <= 0)
                erreurs.Add($"{nom} : le volume doit être positif.");
            if (b.PressionFinBar < 0 || b.PressionFinBar > b.PressionDebutBar)
                erreurs.Add($"{nom} : la pression de fin doit être entre 0 et la pression de début.");
            if (b.FractionO2 is < 0.08 or > 1)
                erreurs.Add($"{nom} : l'O2 doit être entre 8 et 100 %.");
            if (b.FractionHe < 0 || b.FractionO2 + b.FractionHe > 1)
                erreurs.Add($"{nom} : O2 + hélium ne peut pas dépasser 100 %.");
        }

        return erreurs;
    }
}
