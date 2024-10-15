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
            }
        }
        return timeline_nodes_name;
    }
}
