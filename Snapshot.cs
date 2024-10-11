using System.CodeDom;
using System.Reflection;
using Accessibility;
using LibGit2Sharp;

namespace Panopticon;
public partial class Snapshot : Form
{
    public static void InitializeComponent()
    {
        var groupBox_snapshot = new System.Windows.Forms.GroupBox();
        var groupBox_modified_files = new System.Windows.Forms.GroupBox();

        Game.UI.TopPanel?.Controls.Clear();
        Game.UI.BottomPanel?.Controls.Clear();

        Button NewSnapshotButton = new()
        {
            Location = new System.Drawing.Point(20, 20),
            Text = "Create Snapshot",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_snapshot.Controls.Add(NewSnapshotButton);
        groupBox_snapshot.Location = new System.Drawing.Point(10, 5);
        groupBox_snapshot.Size = new System.Drawing.Size(220, 115);
        groupBox_snapshot.Text = "New Timeline node";
        groupBox_snapshot.ForeColor = Color.Orange;

        Label description = new()
        {
            Text = "You are currently using manual Timeline node creation."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Simply click <Create Snapshot> button and a new node will be created if a change has been detected.",
            Dock = DockStyle.Fill
        };

        // Figure out the current status of our working directory
        var status = Git.Status();
        if (status == null)
        {
            description.Text += System.Environment.NewLine
            + System.Environment.NewLine
            + "Current status: No pending changes.";
        }
        else
        {
            // Check if a turn has been made
            bool maybe_new_turn = Git.CheckIfFileExists(status.Modified, ".trn");

            string status_description = maybe_new_turn ? "New turn has been detected" : "Save && Quit has been detected";

            description.Text += System.Environment.NewLine
            + System.Environment.NewLine
            + $"Current status: {status_description}!"
            + System.Environment.NewLine;

            groupBox_modified_files.Location = new System.Drawing.Point(5, 100);
            groupBox_modified_files.Size = new System.Drawing.Size(220, 100);
            groupBox_modified_files.Text = "Modified Files";
            groupBox_modified_files.ForeColor = Color.Orange;

            Label modified_files = new()
            {
                Dock = DockStyle.Fill
            };

            status.Modified.ToList().ForEach(status => modified_files.Text += $"{status.FilePath + System.Environment.NewLine}");

            groupBox_modified_files.Controls.Add(modified_files);
            Game.UI.BottomPanel?.Controls.Add(groupBox_modified_files);
        }

        Game.UI.TopPanel?.Controls.Add(groupBox_snapshot);
        Game.UI.BottomPanel?.Controls.Add(description);

        NewSnapshotButton.Click += new EventHandler(NewSnapshotButton_Click);
    }

    static void NewSnapshotButton_Click(object? sender, EventArgs e)
    {
        var status = Git.Status();
        if (status != null)
        {
            // Check if a turn has been made
            bool maybe_new_turn = Git.CheckIfFileExists(status.Modified, ".trn");

            if (maybe_new_turn)
            {
                // New turn detected

                // Commit all changes
                Git.Commit(Game.Path, Git.Commit_title(maybe_new_turn));

                // Save current commit information to timelines DB
                DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

                // Save settings (Turn have been updated & commited)
                DB.SaveAllSettings();

                // Refresh Timeline nodes
                Timeline.Refresh_Timeline_Nodes();

                // Refresh Snapshot UI
                InitializeComponent();

            }
            else
            {
                // Save & Quit detected

                // var commit_title = Game.Settings.Prefix + Game.Name + "_SQ_" + Game.Settings.Compound_Turn.ToString("0.00");

                // Commit all changes
                Git.Commit(Game.Path, Git.Commit_title(maybe_new_turn));

                // Save current commit information to timelines DB
                DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

                // Save settings (Turn(s) have been updated & commited)
                DB.SaveAllSettings();

                // Added default timeline notes for S&Q
                DB.SaveTimelineNotes($"Save & Quit on turn {Game.Settings.Turn}", Git.Commit_title(maybe_new_turn));

                // Refresh Timeline nodes
                Timeline.Refresh_Timeline_Nodes();

                // Refresh Snapshot UI
                InitializeComponent();

            }

        }
        else
        {
            MessageBox.Show("There are no pending changes!");
        }

    }
}