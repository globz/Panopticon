using System.CodeDom;
using System.Reflection;
using Accessibility;
using LibGit2Sharp;
using static Panopticon.Settings;

namespace Panopticon;
public class Snapshot
{
    public static void InitializeDefaultComponent()
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

            string status_description = maybe_new_turn ? "New turn has been detected" : "A save has been detected";

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

            // Did FileWatcherManager missed a turn?
            if (TurnTracker.Maybe_missed_turn())
            {
                // Update turn | sq_turn | compound_turn
                TurnTracker.Update_Turn(maybe_new_turn);
                Console.WriteLine("TurnTracker missed a turn!");
            }

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
                InitializeDefaultComponent();
            }
            else
            {
                // Save & Quit detected

                // Commit all changes
                Git.Commit(Game.Path, Git.Commit_title(maybe_new_turn));

                // Save current commit information to timelines DB
                DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

                // Save settings (Turn(s) have been updated & commited)
                DB.SaveAllSettings();

                // Added default timeline notes for Saves
                DB.SaveTimelineNotes($"Save on turn {Game.Settings.Turn}", Git.Commit_title(maybe_new_turn));

                // Refresh Timeline nodes
                Timeline.Refresh_Timeline_Nodes();

                // Refresh Snapshot UI
                InitializeDefaultComponent();
            }
        }
        else
        {
            MessageBox.Show("There are no pending changes!");
        }

    }

    public static void InitializeReplayComponent()
    {
        var groupBox_snapshot = new System.Windows.Forms.GroupBox();
        var groupBox_modified_files = new System.Windows.Forms.GroupBox();

        Game.UI.TopPanel?.Controls.Clear();
        Game.UI.BottomPanel?.Controls.Clear();

        Button DiscardButton = new()
        {
            Location = new System.Drawing.Point(10, 20),
            Text = "Discard",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button ContinueButton = new()
        {
            Location = new System.Drawing.Point(80, 20),
            Text = "Continue",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_snapshot.Controls.Add(DiscardButton);
        groupBox_snapshot.Controls.Add(ContinueButton);
        groupBox_snapshot.Location = new System.Drawing.Point(10, 5);
        groupBox_snapshot.Size = new System.Drawing.Size(220, 115);
        groupBox_snapshot.Text = "Replay Mode";
        groupBox_snapshot.ForeColor = Color.Orange;

        Label description = new()
        {
            Text = "You are currently in replay mode."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You are currently using manual Timeline node creation. (forced)"
            + System.Environment.NewLine
            + System.Environment.NewLine,
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

            string status_description = maybe_new_turn ? "New turn has been detected" : "A save has been detected";

            description.Text += System.Environment.NewLine
            + System.Environment.NewLine
            + $"Current status: {status_description}!"
            + System.Environment.NewLine;

            groupBox_modified_files.Location = new System.Drawing.Point(5, 250);
            groupBox_modified_files.Size = new System.Drawing.Size(220, 100);
            groupBox_modified_files.Text = "Modified Files";
            groupBox_modified_files.ForeColor = Color.Orange;

            Label modified_files = new()
            {
                Dock = DockStyle.Fill
            };

            status.Modified.ToList().ForEach(status => modified_files.Text += $"{status.FilePath + System.Environment.NewLine}");

            Button PersistButton = new()
            {
                Location = new System.Drawing.Point(5, 175),
                Size = new System.Drawing.Size(100, 25),
                Text = "Persist",
                BackColor = Color.Purple,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                TabIndex = 2
            };

            var textBoxField_branch_name = new TextBoxField("Branch name:")
            {
                TabIndex = 1,
                Location = new System.Drawing.Point(5, 145),
                ForeColor = Game.UI.ForeColor,
                MaxLength = 50
            };


            Game.UI.BottomPanel?.Controls.Add(PersistButton);
            Game.UI.BottomPanel?.Controls.Add(textBoxField_branch_name);

            groupBox_modified_files.Controls.Add(modified_files);
            Game.UI.BottomPanel?.Controls.Add(groupBox_modified_files);

            PersistButton.Click += (sender, e) => { TimeTravel.ReplayMode.Persist(); };
        }

        Game.UI.TopPanel?.Controls.Add(groupBox_snapshot);
        Game.UI.BottomPanel?.Controls.Add(description);

        DiscardButton.Click += (sender, e) => { TimeTravel.ReplayMode.Discard(); };
        ContinueButton.Click += (sender, e) => { TimeTravel.ReplayMode.Continue(); };

    }
}