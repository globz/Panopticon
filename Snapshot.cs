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
        RepositoryStatus status = Git.Status();
        if (!status.Modified.Any())
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

    static public bool Create()
    {
        RepositoryStatus status = Git.Status();
        if (status.Modified.Any())
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

                // Save current commit information to timeline table
                DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

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

                // Save current commit information to timeline table
                DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

                // Added default timeline notes for Saves
                DB.SaveTimelineNotes($"Save on turn {Game.Settings.Turn}", Git.Commit_title(maybe_new_turn));

                // Refresh Timeline nodes
                Timeline.Refresh_Timeline_Nodes();

                // Refresh Snapshot UI
                InitializeDefaultComponent();
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    static void NewSnapshotButton_Click(object? sender, EventArgs e)
    {
        if (!Create())
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

        Button ExitButton = new()
        {
            Location = new System.Drawing.Point(10, 20),
            Text = "Exit",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_snapshot.Controls.Add(ExitButton);
        groupBox_snapshot.Location = new System.Drawing.Point(10, 5);
        groupBox_snapshot.Size = new System.Drawing.Size(220, 115);
        groupBox_snapshot.Text = "Replay Mode";
        groupBox_snapshot.ForeColor = Color.Orange;

        Label description = new()
        {
            Text = "You are currently in replay mode."
            + System.Environment.NewLine
            + System.Environment.NewLine,
            Dock = DockStyle.Fill
        };

        // Figure out the current status of our working directory
        RepositoryStatus status = Git.Status();
        if (!status.Modified.Any())
        {
            description.Text += System.Environment.NewLine
            + System.Environment.NewLine
            + "Current status: No pending changes.";

            // Check if user has already commited changes to the replay session
            // If true enable [Persist] option
            if (Git.HasUnreferencedCommits())
            {
                var textBox_branch_name = new TextBox
                {
                    TabIndex = 1,
                    ForeColor = Game.UI.ForeColor,
                    MaxLength = 50,
                    Dock = DockStyle.Left
                };

                ErrorProvider branchName_errorProvider = new ErrorProvider
                {
                    BlinkStyle = ErrorBlinkStyle.BlinkIfDifferentError // Optional: Blink when error changes
                };

                Button PersistButton = new()
                {
                    Location = new System.Drawing.Point(5, 150),
                    Size = new System.Drawing.Size(100, 25),
                    Text = "Persist",
                    BackColor = Color.Purple,
                    ForeColor = Game.UI.ForeColor,
                    Padding = new(2),
                    TabIndex = 2
                };

                var groupBox_branch_name_textbox = new System.Windows.Forms.GroupBox();
                groupBox_branch_name_textbox.Location = new System.Drawing.Point(5, 100);
                groupBox_branch_name_textbox.Size = new System.Drawing.Size(130, 50);
                groupBox_branch_name_textbox.Text = "branch name";
                groupBox_branch_name_textbox.ForeColor = Color.Orange;
                groupBox_branch_name_textbox.Controls.Add(textBox_branch_name);

                Game.UI.BottomPanel?.Controls.Add(PersistButton);
                Game.UI.BottomPanel?.Controls.Add(groupBox_branch_name_textbox);

                textBox_branch_name.TextChanged += (sender, e) => Game.UI.TextBox_branch_name_TextChanged(textBox_branch_name, branchName_errorProvider);
                textBox_branch_name.Validating += (sender, e) => Game.UI.TextBox_branch_name_Validating(sender, e, textBox_branch_name);

                PersistButton.Click += (sender, e) => { PersistButon_Click(textBox_branch_name.Text); };
            }

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

            groupBox_modified_files.Location = new System.Drawing.Point(5, 200);
            groupBox_modified_files.Size = new System.Drawing.Size(220, 100);
            groupBox_modified_files.Text = "Modified Files";
            groupBox_modified_files.ForeColor = Color.Orange;

            Label replay_description = new()
            {
                Text = "Selecting [Persist] will save your replay session to a new branch and exit replay mode."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "Selecting [Discard], will discard the latest change made during your replay session."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "Selecting [Continue], will protect your latest turn from being removed when using [Discard]"
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "Selecting [Exit], will exit replay mode discarding all changes and bring you back to your previous branch.",
                Dock = DockStyle.Fill
            };

            var groupBox_replay_description = new System.Windows.Forms.GroupBox();
            groupBox_replay_description.Location = new System.Drawing.Point(5, 200);
            groupBox_replay_description.Size = new System.Drawing.Size(520, 130);
            groupBox_replay_description.Text = "Replay commands";
            groupBox_replay_description.ForeColor = Color.Orange;
            groupBox_replay_description.Controls.Add(replay_description);
            groupBox_replay_description.Dock = DockStyle.Bottom;

            Label modified_files = new()
            {
                Dock = DockStyle.Fill
            };

            status.Modified.ToList().ForEach(status => modified_files.Text += $"{status.FilePath + System.Environment.NewLine}");

            Button DiscardButton = new()
            {
                Location = new System.Drawing.Point(50, 20),
                Text = "Discard",
                BackColor = Color.OrangeRed,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            Button ContinueButton = new()
            {
                Location = new System.Drawing.Point(110, 20),
                Text = "Continue",
                BackColor = Color.LimeGreen,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var textBox_branch_name = new TextBox
            {
                TabIndex = 1,
                ForeColor = Game.UI.ForeColor,
                MaxLength = 50,
                Dock = DockStyle.Left
            };

            ErrorProvider branchName_errorProvider = new ErrorProvider
            {
                BlinkStyle = ErrorBlinkStyle.BlinkIfDifferentError // Optional: Blink when error changes
            };

            Button PersistButton = new()
            {
                Location = new System.Drawing.Point(5, 150),
                Size = new System.Drawing.Size(100, 25),
                Text = "Persist",
                BackColor = Color.Purple,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                TabIndex = 2
            };

            var groupBox_branch_name_textbox = new System.Windows.Forms.GroupBox();
            groupBox_branch_name_textbox.Location = new System.Drawing.Point(5, 100);
            groupBox_branch_name_textbox.Size = new System.Drawing.Size(130, 50);
            groupBox_branch_name_textbox.Text = "branch name";
            groupBox_branch_name_textbox.ForeColor = Color.Orange;
            groupBox_branch_name_textbox.Controls.Add(textBox_branch_name);

            Game.UI.BottomPanel?.Controls.Add(PersistButton);
            Game.UI.BottomPanel?.Controls.Add(groupBox_branch_name_textbox);

            textBox_branch_name.TextChanged += (sender, e) => Game.UI.TextBox_branch_name_TextChanged(textBox_branch_name, branchName_errorProvider);
            textBox_branch_name.Validating += (sender, e) => Game.UI.TextBox_branch_name_Validating(sender, e, textBox_branch_name);

            groupBox_snapshot.Controls.Add(DiscardButton);
            groupBox_snapshot.Controls.Add(ContinueButton);

            groupBox_modified_files.Controls.Add(modified_files);
            Game.UI.BottomPanel?.Controls.Add(groupBox_modified_files);
            Game.UI.BottomPanel?.Controls.Add(groupBox_replay_description);

            PersistButton.Click += (sender, e) => { PersistButon_Click(textBox_branch_name.Text); };

            ContinueButton.Click += (sender, e) => { TimeTravel.ReplayMode.Continue(); };

            DiscardButton.Click += (sender, e) =>
            {
                var confirmDiscard = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before discarding your turn.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and discard your latest turn?",
                $"Confirm discardation",
                MessageBoxButtons.YesNo);

                if (confirmDiscard == DialogResult.Yes)
                {

                    TimeTravel.ReplayMode.Discard();
                }
                else
                {
                    return;
                }

            };
        }

        Game.UI.TopPanel?.Controls.Add(groupBox_snapshot);
        Game.UI.BottomPanel?.Controls.Add(description);

        ExitButton.Click += (sender, e) =>
        {
            var confirmPersistence = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before persisting your replay session.{System.Environment.NewLine + System.Environment.NewLine} Do you want to exit replay mode now?",
            $"Exit confirmation", MessageBoxButtons.YesNo);
            if (confirmPersistence == DialogResult.Yes)
            {

                TimeTravel.ReplayMode.Exit();
            }
            else if (confirmPersistence == DialogResult.No)
            {
                return;
            }
        };

    }

    private static void PersistButon_Click(string? branch_name)
    {
        var confirmPersistence = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before persisting your replay session.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and persist your replay session?",
        $"Confirm replay session persistence",
        MessageBoxButtons.YesNo);
        if (confirmPersistence == DialogResult.Yes && !string.IsNullOrWhiteSpace(branch_name))
        {

            TimeTravel.ReplayMode.Persist(branch_name);
        }
        else if (confirmPersistence == DialogResult.Yes && string.IsNullOrWhiteSpace(branch_name))
        {
            MessageBox.Show("A branch name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (confirmPersistence == DialogResult.No)
        {
            return;
        }
    }

}