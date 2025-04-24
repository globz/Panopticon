using Microsoft.Data.Sqlite;
using LibGit2Sharp;
using System.Diagnostics;

namespace Panopticon;

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
        Game.UI.Theme = ColorTranslator.FromHtml("#050505");
        Game.UI.ForeColor = Color.GhostWhite;

        // Set default Settings values
        Game.Settings.Turn = 1;
        Game.Settings.SQ_Turn = 0.00;
        Game.Settings.Compound_Turn = 1.00;
        Game.Settings.Prefix = "";
        Game.Settings.Suffix = "_TURN_";
        Game.Settings.Auto_commit = true;
        Game.Settings.Replay_Mode = false;

        // Default Git setting
        Git.userName = Environment.GetEnvironmentVariable("GIT_USER_NAME") ?? "Panopticon";
        Git.userEmail = Environment.GetEnvironmentVariable("GIT_USER_EMAIL") ?? "panopticon@kittybomber.com";
        Git.previous_branch_name = "root";

        Application.Run(new Home());
    }

}

public static class Game
{
    public static string? Path { get; set; }
    public static string? Name { get; set; }
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
        public static TreeViewCancelEventHandler? beforeSelectHandler { get; set;}
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
        public static dynamic? FindControlByName(Control parent, string controlName, Type type)
        {
            foreach (Control control in parent.Controls)
            {
                if (type == typeof(Button) && control is Button button && button.Name == controlName)
                {
                    return button; // Found the button
                }

                if (type == typeof(GroupBox) && control is GroupBox groupBox && groupBox.Name == controlName)
                {
                    return groupBox; // Found the groupBox
                }

                // Recursively search in child controls
                dynamic? foundButton = FindControlByName(control, controlName, type);
                if (foundButton != null)
                {
                    return foundButton;
                }
            }
            return null; // No matching button found
        }
        public static TreeNode? GetLastNode(TreeView treeView)
        {
            if (treeView.Nodes.Count == 0)
                return null;

            // Start from the last root node
            TreeNode lastNode = treeView.Nodes[^1];

            // Recursively find the last child node
            while (lastNode.Nodes.Count > 0)
            {
                lastNode = lastNode.Nodes[^1];
            }

            return lastNode;
        }

        // @ HACK to force node selection
        // This will force TreeViewLeft_Node_Selection_Behaviour to always return false
        // This is needed to deal with all of this hacky logic to bypass the lostFocus 
        // default behaviour on node selection.
        public static void ForceNodeSelection(TreeNode? selection)
        {
            if (selection != null)
            {
                Game.UI.SelectedNode = selection;
                Game.UI.TreeViewLeft.SelectedNode = selection;
            }
        }

        public static void TextBox_branch_name_TextChanged(TextBox t, ErrorProvider _errorProvider)
        {
            if (!Git.IsValidGitBranchName(t.Text))
            {
                t.ForeColor = Color.MediumVioletRed;
                _errorProvider.SetError(t,
                    "Invalid branch name. Branch names cannot:\n" +
                    "- Start with a dot\n" +
                    "- Contain consecutive dots, spaces, or ~^:?*[]{}\n" +
                    "- End with / or .lock\n" +
                    "- Contain control characters or be empty");
            }
            else
            {
                t.ForeColor = Color.Black;
                t.BackColor = Color.GhostWhite;
                _errorProvider.SetError(t, string.Empty);
            }
        }

        public static void TextBox_branch_name_Validating(object? sender, System.ComponentModel.CancelEventArgs e, TextBox t)
        {
            if (!Git.IsValidGitBranchName(t.Text) && !string.IsNullOrWhiteSpace(t.Text))
            {
                e.Cancel = true; // Prevent focus change
                                 // ErrorProvider already set by TextChanged, so no need to set again
            }
        }
    }

    public static class Settings
    {
        public static bool Auto_commit { get; set; }
        public static string? Prefix { get; set; }
        public static string? Suffix { get; set; }
        public static int Turn { get; set; }
        public static double SQ_Turn { get; set; }
        public static double Compound_Turn { get; set; }
        public static bool Replay_Mode { get; set; }
    }

}

public static class DB
{
    private static string _databasePath = Game.Path + @"\panopticon.db";
    private static string _databaseSource => $"Data Source={_databasePath}";
    private static SqliteConnection? _connection;

    // Property to access the connection
    public static SqliteConnection Connection
    {
        get
        {
            EnsureConnection();
            return _connection!;
        }
    }

    // Update the game path and refresh the connection
    public static void UpdateGamePath(string? newGamePath)
    {
        if (string.IsNullOrEmpty(newGamePath))
            throw new ArgumentNullException(nameof(newGamePath));

        _databasePath = newGamePath + @"\panopticon.db";
        RefreshConnection();
    }

    // Ensure connection is valid and open
    private static void EnsureConnection()
    {
        if (_connection == null || _connection.State == System.Data.ConnectionState.Closed || _connection.State == System.Data.ConnectionState.Broken)
        {
            RefreshConnection();
        }
        if (_connection?.State != System.Data.ConnectionState.Open)
        {
            _connection?.Open();
        }
    }

    // Refresh the connection
    private static void RefreshConnection()
    {
        if (_connection != null)
        {
            try
            {
                if (_connection.State != System.Data.ConnectionState.Closed)
                {
                    _connection.Close();
                }
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing connection: {ex.Message}");
            }
        }

        try
        {
            _connection = new SqliteConnection(_databaseSource);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating new connection: {ex.Message}");
            throw;
        }
    }

    // Clean up connection resources
    public static void Cleanup()
    {
        if (_connection != null)
        {
            try
            {
                if (_connection.State != System.Data.ConnectionState.Closed)
                {
                    _connection.Close();
                }
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            _connection = null;
        }
    }

    // Create a parameterized query
    public static SqliteCommand Query(string text)
    {
        EnsureConnection();
        var statement = Connection.CreateCommand();
        statement.CommandText = text;
        return statement;
    }

    // Read data from a query
    public static void ReadData(SqliteCommand statement, Action<SqliteDataReader> reader)
    {
        using (var sdr = statement.ExecuteReader())
        {
            while (sdr.Read())
            {
                reader(sdr);
            }
        }
    }

    // Save all settings to the database
    public static void SaveAllSettings()
    {
        using var statement = Query(
            "INSERT INTO settings (game, branch, auto_commit, prefix, suffix, turn, sq_turn, compound_turn, replay_mode) " +
            "VALUES (@game, @branch, @auto_commit, @prefix, @suffix, @turn, @sq_turn, @compound_turn, @replay_mode) " +
            "ON CONFLICT(game, branch) DO UPDATE SET " +
            "auto_commit = @auto_commit, prefix = @prefix, suffix = @suffix, turn = @turn, " +
            "sq_turn = @sq_turn, compound_turn = @compound_turn, replay_mode = @replay_mode");
        statement.Parameters.AddWithValue("@game", Game.Name);
        statement.Parameters.AddWithValue("@branch", Git.CurrentBranch());
        statement.Parameters.AddWithValue("@auto_commit", Game.Settings.Auto_commit);
        statement.Parameters.AddWithValue("@prefix", Game.Settings.Prefix);
        statement.Parameters.AddWithValue("@suffix", Game.Settings.Suffix);
        statement.Parameters.AddWithValue("@turn", Game.Settings.Turn);
        statement.Parameters.AddWithValue("@sq_turn", Game.Settings.SQ_Turn);
        statement.Parameters.AddWithValue("@compound_turn", Game.Settings.Compound_Turn);
        statement.Parameters.AddWithValue("@replay_mode", Game.Settings.Replay_Mode);
        statement.ExecuteNonQuery();
    }

    // Load settings from the database
    public static void LoadSettingsData(SqliteDataReader settings)
    {
        Game.Settings.Auto_commit = Convert.ToBoolean(settings["auto_commit"]);
        Game.Settings.Prefix = settings["prefix"].ToString();
        Game.Settings.Suffix = settings["suffix"].ToString();
        Game.Settings.Turn = Convert.ToInt32(settings["turn"]);
        Game.Settings.SQ_Turn = Convert.ToDouble(settings["sq_turn"]);
        Game.Settings.Compound_Turn = Convert.ToDouble(settings["compound_turn"]);
        Game.Settings.Replay_Mode = Convert.ToBoolean(settings["replay_mode"]);
    }

    // Save timeline data
    public static void SaveTimeline(string title)
    {
        using var statement = Query(
            "INSERT INTO timeline (game, branch, node_name, node_seq, compound_turn, commit_hash) " +
            "VALUES (@game, @branch, @node_name, @node_seq, @compound_turn, @commit_hash)");
        statement.Parameters.AddWithValue("@game", Game.Name);
        statement.Parameters.AddWithValue("@branch", Git.CurrentBranch());
        statement.Parameters.AddWithValue("@node_name", title);
        statement.Parameters.AddWithValue("@node_seq", Git.CommitCount());
        statement.Parameters.AddWithValue("@compound_turn", Game.Settings.Compound_Turn);
        statement.Parameters.AddWithValue("@commit_hash", Git.head_commit_hash);
        statement.ExecuteNonQuery();
    }

    // Save timeline notes
    public static void SaveTimelineNotes(string notes, string? nodeName = null)
    {
        using var statement = Query(
            "INSERT INTO notes (game, branch, node_name, notes) " +
            "VALUES (@game, @branch, @node_name, @notes) " +
            "ON CONFLICT(game, branch, node_name) DO UPDATE SET notes = @notes");
        statement.Parameters.AddWithValue("@game", Game.Name);
        statement.Parameters.AddWithValue("@branch", Git.CurrentBranch());
        statement.Parameters.AddWithValue("@node_name", nodeName ?? Game.UI.SelectedNode?.Name ?? throw new ArgumentNullException(nameof(nodeName)));
        statement.Parameters.AddWithValue("@notes", notes);
        statement.ExecuteNonQuery();
    }
}

public static class Git
{
    public static string? userName { get; set; }
    public static string? userEmail { get; set; }
    public static string? head_commit_hash { get; set; }
    public static string? original_detached_head_commit_hash { get; set; }
    public static string? previous_branch_name { get; set; }

    public static string Commit_title(bool maybe_new_turn)
    {
        if (maybe_new_turn)
        {
            // New turn detected
            return Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
        }
        else
        {
            // Save detected
            return Game.Settings.Prefix + Game.Name + "_SAVE_" + Game.Settings.Compound_Turn.ToString("0.00");
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

    public static bool IsValidGitBranchName(string branchName)
    {
        if (string.IsNullOrEmpty(branchName))
            return false;

        // Git branch names are stored as refs/heads/<branchName>
        string refName = $"refs/heads/{branchName.Trim()}";
        return Reference.IsValidName(refName);
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
        Git.head_commit_hash = repo.Head.Tip.Sha;
        Console.WriteLine($"HEAD Commit hash {repo.Head.Tip.Sha}");
    }

    public static bool HasUnreferencedCommits()
    {

        if (Head_isDetached())
        {

            using var repo = new Repository(Game.Path);

            // Check if current HEAD match the original detached head commit which was save upon detaching HEAD
            bool hasUnreferencedCommits = repo.Head.Tip.Sha != original_detached_head_commit_hash;

            if (hasUnreferencedCommits)
            {
                Console.WriteLine("New commits have been made in detached HEAD.");
                return true;
            }
            else
            {
                Console.WriteLine("No new commits have been made in detached HEAD.");
                return false;
            }
        }
        else
        {
            return false;
        }
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

    public static void Delete_Branch(string? path, string branch)
    {
        using var repo = new Repository(path);
        repo.Branches.Remove(branch);
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

    public static RepositoryStatus Status()
    {
        using var repo = new Repository(Game.Path);
        return repo.RetrieveStatus();
    }

    public static bool CheckIfFileExists(IEnumerable<StatusEntry> modifiedEntries, string file)
    {
        // Check if any entries have a FilePath containing file
        bool hasMatchingEntry = modifiedEntries
            .Any(entry => entry.FilePath != null && entry.FilePath.Contains(file));

        return hasMatchingEntry;
    }

    public static void ResetHard(string? commit_hash = null)
    {
        using var repo = new Repository(Game.Path);
        if (commit_hash != null)
        {
            repo.Reset(ResetMode.Hard, commit_hash);
        }
        else
        {
            repo.Reset(ResetMode.Hard);
        }
    }

    public static BranchResult Detached_Head(string? commit_hash)
    {
        try
        {
            using var repo = new Repository(Game.Path);
            Git.previous_branch_name = repo.Head.FriendlyName;
            Branch detached_head = Commands.Checkout(repo, commit_hash);
            Git.original_detached_head_commit_hash = repo.Head.Tip.Sha;
            Console.WriteLine($"HEAD is detached at commit {repo.Head.Tip.Sha}");
            return new BranchResult { Branch = detached_head };
        }
        catch (Exception ex)
        {
            return new BranchResult { ErrorMessage = ex.Message };
        }
    }

    public static bool Head_isDetached()
    {
        using var repo = new Repository(Game.Path);

        // Check if HEAD is detached
        return repo.Head.Reference.TargetIdentifier == repo.Head.Tip.Sha;
    }

    public class BranchResult
    {
        public Branch? Branch { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess => Branch != null;
    }

    public static BranchResult New_branch(string branch, string commit_hash)
    {
        try
        {
            using var repo = new Repository(Game.Path);
            Branch new_branch = repo.Branches.Add(branch, commit_hash);
            return new BranchResult { Branch = new_branch };
        }
        catch (Exception ex)
        {
            return new BranchResult { ErrorMessage = ex.Message };
        }
    }

    public static BranchResult Checkout(string? checkout_branch)
    {
        try
        {
            using var repo = new Repository(Game.Path);
            Branch current_branch = Commands.Checkout(repo, checkout_branch);
            return new BranchResult { Branch = current_branch };
        }
        catch (LibGit2Sharp.CheckoutConflictException ex)
        {
            return new BranchResult { ErrorMessage = $"You have unsaved turn(s) - please manually create a snapshot. - {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new BranchResult { ErrorMessage = ex.Message };
        }

    }

    public static IEnumerable<string> List_branches()
    {

        var branches = Repository.ListRemoteReferences(Game.Path)
                        .Where(elem => elem.IsLocalBranch)
                        .Select(elem => elem.CanonicalName
                        .Replace("refs/heads/", ""));

        return branches;
    }

    public static int Count_branches()
    {

        var branches = Repository.ListRemoteReferences(Game.Path)
                        .Where(elem => elem.IsLocalBranch)
                        .Select(elem => elem.CanonicalName
                        .Replace("refs/heads/", ""));

        return branches.Count();
    }

    public static BranchResult Switch_c(string newBranchName)
    {
        using var repo = new Repository(Game.Path);

        // Save uncommited changes if available
        // Does not matter if it return false
        Snapshot.Create();

        // Create and checkout new branch in one step
        Branch newBranch = repo.CreateBranch(newBranchName);
        BranchResult current_branch = Checkout(newBranch.FriendlyName);

        // Retrieve hash of current commit
        var head = (SymbolicReference)repo.Refs.Head;
        head_commit_hash = head.ResolveToDirectReference().Target.Sha;
        Console.WriteLine($"Commit hash {head_commit_hash}");
        return current_branch;

    }

    public static void CreateGitIgnore(string? path)
    {
        string gitignore_path = @$"{path}\.gitignore";

        // Check if file does not already exist.
        if (!File.Exists(gitignore_path))
        {
            // Create the file
            using (FileStream fs = File.Create(gitignore_path, 1024))
            {
                byte[] info = new System.Text.UTF8Encoding(true).GetBytes("panopticon.db\n.gitignore");
                fs.Write(info, 0, info.Length);
            }
        }

    }

}
