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
        Game.Settings.Turn = 1;
        Game.Settings.SQ_Turn = 0.00;
        Game.Settings.Compound_Turn = 1.00;
        Game.Settings.Prefix = "";
        Game.Settings.Suffix = "_TURN_";
        Game.Settings.Auto_commit = true;
        Game.Settings.Auto_commit_on_save_and_quit = false;

        Application.Run(new Home());
    }

}

public static class Game
{
    public static string? Path { get; set; }
    public static string? Name { get; set; }

    public static class Timeline
    {
        public static void Update_Turn(bool maybe_new_turn)
        {
            if (maybe_new_turn)
            {
                // New turn detected
                Settings.Turn++;
            }
            else
            {
                // Save & Quit detected
                if (Settings.Turn > Math.Truncate(Settings.Compound_Turn))
                {
                    // Current turn is now greater than the previous compound turn
                    // Reset SQ_Turn to 0.0
                    Settings.SQ_Turn = 0.00;
                }
                Settings.SQ_Turn += 0.01;
                Settings.Compound_Turn = Math.Round(Settings.Turn + Settings.SQ_Turn, 2);
            }
        }
    }

    public static class UI
    {
        public static SplitContainer VerticalSplitContainer { get; set; } = new SplitContainer();
        public static SplitContainer HorizontalSplitContainer { get; set; } = new SplitContainer();
        public static TreeView TreeViewLeft { get; set; } = new TreeView();
        public static Panel TopPanel { get; set; } = new Panel();
        public static Panel BottomPanel { get; set; } = new Panel();
        public static TreeNode Timeline_settings { get; set; } = new TreeNode();
        public static TreeNode Timeline_history { get; set; } = new TreeNode();
        public static Color Theme { get; set; }
        public static Color ForeColor { get; set; }
        public static TreeNode? SelectedNode { get; set; }
        public static TreeNode? FindNodeByName(TreeNodeCollection nodes, string searchName)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == searchName)
                {

                    return node;
                }

                // Recursively search the child nodes
                TreeNode? found = FindNodeByName(node.Nodes, searchName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
        public static Button? FindButtonByName(Control parent, string buttonName)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button && button.Name == buttonName)
                {
                    return button; // Found the button
                }

                // Recursively search in child controls
                Button? foundButton = FindButtonByName(control, buttonName);
                if (foundButton != null)
                {
                    return foundButton;
                }
            }
            return null; // No matching button found
        }

    }

    public static class Settings
    {
        public static bool Auto_commit { get; set; }
        public static bool Auto_commit_on_save_and_quit { get; set; } // TODO might not be needed
        public static string? Prefix { get; set; }
        public static string? Suffix { get; set; }
        public static int Turn { get; set; }
        public static double SQ_Turn { get; set; }
        public static double Compound_Turn { get; set; }
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
        SqliteCommand statement = Query("INSERT INTO settings (game, branch, auto_commit, prefix, suffix, turn, sq_turn, compound_turn) VALUES (@game, @branch, @auto_commit, @prefix, @suffix, @turn, @sq_turn, @compound_turn) ON CONFLICT(game, branch) DO UPDATE SET auto_commit = @auto_commit, prefix = @prefix, suffix = @suffix, turn = @turn, sq_turn = @sq_turn, compound_turn = @compound_turn");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@auto_commit", SqliteType.Text).Value = Game.Settings.Auto_commit;
        statement.Parameters.Add("@prefix", SqliteType.Text).Value = Game.Settings.Prefix;
        statement.Parameters.Add("@suffix", SqliteType.Text).Value = Game.Settings.Suffix;
        statement.Parameters.Add("@turn", SqliteType.Text).Value = Game.Settings.Turn;
        statement.Parameters.Add("@sq_turn", SqliteType.Text).Value = Game.Settings.SQ_Turn;
        statement.Parameters.Add("@compound_turn", SqliteType.Text).Value = Game.Settings.Compound_Turn;
        statement.ExecuteNonQuery();
        Close();
    }

    public static void LoadSettingsData(SqliteDataReader settings)
    {
        Game.Settings.Auto_commit = Convert.ToBoolean(settings["auto_commit"]);
        Game.Settings.Prefix = (string)settings["prefix"];
        Game.Settings.Suffix = (string)settings["suffix"];
        Game.Settings.Turn = Convert.ToInt32(settings["turn"]);
        Game.Settings.SQ_Turn = (double)settings["sq_turn"];
        Game.Settings.Compound_Turn = (double)settings["compound_turn"];
    }

    public static void SaveTimeline(string title)
    {
        Open();
        SqliteCommand statement = Query("INSERT INTO timelines (game, branch, node_name, node_seq, commit_hash) VALUES (@game, @branch, @node_name, @node_seq, @commit_hash)");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = title;
        statement.Parameters.Add("@node_seq", SqliteType.Integer).Value = Git.CommitCount();
        statement.Parameters.Add("@commit_hash", SqliteType.Integer).Value = Git.head_commit_hash;
        statement.ExecuteNonQuery();
        Close();
    }

    public static void SaveTimelineNotes(string notes, string? nodeName = null)
    {
        Open();
        SqliteCommand statement = Query("INSERT INTO notes (game, branch, node_name, notes) VALUES (@game, @branch, @node_name, @notes) ON CONFLICT(game, branch, node_name) DO UPDATE SET notes = @notes");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = nodeName ?? Game.UI.SelectedNode?.Name;
        statement.Parameters.Add("@notes", SqliteType.Text).Value = notes;
        statement.ExecuteNonQuery();
        Close();
    }

}

public static class Git
{
    public static string userName = Environment.GetEnvironmentVariable("GIT_USER_NAME") ?? "Panopticon";
    public static string userEmail = Environment.GetEnvironmentVariable("GIT_USER_EMAIL") ?? "panopticon@kittybomber.com";
    public static string? head_commit_hash { get; set; }

    public static string Commit_title(bool maybe_new_turn)
    {
        if (maybe_new_turn)
        {
            // New turn detected
            return Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
        }
        else
        {
            // Save & Quit detected
            return Game.Settings.Prefix + Game.Name + "_SQ_" + Game.Settings.Compound_Turn.ToString("0.00");
        }
    }

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
        // A branch may not already exist but will always default to root
        if (Git.Exist(Game.Path))
        {
            using var repo = new Repository(Game.Path);
            return repo.Head.FriendlyName;
        }
        else
        {
            return "root";
        }
    }

    public static int CommitCount()
    {
        using var repo = new Repository(Game.Path);
        return repo.Head.Commits.Count();
    }

    public static Dictionary<string, ChangeKind> Diff()
    {
        var diffs = new Dictionary<string, ChangeKind> { };
        if (Exist(Game.Path))
        {
            using var repo = new Repository(Game.Path);
            foreach (TreeEntryChanges c in repo.Diff.Compare<TreeChanges>())
            {
                diffs.Add(c.Path, c.Status);
            }
        }
        return diffs;
    }

    public static RepositoryStatus? Status()
    {
        RepositoryStatus? status = null;

        if (Exist(Game.Path))
        {
            using var repo = new Repository(Game.Path);
            status = repo.RetrieveStatus();

            if (!status.Any())
            {
                status = null;
            }
        }

        return status;
    }

    public static bool CheckIfFileExists(IEnumerable<StatusEntry> modifiedEntries, string file)
    {
        // Check if any entries have a FilePath containing file
        bool hasMatchingEntry = modifiedEntries
            .Any(entry => entry.FilePath != null && entry.FilePath.Contains(file));

        return hasMatchingEntry;
    }

    public static void ResetHard(string? commit_hash)
    {
        using var repo = new Repository(Game.Path);
        if (commit_hash != null)
        {
            repo.Reset(ResetMode.Hard, commit_hash);
        }
    }

}
