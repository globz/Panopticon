using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Data.Sqlite;

namespace Panopticon;
public class TimeTravel
{
    public static List<string> Undo(bool permanent = false)
    {
        // Retrieve current selected node sequence
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT node_seq FROM timelines WHERE game = @game AND branch = @branch AND node_name = @node_name");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
        var data = statement.ExecuteScalar();
        DB.Close();

        int? node_seq = Convert.ToInt32(data);

        // Retrieve parent node sequence
        int? parent_node_seq = node_seq - 1;

        // Retrieve the commit hash of the node that HEAD will point to.
        DB.Open();
        statement = DB.Query("SELECT commit_hash FROM timelines WHERE game = @game AND branch = @branch AND node_seq = @node_seq");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_seq", SqliteType.Integer).Value = parent_node_seq;
        data = statement.ExecuteScalar();
        DB.Close();

        string? commit_hash = data?.ToString();

        // Retrieve max node_seq associated to this timeline & branch
        DB.Open();
        statement = DB.Query("SELECT MAX(node_seq) FROM timelines WHERE game = @game AND branch = @branch");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        data = statement.ExecuteScalar();
        DB.Close();

        int? max_node_seq = Convert.ToInt32(data);

        // Calculate how many nodes will be deleted
        int? absolute_delta = max_node_seq - parent_node_seq;
        Console.WriteLine($"Absolute Delta: {absolute_delta}");

        int? node_seq_start = 0;
        int? node_seq_end = 0;
        if (node_seq == max_node_seq)
        {
            // This node is HEAD therefor we only need to point to ourself
            node_seq_start = node_seq;
            node_seq_end = node_seq;
            Console.WriteLine($"HEAD node_seq: {node_seq}");
        }
        else
        {
            // Build range sequence to be deleted
            int[] range_seq_to_be_deleted = Enumerable.Range((int)node_seq, (int)absolute_delta).ToArray();
            range_seq_to_be_deleted.ToList().ForEach(i => Console.WriteLine($" range_seq_to_be_deleted: {i.ToString()}"));
            node_seq_start = range_seq_to_be_deleted.First();
            node_seq_end = range_seq_to_be_deleted.Max();
        }

        // Retrieve node_name(s) associated with this commit_hash along with subsequent nodes
        List<string> timeline_nodes_name = new List<string>();
        DB.Open();
        statement = DB.Query("SELECT node_name FROM timelines WHERE game = @game AND branch = @branch AND node_seq between @node_seq_start AND @node_seq_end ORDER BY node_seq");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_seq_start", SqliteType.Integer).Value = node_seq_start;
        statement.Parameters.Add("@node_seq_end", SqliteType.Integer).Value = node_seq_end;
        SqliteDataReader timeline = statement.ExecuteReader();

        while (timeline.Read())
        {
            timeline_nodes_name.Add((string)timeline["node_name"]);
        }
        DB.Close();

        if (node_seq == 1)
        {
            timeline_nodes_name.Clear();
        }

        // DESTRUCTIVE actions below:
        if (permanent)
        {
            // Do not undo the first commit (Delete and rebuild a new timeline instead!)
            if (node_seq != 1)
            {
                // git reset --hard using the parent_node commit_hash
                Git.ResetHard(commit_hash);

                // Delete timeline node(s) associated with this commit_hash along with subsequent nodes (UI + DB)
                TreeNode? node_to_delete = new TreeNode();
                timeline_nodes_name.ForEach(i =>
                {
                    node_to_delete = Game.UI.FindNodeByName(Game.UI.TreeViewLeft.Nodes, i);
                    if (node_to_delete != null)
                    {
                        Game.UI.TreeViewLeft.Nodes.Remove(node_to_delete);
                    }
                });

                DB.Open();
                statement = DB.Query("DELETE FROM timelines WHERE game = @game AND branch = @branch AND node_seq between @node_seq_start AND @node_seq_end");
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.Parameters.Add("@node_seq_start", SqliteType.Integer).Value = node_seq_start;
                statement.Parameters.Add("@node_seq_end", SqliteType.Integer).Value = node_seq_end;
                statement.ExecuteNonQuery();
                DB.Close();

                // Retrieve the compound turn of the node that HEAD will point to.
                DB.Open();
                statement = DB.Query("SELECT compound_turn FROM timelines WHERE game = @game AND branch = @branch AND node_seq = @node_seq");
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.Parameters.Add("@node_seq", SqliteType.Integer).Value = parent_node_seq;
                data = statement.ExecuteScalar();
                DB.Close();

                double compoundTurnValue = Convert.ToDouble(data ?? 0.00);
                string compoundTurnString = compoundTurnValue.ToString("0.00");

                // Rewind the Turn(s) settings values
                string pattern = @"(\d+)\.(\d+)";
                var match = Regex.Match(compoundTurnString, pattern);

                int rewinded_turn = int.Parse(match.Groups[1].Value);
                double rewinded_sq_turn = double.Parse(match.Groups[2].Value) / 100.0;
                double rewinded_compound_turn = rewinded_turn + rewinded_sq_turn;

                DB.Open();
                statement = DB.Query("UPDATE settings SET turn = @turn, sq_turn = @sq_turn, compound_turn = @compound_turn WHERE game = @game AND branch = @branch");
                statement.Parameters.Add("@turn", SqliteType.Integer).Value = rewinded_turn;
                statement.Parameters.Add("@sq_turn", SqliteType.Integer).Value = rewinded_sq_turn;
                statement.Parameters.Add("@compound_turn", SqliteType.Text).Value = rewinded_compound_turn;
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.ExecuteNonQuery();
                DB.Close();

                // Reload settings
                Timeline.Retrieve_Settings();

            }
        }
        return timeline_nodes_name;
    }

    public static void BranchOff(string branch_name)
    {
        Console.WriteLine(branch_name);

        // Retrieve information tied to this node
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT node_seq, compound_turn, commit_hash FROM timelines WHERE game = @game AND branch = @branch AND node_name = @node_name");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
        SqliteDataReader node_info = statement.ExecuteReader();

        int node_seq_end = 0;
        string commit_hash = "";
        double compoundTurnValue = 0.0;
        while (node_info.Read())
        {
            node_seq_end = Convert.ToInt32(node_info["node_seq"]);
            commit_hash = (string)node_info["commit_hash"];
            compoundTurnValue = Convert.ToDouble(node_info["compound_turn"] ?? 0.00);
        }
        DB.Close();

        string compoundTurnString = compoundTurnValue.ToString("0.00");
        Console.WriteLine(node_seq_end);
        Console.WriteLine(commit_hash);
        Console.WriteLine(compoundTurnString);

        // Keep a reference of the branch BEFORE CHECKOUT
        string branch_before_checkout = Git.CurrentBranch();

        // Create new branch @ <commit hash>
        var new_branch_result = Git.New_branch(branch_name, commit_hash);

        if (!new_branch_result.IsSuccess)
        {
            MessageBox.Show($"An error occured while attempting to create a branch - {new_branch_result.ErrorMessage}");
            return;
        }

        // Checkout new branch
        var checkout_branch = Git.Checkout(new_branch_result.Branch?.FriendlyName);

        if (checkout_branch.IsSuccess)
        {

            var new_branch = checkout_branch.Branch;

            // Retrieve turn, sq_turn & compound_turn with the compound_turn found in timelines
            // Keep default prefix & suffix
            // Keep default auto_commit value
            string pattern = @"(\d+)\.(\d+)";
            var match = Regex.Match(compoundTurnString, pattern);

            int branch_turn = int.Parse(match.Groups[1].Value);
            double branch_sq_turn = double.Parse(match.Groups[2].Value) / 100.0;
            double branch_compound_turn = branch_turn + branch_sq_turn;

            // Save newbranch settings
            DB.Open();
            statement = DB.Query("INSERT INTO settings (game, branch, auto_commit, prefix, suffix, turn, sq_turn, compound_turn, replay_mode) VALUES (@game, @branch, @auto_commit, @prefix, @suffix, @turn, @sq_turn, @compound_turn, @replay_mode) ON CONFLICT(game, branch) DO UPDATE SET auto_commit = @auto_commit, prefix = @prefix, suffix = @suffix, turn = @turn, sq_turn = @sq_turn, compound_turn = @compound_turn, replay_mode = @replay_mode");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
            statement.Parameters.Add("@auto_commit", SqliteType.Text).Value = Game.Settings.Auto_commit;
            statement.Parameters.Add("@prefix", SqliteType.Text).Value = Game.Settings.Prefix;
            statement.Parameters.Add("@suffix", SqliteType.Text).Value = Game.Settings.Suffix;
            statement.Parameters.Add("@turn", SqliteType.Integer).Value = branch_turn;
            statement.Parameters.Add("@sq_turn", SqliteType.Text).Value = branch_sq_turn;
            statement.Parameters.Add("@compound_turn", SqliteType.Text).Value = branch_compound_turn;
            statement.Parameters.Add("@replay_mode", SqliteType.Text).Value = Game.Settings.Replay_Mode;
            statement.ExecuteNonQuery();
            DB.Close();

            // Reload all settings from this current branch
            Timeline.Retrieve_Settings();

            // Find all previous node(s) FROM 
            // This information has to come from branch_before_checkout since this is our reference branch
            // Persist them to timelines table under this new branch
            int node_seq_start = 1;
            DB.Open();
            SqliteCommand insertStatement = DB.Query(@"
            INSERT INTO timelines (game, branch, node_name, node_seq, compound_turn, commit_hash)
            SELECT @game, @new_branch, node_name, node_seq, compound_turn, commit_hash
            FROM timelines 
            WHERE game = @game 
            AND branch = @old_branch 
            AND node_seq BETWEEN @node_seq_start AND @node_seq_end
            ORDER BY node_seq");

            insertStatement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            insertStatement.Parameters.Add("@new_branch", SqliteType.Text).Value = new_branch?.FriendlyName;
            insertStatement.Parameters.Add("@old_branch", SqliteType.Text).Value = branch_before_checkout;
            insertStatement.Parameters.Add("@node_seq_start", SqliteType.Integer).Value = node_seq_start;
            insertStatement.Parameters.Add("@node_seq_end", SqliteType.Integer).Value = node_seq_end;
            insertStatement.ExecuteNonQuery();
            DB.Close();

            // Refresh timeline UI
            Timeline.Refresh_Timeline_Nodes();

            // Refresh timeline TopPanel
            Timeline.Initialize_Timeline_Root();

        }
        else
        {
            MessageBox.Show($"An error occured while attempting to switch branch - {checkout_branch.ErrorMessage}");
            return;
        }

    }

    public static void SwitchBranch(string branch)
    {

        var checkout_branch = Git.Checkout(branch);

        if (!checkout_branch.IsSuccess)
        {
            MessageBox.Show($"An error occured while attempting to switch branch - {checkout_branch.ErrorMessage}");
            return;
        }

        // Reload all settings from this current branch
        Timeline.Retrieve_Settings();

        // Refresh timeline UI
        Timeline.Refresh_Timeline_Nodes();

        // Refresh timeline TopPanel (switch between Delete Timeline & Delete Branch button)
        Timeline.Initialize_Timeline_Root();

        // Enable Manual snapshot Node OR Replay Mode Node if needed
        Timeline.Manual_Snapshot_Node();
        Timeline.Replay_Mode_Node();

    }

    public static class ReplayMode
    {

        public static bool Enable()
        {
            // Enable Replay mode (detached HEAD)
            // git checkout <commit hash>

            /*             // Retrieve current selected node sequence
                        DB.Open();
                        SqliteCommand statement = DB.Query("SELECT node_seq FROM timelines WHERE game = @game AND branch = @branch AND node_name = @node_name");
                        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
                        var data = statement.ExecuteScalar();
                        DB.Close();

                        int? node_seq = Convert.ToInt32(data);

                        // Retrieve the commit hash of the node that HEAD will point to.
                        DB.Open();
                        statement = DB.Query("SELECT commit_hash FROM timelines WHERE game = @game AND branch = @branch AND node_seq = @node_seq");
                        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                        statement.Parameters.Add("@node_seq", SqliteType.Integer).Value = node_seq;
                        data = statement.ExecuteScalar();
                        DB.Close(); */

            // string? commit_hash = data?.ToString();

            // Retrieve information tied to this node
            DB.Open();
            SqliteCommand statement = DB.Query("SELECT node_seq, compound_turn, commit_hash FROM timelines WHERE game = @game AND branch = @branch AND node_name = @node_name");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
            statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
            SqliteDataReader node_info = statement.ExecuteReader();

            int node_seq_end = 0;
            string commit_hash = "";
            double compoundTurnValue = 0.0;
            while (node_info.Read())
            {
                node_seq_end = Convert.ToInt32(node_info["node_seq"]);
                commit_hash = (string)node_info["commit_hash"];
                compoundTurnValue = Convert.ToDouble(node_info["compound_turn"] ?? 0.00);
            }
            DB.Close();

            string compoundTurnString = compoundTurnValue.ToString("0.00");
            Console.WriteLine(node_seq_end);
            Console.WriteLine(commit_hash);
            Console.WriteLine(compoundTurnString);

            // Retrieve turn, sq_turn & compound_turn with the compound_turn found in timelines
            // Keep default prefix & suffix
            // Keep default auto_commit value
            string pattern = @"(\d+)\.(\d+)";
            var match = Regex.Match(compoundTurnString, pattern);

            int branch_turn = int.Parse(match.Groups[1].Value);
            double branch_sq_turn = double.Parse(match.Groups[2].Value) / 100.0;
            double branch_compound_turn = branch_turn + branch_sq_turn;

            // Detach HEAD to targetted commit hash
            Branch replay_branch = Git.Detached_Head(commit_hash);
            Console.WriteLine(replay_branch); //TODO remove

            // Disable auto-commit, user may or not want to persist this replay session
            // By forcing manual mode we are now allowing this choice.
            Game.Settings.Auto_commit = false;

            // Enable Replay Mode setting
            Game.Settings.Replay_Mode = true;

            // Update Turn
            Game.Settings.Turn = node_seq_end; //TODO do I use node_seq_end or branch_turn??
            // TODO Update SQ_turn
            Game.Settings.SQ_Turn = branch_sq_turn;

            // Update Compound turn
            Game.Settings.Compound_Turn = branch_compound_turn;

            // Persist Replay Mode setting
            DB.SaveAllSettings();

            // Reload all settings from this current branch
            Timeline.Retrieve_Settings();

            // Refresh timeline UI
            Timeline.Refresh_Timeline_Nodes();

            // Refresh timeline TopPanel
            Timeline.Initialize_Timeline_Root();

            // Enable Replay Mode Node OR  disable Manual snapshot Node if needed
            Timeline.Manual_Snapshot_Node();
            Timeline.Replay_Mode_Node();

            return true;
        }

        public static bool Disable()
        {

            // Disable Replay Mode setting
            Game.Settings.Replay_Mode = false;

            // Persist Replay Mode setting
            DB.Open();
            SqliteCommand statement = DB.Query("DELETE FROM settings WHERE game = @game AND branch = @branch");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.Parameters.Add("@branch", SqliteType.Text).Value = "(no branch)";
            statement.ExecuteNonQuery();
            DB.Close();

            return true;
        }

        public static bool Discard()
        {
            // Discard all changes made while Replay mode was active - Go back to previous branch
            // git checkout -- * or git reset --hard

            // Refresh timeline UI
            Timeline.Refresh_Timeline_Nodes();

            // Refresh timeline TopPanel (switch between Delete Timeline & Delete Branch button)
            Timeline.Initialize_Timeline_Root();

            return true;
        }

        public static bool Persist(string branch_name, bool maybe_new_turn)
        {
            // Everything done during Replay mode may be saved to a new branch
            // git switch -c <new-branch-name>

            // Commit changes from DETACHED HEAD, create new branch and checkout
            var new_branch_result = Git.Switch_c(branch_name, Git.Commit_title(maybe_new_turn));

            if (!new_branch_result.IsSuccess)
            {
                MessageBox.Show($"An error occured while attempting to create a branch - {new_branch_result.ErrorMessage}");
                return false;
            }
            else
            {
                // Persisting a replay will kick the user out of replay mode
                ReplayMode.Disable();

                // Persist new branch settings
                DB.Open();
                SqliteCommand statement = DB.Query("INSERT INTO settings (game, branch, auto_commit, prefix, suffix, turn, sq_turn, compound_turn, replay_mode) VALUES (@game, @branch, @auto_commit, @prefix, @suffix, @turn, @sq_turn, @compound_turn, @replay_mode) ON CONFLICT(game, branch) DO UPDATE SET auto_commit = @auto_commit, prefix = @prefix, suffix = @suffix, turn = @turn, sq_turn = @sq_turn, compound_turn = @compound_turn, replay_mode = @replay_mode");
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.Parameters.Add("@auto_commit", SqliteType.Text).Value = Game.Settings.Auto_commit;
                statement.Parameters.Add("@prefix", SqliteType.Text).Value = Game.Settings.Prefix;
                statement.Parameters.Add("@suffix", SqliteType.Text).Value = Game.Settings.Suffix;
                statement.Parameters.Add("@turn", SqliteType.Integer).Value = Game.Settings.Turn;
                statement.Parameters.Add("@sq_turn", SqliteType.Text).Value = Game.Settings.SQ_Turn;
                statement.Parameters.Add("@compound_turn", SqliteType.Text).Value = Game.Settings.Compound_Turn;
                statement.Parameters.Add("@replay_mode", SqliteType.Text).Value = false;
                statement.ExecuteNonQuery();
                DB.Close();

                // TODO Create a timeline for this new branch

                // Refresh timeline UI
                Timeline.Refresh_Timeline_Nodes();

                // Refresh timeline TopPanel
                //Timeline.Initialize_Timeline_Root();

                // Enable Replay Mode Node OR  disable Manual snapshot Node if needed
                Timeline.Manual_Snapshot_Node();
                Timeline.Replay_Mode_Node();

                return true;
            }

        }

        public static void Exit()
        {

            // Discard all changes made while Replay mode was active - Go back to previous branch
            // git checkout <previous-branch> --force

            // Call ReplayMode.Disable
            ReplayMode.Disable();

            // TODO retrieve proper branch_name and use checkout --force
            SwitchBranch("root");

        }

    }
}
