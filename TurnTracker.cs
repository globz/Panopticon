using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Panopticon;
public partial class TurnTracker
{
    public static void Process()
    {
        var status = Git.Status();
        if (status != null)
        {
            // Check if a turn has been made
            bool maybe_new_turn = Git.CheckIfFileExists(status.Modified, ".trn");

            // Update turn | sq_turn | compound_turn
            Update_Turn(maybe_new_turn);

            // Auto commit if needed
            Commit(Game.Settings.Auto_commit, maybe_new_turn);

            // Refresh Timeline or Snapshot UI
            Refresh_UI(Game.Settings.Auto_commit);

        }
    }
    public static void Update_Turn(bool maybe_new_turn)
    {
        if (maybe_new_turn)
        {
            // New turn detected
            Game.Settings.Turn++;
        }
        else
        {
            // Save & Quit detected
            if (Game.Settings.Turn > Math.Truncate(Game.Settings.Compound_Turn))
            {
                // Current turn is now greater than the previous compound turn
                // Reset SQ_Turn to 0.0
                Game.Settings.SQ_Turn = 0.00;
            }
            Game.Settings.SQ_Turn += 0.01;
            Game.Settings.Compound_Turn = Math.Round(Game.Settings.Turn + Game.Settings.SQ_Turn, 2);
        }
    }

    public static void Refresh_UI(bool auto_commit)
    {
        if (auto_commit)
        {
            Game.UI.TreeViewLeft.Invoke((MethodInvoker)delegate
            {
                // Refresh Timeline nodes
                Timeline.Refresh_Timeline_Nodes();
            });
        }
        else
        {
            // Refresh Snapshot UI
            Game.UI.BottomPanel?.Invoke((MethodInvoker)delegate
            {
                // Call Snapshot.InitializeComponent() to refresh the UI
                Snapshot.InitializeComponent();
            });
        }
    }

    public static void Commit(bool auto_commit, bool maybe_new_turn)
    {
        if (auto_commit)
        {
            // Auto-commit enabled
            Console.WriteLine($"Auto-commit [enabled]");

            // Commit all changes
            Git.Commit(Game.Path, Git.Commit_title(maybe_new_turn));

            // Save current commit information to timelines DB
            DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

            // Save settings (Turn(s) have been updated)
            DB.SaveAllSettings();

            if (!maybe_new_turn)
            {
                // Added default timeline notes for Saves
                DB.SaveTimelineNotes($"Save on turn {Game.Settings.Turn}", Git.Commit_title(maybe_new_turn));
            }
        }
        else
        {
            // Auto-commit disabled
            Console.WriteLine($"Auto-commit [disabled]");
        }
    }

    public static bool Maybe_missed_turn()
    {
        DB.Open();
        SqliteCommand statement = DB.Query(
        "SELECT node_name FROM timelines " +
        "WHERE game = @game AND branch = @branch " +
        "AND node_seq = (" +
        "SELECT MAX(node_seq) FROM timelines " +
        "WHERE game = @game AND branch = @branch)");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        var data = statement.ExecuteScalar();
        string? current_saved_title = (data != null) ? data.ToString() : "";
        DB.Close();

        bool hasDecimal = !string.IsNullOrEmpty(current_saved_title) && DecimalPattern().IsMatch(current_saved_title);

        if (hasDecimal)
        {
            // We are dealing with "SAVE" turn ie; SAVE_45.01
            return current_saved_title == Git.Commit_title(false);
        }
        else
        {
            // We are dealing with turn only ie; TURN_45
            return current_saved_title == Git.Commit_title(true);
        }

    }

    [GeneratedRegex(@"\d+\.\d+$")]
    private static partial Regex DecimalPattern();
}