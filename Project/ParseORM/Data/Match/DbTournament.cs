using System.Text;
using Bridgestars.BridgeUtilities;
using Parse;
using RSG;
using Project.Util;

namespace Project.ParseORM.Data;

public class DbTournament : DbObject
{
    protected override Dictionary<string, string> KeyMaps =>
        new()
        {
            {"NbrPlayers", "nbr_players"},
            {"NbrBoards", "nbr_boards"},
            {"TournamentId", "id"},
            {"ResultsFile", "pbn"},
            {"InternalTournament", "internal"},
        };
    public override string ClassName => "Tournament";

    private DbTournament()
    {
        
    }

    /// <summary>
    /// Creates a tournament from a parsed PBN file.
    /// Use for non bridgestars hosted tournaments
    /// </summary>
    public static DbTournament NewExternal(Tournament data)
    {
        var t = new DbTournament();
        t.NbrPlayers = data.Players.Count;
        t.NbrBoards = data.BoardCount;
        t.InternalTournament = false;
        t.Date = data.ContestDate;
        t.Arranger = data.Arranger;
        //t.TournamentId = data.SerialNumber;
        t.Name = data.ContestName;
        
        //name does not have to be unique
        t.ResultsFile = new DbFile(t.Name + "_"+t.Date?.ToShortDateString()+".pbn", data.GetFileContent());
        return t;
    }

    /// use this to save the file as well, using DbOjbectExtensions.SaveAsync(this) will only save the object, not the file and therefore throw exception.
    public IPromise<DbTournament> SaveAsync()
    {
        if(InternalTournament)
            return DbObjectExtensions.SaveAsync(this);
        if (ResultsFile == null) throw new Exception("Results file is null, can't save external tournament without results file.");
        return ResultsFile.SaveAsync().Then(_ =>
            DbObjectExtensions.SaveAsync(this)
        );
    }

    public static DbTournament NewInternalTournament(string name, DateTime date, string arranger)
    {
        var t = new DbTournament();
        t.InternalTournament = true;
        t.Date = date;
        t.Arranger = arranger;
        t.Name = name;
        return t;
    }

    public DateTime? Date {
        get => GetOrElse<DateTime?>(null);
        set => Set(value);
    }
    
    public string Name {
        get => GetOrElse("");
        set => Set(value);
    }
    
    public string Arranger {
        get => GetOrElse("");
        set => Set(value);
    }

    public int TournamentId // Vet ej hur vi ska få tag på denna, hade varit bra att ha
    {
        get => GetOrElse(-1);
        set => Set(value);
    }

    public int NbrPlayers
    {
        get => GetOrElse(0);
        set => Set(value);
    }
    public int NbrBoards
    {
        get => GetOrElse(0);
        set => Set(value);
    }

    public bool InternalTournament
    {
        get => GetOrElse(false);
        set => Set(value);
    }

    public DbFile ResultsFile
    {
        get => GetFile();
        set => SetFile(value);
    }
}

