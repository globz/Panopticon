using Microsoft.Data.Sqlite;
using LibGit2Sharp;

namespace Panopticon;

/* public static class Helper
{
    public static object? GetPropertyValue(this object T, string PropName)
    {
        return T.GetType().GetProperty(PropName)?.GetValue(T, null);

    }

} */
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Set UI theme        
        Game.UI.Theme = ColorTranslator.FromHtml("#313338");
        Game.UI.ForeColor = Color.GhostWhite;

        // Set default Settings values
        Game.Settings.Turn = 0;
        Game.Settings.Prefix = "";
        Game.Settings.Suffix = "_TURN_";
        Game.Settings.Auto_commit = false;
        Game.Settings.Auto_commit_on_save_and_quit = false;

        Application.Run(new Home());
    }

}

public static class Game
{
    public static string? Path { get; set; }
    public static string? Name { get; set; }

    public static class UI
    {
        public static SplitContainer? VerticalSplitContainer { get; set; }
        public static SplitContainer? HorizontalSplitContainer { get; set; }
        public static TreeView? TreeViewLeft { get; set; }
        public static Panel? TopPanel { get; set; }
        public static Panel? BottomPanel { get; set; }
        public static TreeNode? Timeline_settings { get; set; }
        public static TreeNode? Timeline_history { get; set; }
        public static Color Theme { get; set; }
        public static Color ForeColor { get; set; }
        public static TreeNode? SelectedNode { get; set; }
    }

    public static class Settings
    {
        public static bool Auto_commit { get; set; }
        public static bool Auto_commit_on_save_and_quit { get; set; } // TODO might not be needed
        public static string? Prefix { get; set; }
        public static string? Suffix { get; set; }
        public static decimal Turn { get; set; }
    }

}

public static class DB
{
    // Types : https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types

    private static readonly string DatabasePath = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\db\panopticon.db";
    private static readonly string DatabaseSource = $"Data Source={DatabasePath}";
    private static readonly SqliteConnection Connection = new(DatabaseSource);

    public static void Open()
    {
        Connection.Open();
    }

    public static void Close()
    {
        Connection.Close();
    }

    public static SqliteCommand Query(string text)
    {
        var statement = Connection.CreateCommand();
        statement.CommandText = text;
        return statement;
    }

    public static void ReadData(SqliteCommand statement, Action<SqliteDataReader> reader)
    {
        SqliteDataReader sdr = statement.ExecuteReader();
        while (sdr.Read())
        {
            reader(sdr);
        }
    }

    public static void SaveAllSettings()
    {
        Open();
        SqliteCommand statement = Query("INSERT INTO settings (game, auto_commit, prefix, suffix, turn) VALUES (@game, @auto_commit, @prefix, @suffix, @turn) ON CONFLICT(game) DO UPDATE SET auto_commit = @auto_commit, prefix = @prefix, suffix = @suffix, turn = @turn");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@auto_commit", SqliteType.Text).Value = Game.Settings.Auto_commit;
        statement.Parameters.Add("@prefix", SqliteType.Text).Value = Game.Settings.Prefix;
        statement.Parameters.Add("@suffix", SqliteType.Text).Value = Game.Settings.Suffix;
        statement.Parameters.Add("@turn", SqliteType.Text).Value = Game.Settings.Turn;
        statement.ExecuteNonQuery();
        Close();
    }

    public static void LoadSettingsData(SqliteDataReader settings)
    {
        Game.Settings.Auto_commit = Convert.ToBoolean(settings["auto_commit"]);
        Game.Settings.Prefix = (string)settings["prefix"];
        Game.Settings.Suffix = (string)settings["suffix"];
        Game.Settings.Turn = Convert.ToDecimal(settings["turn"]);
    }

    public static void SaveTimeline()
    {
        Open();
        SqliteCommand statement = Query("INSERT INTO timelines (game, branch, node_name, node_seq, commit_hash) VALUES (@game, @branch, @node_name, @node_seq, @commit_hash)");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Git.commit_title;
        statement.Parameters.Add("@node_seq", SqliteType.Integer).Value = Git.CommitCount();
        statement.Parameters.Add("@commit_hash", SqliteType.Integer).Value = Git.head_commit_hash;
        statement.ExecuteNonQuery();
        Close();
    }

    public static void SaveTimelineNotes(string notes)
    {
        Open();
        SqliteCommand statement = Query("INSERT INTO notes (game, branch, node_name, notes) VALUES (@game, @branch, @node_name, @notes) ON CONFLICT(game, branch, node_name) DO UPDATE SET notes = @notes");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
        statement.Parameters.Add("@notes", SqliteType.Text).Value = notes;
        statement.ExecuteNonQuery();
        Close();
    }

}

public static class Git
{
    public static string userName = Environment.GetEnvironmentVariable("GIT_USER_NAME") ?? "Panopticon";
    public static string userEmail = Environment.GetEnvironmentVariable("GIT_USER_EMAIL") ?? "panopticon@kittybomber.com";
    public static string commit_title = Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
    public static string? head_commit_hash { get; set; }
    public static bool Exist(string? path)
    {
        return Repository.IsValid(path);
    }

    public static void Init(string? path)
    {
        Repository.Init(path);
    }

    public static void Commit(string? path, string? title)
    {
        using var repo = new Repository(path);

        // Stage all the working directory changes.
        Commands.Stage(repo, "*");

        // Commit changes
        var author = new Signature(userName, userEmail, DateTimeOffset.Now);
        var committer = author;
        var commit = repo.Commit(title, author, committer);

        // Retrieve hash of current commit
        var head = (SymbolicReference)repo.Refs.Head;
        head_commit_hash = head.ResolveToDirectReference().Target.Sha;
        Console.WriteLine($"Commit hash {head_commit_hash}");
    }

    public static void Delete_Repo(string? path)
    {
        var git_path = path + "/.git";

        var directory = new DirectoryInfo(git_path) { Attributes = FileAttributes.Normal };

        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }

    public static string CurrentBranch()
    {
        using var repo = new Repository(Game.Path);
        return repo.Head.FriendlyName;
    }

    public static int CommitCount()
    {
        using var repo = new Repository(Game.Path);
        return repo.Head.Commits.Count();
    }

}
