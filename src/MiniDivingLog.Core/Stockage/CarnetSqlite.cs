using System.Globalization;
using Microsoft.Data.Sqlite;
using MiniDivingLog.Core.Carnet;

namespace MiniDivingLog.Core.Stockage;

/// <summary>
/// Enregistre le carnet dans une base SQLite (un seul fichier).
/// Deux tables : Plongees, et Blocs qui pointe vers sa plongée.
/// </summary>
public sealed class CarnetSqlite : IDisposable
{
    private readonly SqliteConnection _connexion;

    /// <param name="chemin">Fichier de la base, ou ":memory:" pour une base en mémoire.</param>
    public CarnetSqlite(string chemin)
    {
        _connexion = new SqliteConnection($"Data Source={chemin}");
        _connexion.Open();
        Executer("PRAGMA foreign_keys = ON;");
        CreerSchema();
    }

    private void CreerSchema()
    {
        Executer("""
            CREATE TABLE IF NOT EXISTS Plongees (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                Numero           INTEGER NOT NULL UNIQUE,
                Date             TEXT    NOT NULL,
                Lieu             TEXT    NOT NULL,
                DureeMin         REAL    NOT NULL,
                ProfondeurMaxM   REAL    NOT NULL,
                ProfondeurMoyM   REAL    NOT NULL,
                TemperatureEauC  REAL,
                Binome           TEXT    NOT NULL DEFAULT '',
                Commentaires     TEXT    NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS Blocs (
                Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                PlongeeId         INTEGER NOT NULL REFERENCES Plongees(Id) ON DELETE CASCADE,
                Ordre             INTEGER NOT NULL,
                VolumeL           REAL    NOT NULL,
                PressionDebutBar  REAL    NOT NULL,
                PressionFinBar    REAL    NOT NULL,
                FractionO2        REAL    NOT NULL,
                FractionHe        REAL    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PointsProfil (
                PlongeeId     INTEGER NOT NULL REFERENCES Plongees(Id) ON DELETE CASCADE,
                TempsS        REAL    NOT NULL,
                ProfondeurM   REAL    NOT NULL,
                TemperatureC  REAL,
                PRIMARY KEY (PlongeeId, TempsS)
            );
            """);
    }

    /// <summary>Ajoute une plongée et ses blocs, tout ou rien (transaction).</summary>
    public void Ajouter(Plongee p)
    {
        using var transaction = _connexion.BeginTransaction();

        var cmd = _connexion.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Plongees (Numero, Date, Lieu, DureeMin, ProfondeurMaxM, ProfondeurMoyM,
                                  TemperatureEauC, Binome, Commentaires)
            VALUES ($numero, $date, $lieu, $duree, $pmax, $pmoy, $temp, $binome, $commentaires)
            RETURNING Id;
            """;
        cmd.Parameters.AddWithValue("$numero", p.Numero);
        cmd.Parameters.AddWithValue("$date", p.Date.ToString("s", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$lieu", p.Lieu);
        cmd.Parameters.AddWithValue("$duree", p.DureeMin);
        cmd.Parameters.AddWithValue("$pmax", p.ProfondeurMaxM);
        cmd.Parameters.AddWithValue("$pmoy", p.ProfondeurMoyM);
        cmd.Parameters.AddWithValue("$temp", (object?)p.TemperatureEauC ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$binome", p.Binome);
        cmd.Parameters.AddWithValue("$commentaires", p.Commentaires);
        long id = (long)cmd.ExecuteScalar()!;

        for (int i = 0; i < p.Blocs.Count; i++)
        {
            Bloc b = p.Blocs[i];
            var cmdBloc = _connexion.CreateCommand();
            cmdBloc.CommandText = """
                INSERT INTO Blocs (PlongeeId, Ordre, VolumeL, PressionDebutBar, PressionFinBar, FractionO2, FractionHe)
                VALUES ($id, $ordre, $volume, $debut, $fin, $o2, $he);
                """;
            cmdBloc.Parameters.AddWithValue("$id", id);
            cmdBloc.Parameters.AddWithValue("$ordre", i);
            cmdBloc.Parameters.AddWithValue("$volume", b.VolumeL);
            cmdBloc.Parameters.AddWithValue("$debut", b.PressionDebutBar);
            cmdBloc.Parameters.AddWithValue("$fin", b.PressionFinBar);
            cmdBloc.Parameters.AddWithValue("$o2", b.FractionO2);
            cmdBloc.Parameters.AddWithValue("$he", b.FractionHe);
            cmdBloc.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>Toutes les plongées, triées par numéro.</summary>
    public List<Plongee> Lister()
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Plongees ORDER BY Numero;";
        var ids = new List<long>();
        using (var lecteur = cmd.ExecuteReader())
            while (lecteur.Read())
                ids.Add(lecteur.GetInt64(0));

        return ids.Select(Charger).ToList();
    }

    /// <summary>La plongée portant ce numéro, ou null.</summary>
    public Plongee? Trouver(int numero) => IdDe(numero) is long id ? Charger(id) : null;

    private long? IdDe(int numero)
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Plongees WHERE Numero = $numero;";
        cmd.Parameters.AddWithValue("$numero", numero);
        return cmd.ExecuteScalar() as long?;
    }

    /// <summary>Enregistre (ou remplace) le profil d'une plongée déjà présente dans le carnet.</summary>
    public void EnregistrerProfil(int numero, Profil profil)
    {
        long id = IdDe(numero) ?? throw new KeyNotFoundException($"Pas de plongée n°{numero} dans le carnet.");
        using var transaction = _connexion.BeginTransaction();

        var effacer = _connexion.CreateCommand();
        effacer.CommandText = "DELETE FROM PointsProfil WHERE PlongeeId = $id;";
        effacer.Parameters.AddWithValue("$id", id);
        effacer.ExecuteNonQuery();

        // Une seule commande préparée, réutilisée pour chaque point : beaucoup plus rapide.
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = "INSERT INTO PointsProfil (PlongeeId, TempsS, ProfondeurM, TemperatureC) VALUES ($id, $t, $p, $temp);";
        cmd.Parameters.AddWithValue("$id", id);
        var t = cmd.Parameters.Add("$t", SqliteType.Real);
        var prof = cmd.Parameters.Add("$p", SqliteType.Real);
        var temp = cmd.Parameters.Add("$temp", SqliteType.Real);

        foreach (PointProfil point in profil.Points)
        {
            t.Value = point.Temps.TotalSeconds;
            prof.Value = point.ProfondeurM;
            temp.Value = (object?)point.TemperatureC ?? DBNull.Value;
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>Le profil d'une plongée, ou null si elle n'en a pas.</summary>
    public Profil? ChargerProfil(int numero)
    {
        if (IdDe(numero) is not long id)
            return null;

        var cmd = _connexion.CreateCommand();
        cmd.CommandText = "SELECT TempsS, ProfondeurM, TemperatureC FROM PointsProfil WHERE PlongeeId = $id ORDER BY TempsS;";
        cmd.Parameters.AddWithValue("$id", id);

        var points = new List<PointProfil>();
        using var l = cmd.ExecuteReader();
        while (l.Read())
            points.Add(new PointProfil(TimeSpan.FromSeconds(l.GetDouble(0)), l.GetDouble(1),
                                       l.IsDBNull(2) ? null : l.GetDouble(2)));

        return points.Count == 0 ? null : new Profil(points);
    }

    /// <summary>Supprime une plongée (ses blocs et son profil partent avec, grâce à ON DELETE CASCADE).</summary>
    public bool Supprimer(int numero)
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = "DELETE FROM Plongees WHERE Numero = $numero;";
        cmd.Parameters.AddWithValue("$numero", numero);
        return cmd.ExecuteNonQuery() > 0;
    }

    private Plongee Charger(long id)
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = """
            SELECT Numero, Date, Lieu, DureeMin, ProfondeurMaxM, ProfondeurMoyM,
                   TemperatureEauC, Binome, Commentaires
            FROM Plongees WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var l = cmd.ExecuteReader();
        l.Read();

        return new Plongee
        {
            Numero = l.GetInt32(0),
            Date = DateTime.Parse(l.GetString(1), CultureInfo.InvariantCulture),
            Lieu = l.GetString(2),
            DureeMin = l.GetDouble(3),
            ProfondeurMaxM = l.GetDouble(4),
            ProfondeurMoyM = l.GetDouble(5),
            TemperatureEauC = l.IsDBNull(6) ? null : l.GetDouble(6),
            Binome = l.GetString(7),
            Commentaires = l.GetString(8),
            Blocs = ChargerBlocs(id),
        };
    }

    private List<Bloc> ChargerBlocs(long plongeeId)
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = """
            SELECT VolumeL, PressionDebutBar, PressionFinBar, FractionO2, FractionHe
            FROM Blocs WHERE PlongeeId = $id ORDER BY Ordre;
            """;
        cmd.Parameters.AddWithValue("$id", plongeeId);

        var blocs = new List<Bloc>();
        using var l = cmd.ExecuteReader();
        while (l.Read())
            blocs.Add(new Bloc(l.GetDouble(0), l.GetDouble(1), l.GetDouble(2), l.GetDouble(3), l.GetDouble(4)));
        return blocs;
    }

    private void Executer(string sql)
    {
        var cmd = _connexion.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connexion.Dispose();
}
