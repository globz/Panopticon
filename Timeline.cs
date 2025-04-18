using System.ComponentModel;
using System.Net;
using System.Reflection.Metadata;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LibGit2Sharp;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static Panopticon.Settings;

namespace Panopticon;

public partial class Timeline : Form
{
    public FileWatcherManager? _fileWatcher;

    public Timeline()
    {
        InitializeComponent();

        // Add .gitignore to Game.Path (ignoring panopticon.db)
        Git.CreateGitIgnore(Game.Path);

        // Initialize the settings database
        Initialize_Settings_DB();

        // Retrieve saved settings
        Retrieve_Settings();

        // Refresh Timeline nodes
        Refresh_Timeline_Nodes();

        // Initialize FileWatcherManager
        this.Load += (s, e) =>
        {
            _fileWatcher = new FileWatcherManager(@$"{Game.Path}");
            _fileWatcher.Start();
        };

        // Clean up when form closes
        this.FormClosing += (s, e) =>
        {
            _fileWatcher?.Dispose();
            DB.Cleanup();
        };

        // Enable manual snapshot node if needed
        Manual_Snapshot_Node();

        // Enable replay mode node if needed
        Replay_Mode_Node();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S))
        {
            if (Game.UI.SelectedNode != null && Git.Exist(Game.Path))
            {
                if (Game.UI.SelectedNode.Name != "settings")
                {
                    Button? saveNotesButton = Game.UI.FindControlByName(Game.UI.TopPanel, "saveNotes", typeof(Button));
                    saveNotesButton?.PerformClick();
                }
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void InitializeComponent()
    {
        var verticalSplitContainer = new System.Windows.Forms.SplitContainer();
        var treeViewLeft = new System.Windows.Forms.TreeView();
        var horizontalSplitContainer = new System.Windows.Forms.SplitContainer();
        var topPanel = new System.Windows.Forms.Panel();
        var bottomPanel = new System.Windows.Forms.Panel();

        verticalSplitContainer.SuspendLayout();
        horizontalSplitContainer.SuspendLayout();
        SuspendLayout();

        treeViewLeft.BackColor = Game.UI.Theme;
        treeViewLeft.ForeColor = Color.GhostWhite;
        topPanel.BackColor = Game.UI.Theme;
        topPanel.ForeColor = Game.UI.ForeColor;
        bottomPanel.BackColor = Game.UI.Theme;
        bottomPanel.ForeColor = Game.UI.ForeColor;

        // Basic SplitContainer properties.
        // This is a vertical splitter that moves in 10-pixel increments.
        // This splitter needs no explicit Orientation property because Vertical is the default.
        verticalSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        //verticalSplitContainer.ForeColor = System.Drawing.SystemColors.Control;
        verticalSplitContainer.Location = new System.Drawing.Point(0, 0);
        verticalSplitContainer.Name = "verticalSplitContainer";
        // You can drag the splitter no nearer than 30 pixels from the left edge of the container.
        verticalSplitContainer.Panel1MinSize = 30;
        // You can drag the splitter no nearer than 20 pixels from the right edge of the container.
        verticalSplitContainer.Panel2MinSize = 20;
        verticalSplitContainer.Size = new System.Drawing.Size(292, 273);
        verticalSplitContainer.SplitterDistance = 79;
        // This splitter moves in 10-pixel increments.
        verticalSplitContainer.SplitterIncrement = 10;
        verticalSplitContainer.SplitterWidth = 6;
        // verticalSplitContainer is the first control in the tab order.
        verticalSplitContainer.TabIndex = 0;
        verticalSplitContainer.Text = "verticalSplitContainer";
        // When the splitter moves, the cursor changes shape.
        verticalSplitContainer.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(VerticalSplitContainer_SplitterMoved);
        verticalSplitContainer.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(VerticalSplitContainer_SplitterMoving);

        // Add a TreeView control to Panel1.
        verticalSplitContainer.Panel1.Controls.Add(treeViewLeft);
        verticalSplitContainer.Panel1.Name = "splitterPanel1";
        // Controls placed on Panel1 support right-to-left fonts.
        verticalSplitContainer.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;

        // Populate TreeViewLeft with available use actions
        TreeNode settingsNode = new("Settings");
        settingsNode.Name = "settings";
        treeViewLeft.Nodes.Add(settingsNode);

        TreeNode timelineNode = new("Timeline - " + Game.Name);
        timelineNode.Name = "timeline_root";
        treeViewLeft.Nodes.Add(timelineNode);
        treeViewLeft.ExpandAll();

        // Add a SplitContainer to the right panel.
        verticalSplitContainer.Panel2.Controls.Add(horizontalSplitContainer);
        verticalSplitContainer.Panel2.Name = "splitterPanel2";

        // This TreeView control is in Panel1 of verticalSplitContainer.
        treeViewLeft.Dock = System.Windows.Forms.DockStyle.Fill;
        //treeViewLeft.ForeColor = System.Drawing.SystemColors.InfoText;
        treeViewLeft.ImageIndex = -1;
        treeViewLeft.Location = new System.Drawing.Point(0, 0);
        treeViewLeft.Name = "treeViewLeft";
        treeViewLeft.SelectedImageIndex = -1;
        treeViewLeft.Size = new System.Drawing.Size(79, 273);
        // treeViewLeft is the second control in the tab order.
        treeViewLeft.TabIndex = 1;

        // Basic SplitContainer properties.
        // This is a horizontal splitter whose top and bottom panels are ListView controls. The top panel is fixed.
        horizontalSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        // The top panel remains the same size when the form is resized.
        horizontalSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        horizontalSplitContainer.Location = new System.Drawing.Point(0, 0);
        horizontalSplitContainer.Name = "horizontalSplitContainer";
        // Create the horizontal splitter.
        horizontalSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
        horizontalSplitContainer.Size = new System.Drawing.Size(207, 273);
        horizontalSplitContainer.SplitterDistance = 125;
        horizontalSplitContainer.SplitterWidth = 6;
        // horizontalSplitContainer is the third control in the tab order.
        horizontalSplitContainer.TabIndex = 2;
        horizontalSplitContainer.Text = "horizontalSplitContainer";

        // This splitter panel contains the top Panel control.
        horizontalSplitContainer.Panel1.Controls.Add(topPanel);
        horizontalSplitContainer.Panel1.Name = "splitterPanel3";

        // This splitter panel contains the bottom Panel control.
        horizontalSplitContainer.Panel2.Controls.Add(bottomPanel);
        horizontalSplitContainer.Panel2.Name = "splitterPanel4";

        // This ListView control is in the top panel of horizontalSplitContainer.
        topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        topPanel.Location = new System.Drawing.Point(0, 0);
        topPanel.Name = "topPanel";
        topPanel.Size = new System.Drawing.Size(207, 125);
        // topPanel is the fourth control in the tab order.
        topPanel.TabIndex = 3;

        // This ListView control is in the bottom panel of horizontalSplitContainer.
        bottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        bottomPanel.Location = new System.Drawing.Point(0, 0);
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Size = new System.Drawing.Size(207, 142);
        // bottomPanel is the fifth control in the tab order.
        bottomPanel.TabIndex = 4;
        bottomPanel.AutoScroll = true;

        // These are basic properties of the form.
        this.ClientSize = new System.Drawing.Size(800, 600);
        this.Text = "Timeline - " + Game.Name;
        Controls.Add(verticalSplitContainer);

        // Set UI static reference
        Game.UI.VerticalSplitContainer = verticalSplitContainer;
        Game.UI.HorizontalSplitContainer = horizontalSplitContainer;
        Game.UI.TreeViewLeft = treeViewLeft;
        Game.UI.TopPanel = topPanel;
        Game.UI.BottomPanel = bottomPanel;
        Game.UI.Timeline_settings = settingsNode;
        Game.UI.Timeline_history = timelineNode;

        treeViewLeft.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeViewLeft_AfterSelect);

        verticalSplitContainer.ResumeLayout(false);
        horizontalSplitContainer.ResumeLayout(false);
        ResumeLayout(false);
    }

    private void VerticalSplitContainer_SplitterMoving(System.Object? sender, System.Windows.Forms.SplitterCancelEventArgs e)
    {
        // As the splitter moves, change the cursor type.
        Cursor.Current = System.Windows.Forms.Cursors.NoMoveVert;
    }

    private void VerticalSplitContainer_SplitterMoved(System.Object? sender, System.Windows.Forms.SplitterEventArgs e)
    {
        // When the splitter stops moving, change the cursor back to the default.
        Cursor.Current = System.Windows.Forms.Cursors.Default;
    }

    protected static bool TreeViewLeft_Node_Selection_Behaviour(TreeViewAction action, TreeNode current_SelectedNode, TreeNode? previous_SelectedNode)
    {
        Console.WriteLine("Game.UI.TreeViewLeft.SelectedNode: " + Game.UI.TreeViewLeft.SelectedNode.Name);
        Console.WriteLine("Game.UI.SelectedNode: " + Game.UI.SelectedNode?.Name);

        if (action != TreeViewAction.Unknown) { return false; }
        if (current_SelectedNode == previous_SelectedNode) { return false; }
        if (previous_SelectedNode == null) { return false; }
        return true;
    }

    protected void TreeViewLeft_AfterSelect(object? sender, System.Windows.Forms.TreeViewEventArgs e)
    {
        // @ HACK to cancel default selection behaviour when window is losing focus or while navigating without user action.
        // This action is always "Unknown" and does not match "ByMouse" or "ByKeyboard".
        // We return early so we do not update the selectedNode from "TreeViewAction.Unknown" 
        // See implementation details of the function call, whenever it return TRUE the logic below will be skipped entirely.
        bool maybe_skip_logic_below = TreeViewLeft_Node_Selection_Behaviour(e.Action, Game.UI.TreeViewLeft.SelectedNode, Game.UI.SelectedNode);
        Console.WriteLine(maybe_skip_logic_below);
        if (maybe_skip_logic_below) { Game.UI.TreeViewLeft.SelectedNode = null; return; }

        // Reset ForeColor of previous selected node
        if (Game.UI.SelectedNode != null)
        {
            Game.UI.SelectedNode.ForeColor = Color.GhostWhite;
        }

        // Update reference to the current selected node
        Game.UI.SelectedNode = e.Node;

        // Update ForeColor of current selected node
        if (Game.UI.SelectedNode != null)
        {
            Game.UI.SelectedNode.ForeColor = Color.PaleGreen;
            Game.UI.SelectedNode.BackColor = Color.Empty;
        }

        // Reset SelectedNode so we can retrigger this event if the user select the same node again
        // @ HACK - When losing window focus, the default targetted node is settings (THIS SUCKS!)
        Game.UI.TreeViewLeft.SelectedNode = null;

        // Dispatch based on selected node name
        switch (e.Node?.Name)
        {
            case "settings":
                Settings.InitializeComponent();
                break;
            case "timeline_root":
                Initialize_Timeline_Root();
                break;
            case "new_snapshot":
                Snapshot.InitializeDefaultComponent();
                break;
            case "replay_mode":
                Snapshot.InitializeReplayComponent();
                break;
            default:
                Initialize_Timeline_Node(e.Node?.Name);
                break;
        }
    }

    public static void Initialize_Timeline_Root()
    {
        var groupBox_timeline_root = new System.Windows.Forms.GroupBox();

        // Validate if a Timeline has already been created (via git)
        if (Git.Exist(Game.Path))
        {
            Game.UI.TopPanel?.Controls.Clear();
            Game.UI.BottomPanel?.Controls.Clear();

            Label description = new()
            {
                Text = $"This is the beginning of Timeline - {Game.Name}."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + $"This Timeline is part of the following branch : [{Git.CurrentBranch()}]"
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "You may do the following actions:"
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "Add a description about your Timeline/game."
                + System.Environment.NewLine
                + "Switch to an alternate Timeline branch."
                + System.Environment.NewLine
                + "Permenantly delete your Timeline.",
                Dock = DockStyle.Fill
            };


            if (Git.CurrentBranch() == "root")
            {
                Button DeleteTimelineButton = new()
                {
                    Location = new System.Drawing.Point(40, 20),
                    Text = "Delete Timeline",
                    BackColor = Color.LightSteelBlue,
                    ForeColor = Game.UI.ForeColor,
                    Padding = new(2),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                groupBox_timeline_root.Controls.Add(DeleteTimelineButton);
                DeleteTimelineButton.Click += new EventHandler(DeleteTimelineButton_Click);
            }
            else
            {
                Button DeleteBranchButton = new()
                {
                    Location = new System.Drawing.Point(40, 20),
                    Text = "Delete Branch",
                    BackColor = Color.LightSteelBlue,
                    ForeColor = Game.UI.ForeColor,
                    Padding = new(2),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                groupBox_timeline_root.Controls.Add(DeleteBranchButton);
                DeleteBranchButton.Click += new EventHandler(DeleteBranchButton_Click);
            }

            Button SwitchTimelineBranchButton = new()
            {
                Location = new System.Drawing.Point(40, 53),
                Text = "Switch Branch",
                BackColor = Color.LightSteelBlue,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            Button SaveDescriptionButton = new()
            {
                Location = new System.Drawing.Point(40, 86),
                Text = "Save Description",
                BackColor = Color.LightSteelBlue,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Name = "saveNotes"
            };

            groupBox_timeline_root.Controls.Add(SaveDescriptionButton);
            groupBox_timeline_root.Location = new System.Drawing.Point(10, 5);
            groupBox_timeline_root.Size = new System.Drawing.Size(220, 125);
            groupBox_timeline_root.Text = "Timeline - " + Game.Name;
            groupBox_timeline_root.ForeColor = Color.Orange;
            groupBox_timeline_root.Name = "groupBox_timeline_root";

            TextBox DescriptionBox = new()
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                Dock = DockStyle.Bottom,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new System.Drawing.Size(100, 250),
                BackColor = Color.LightYellow,
                Name = "Description"
            };

            // Retrieve DescriptionBox Text
            DescriptionBox.Text = Retrieve_Timeline_Notes();

            groupBox_timeline_root.Controls.Add(SwitchTimelineBranchButton);
            Game.UI.TopPanel?.Controls.Add(groupBox_timeline_root);
            Game.UI.BottomPanel?.Controls.Add(DescriptionBox);
            Game.UI.BottomPanel?.Controls.Add(description);
            Game.UI.HorizontalSplitContainer.SplitterDistance = 150;

            SwitchTimelineBranchButton.Click += new EventHandler(SwitchTimelineBranchButton_Click);
            SaveDescriptionButton.Click += (sender, e) => SaveNotesButton_Click(DescriptionBox);
            DescriptionBox.TextChanged += (sender, e) => Notes_TextChanged(DescriptionBox);
        }
        else
        {

            Label description = new()
            {
                Text = "Ensure your settings are configured before creating your Timeline."
                + System.Environment.NewLine
                + System.Environment.NewLine,
                Dock = DockStyle.Fill
            };

            Button CreateTimelineButton = new()
            {
                Location = new System.Drawing.Point(40, 40),
                Text = "Create Timeline",
                BackColor = Color.LightSteelBlue,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            groupBox_timeline_root.Controls.Add(CreateTimelineButton);
            groupBox_timeline_root.Location = new System.Drawing.Point(10, 5);
            groupBox_timeline_root.Size = new System.Drawing.Size(220, 115);
            groupBox_timeline_root.Text = "Timeline - " + Game.Name;
            groupBox_timeline_root.ForeColor = Color.Orange;

            Game.UI.TopPanel?.Controls.Clear();
            Game.UI.TopPanel?.Controls.Add(groupBox_timeline_root);

            Game.UI.BottomPanel?.Controls.Clear();
            Game.UI.BottomPanel?.Controls.Add(description);

            CreateTimelineButton.Click += new EventHandler(CreateTimelineButton_Click);

        }
    }

    private static void Initialize_Timeline_Node(string? nodeName)
    {
        var groupBox_timeline_node = new System.Windows.Forms.GroupBox();

        Game.UI.TopPanel?.Controls.Clear();
        Game.UI.BottomPanel?.Controls.Clear();

        Label description = new()
        {
            Text = $"This node is part of your Timeline - {Game.Name}"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"This node is part of the following branch : [{Git.CurrentBranch()}]"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may do the following actions:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Add notes about this turn."
            + System.Environment.NewLine
            + "Execute time travel abilities.",
            Dock = DockStyle.Fill
        };

        Label description_replay_mode = new()
        {
            Text = $"This node is part of your Timeline - {Game.Name}"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may do the following action:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Add notes about this turn.",
            Dock = DockStyle.Fill
        };

        Button TimeTravelButton = new()
        {
            Location = new System.Drawing.Point(40, 20),
            Text = "Time Travel",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button SaveNotesButton = new()
        {
            Location = (!Game.Settings.Replay_Mode) ? new System.Drawing.Point(40, 53) : new System.Drawing.Point(40, 20),
            Text = "Save Notes",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Name = "saveNotes"
        };

        if (!Game.Settings.Replay_Mode) { groupBox_timeline_node.Controls.Add(TimeTravelButton); }
        groupBox_timeline_node.Controls.Add(SaveNotesButton);
        groupBox_timeline_node.Location = new System.Drawing.Point(10, 5);
        groupBox_timeline_node.Size = new System.Drawing.Size(220, 115);
        groupBox_timeline_node.Text = nodeName;
        groupBox_timeline_node.ForeColor = Color.Orange;
        groupBox_timeline_node.Name = "groupBox_timeline_node";

        TextBox NotesBox = new()
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            Dock = DockStyle.Bottom,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Size = new System.Drawing.Size(100, 250),
            BackColor = Color.LightYellow,
            Name = "notes"
        };

        // Retrieve NotesBox Text
        NotesBox.Text = Retrieve_Timeline_Notes();

        Game.UI.TopPanel?.Controls.Add(groupBox_timeline_node);
        Game.UI.BottomPanel?.Controls.Add(NotesBox);
        if (!Game.Settings.Replay_Mode)
        {
            Game.UI.BottomPanel?.Controls.Add(description);
        }
        else
        {
            Game.UI.BottomPanel?.Controls.Add(description_replay_mode);
        }
        if (!Game.Settings.Replay_Mode) { TimeTravelButton.Click += new EventHandler(TimeTravelButton_Click); }
        SaveNotesButton.Click += (sender, e) => SaveNotesButton_Click(NotesBox);
        NotesBox.TextChanged += (sender, e) => Notes_TextChanged(NotesBox);
    }

    static void CreateTimelineButton_Click(object? sender, EventArgs e)
    {

        // Initialize the timeline database
        Initialize_Timelines_DB();

        // Initialize the notes database
        Initialize_Notes_DB();

        // Save all game settings in case they were not configured & saved by the user
        DB.SaveAllSettings();

        // Init repo
        Git.Init(Game.Path);

        // Commit all changes
        Git.Commit(Game.Path, Git.Commit_title(true));

        // Rename master branch to root
        using var repo = new Repository(Game.Path);
        repo.Branches.Rename("master", "root");

        // Save current commit information to timeline DB
        DB.SaveTimeline(Git.Commit_title(true));

        // Refresh Timeline nodes
        Refresh_Timeline_Nodes();

        MessageBox.Show("Timeline created successfully!");

        // Reload the node
        Initialize_Timeline_Root();
    }

    static void DeleteTimelineButton_Click(object? sender, EventArgs e)
    {

        var confirmDeletion = MessageBox.Show("This change is irreversible; your Timeline will be permanently deleted. Do you want to proceed with the deletion?",
                                             "Timeline deletion confirmation",
                                             MessageBoxButtons.YesNo);
        if (confirmDeletion == DialogResult.Yes)
        {
            // Delete timeline settings
            using (var statement = DB.Query("DELETE FROM settings WHERE game = @game"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.ExecuteNonQuery();
            }


            // Delete timeline
            using (var statement = DB.Query("DELETE FROM timeline WHERE game = @game"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.ExecuteNonQuery();
            }

            // Delete notes
            using (var statement = DB.Query("DELETE FROM notes WHERE game = @game"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.ExecuteNonQuery();
            }

            // Delete git repo
            Git.Delete_Repo(Game.Path);

            // Delete child nodes from Timeline history
            Game.UI.Timeline_history?.Nodes.Clear();

            MessageBox.Show("Timeline has been deleted!");

            // Reload the node
            Initialize_Timeline_Root();
        }
        else
        {
            MessageBox.Show("Timeline deletion aborted!");
        }

    }

    static void DeleteBranchButton_Click(object? sender, EventArgs e)
    {

        var confirmDeletion = MessageBox.Show("This change is irreversible; your branch will be permanently deleted. Do you want to proceed with the deletion?",
                                             "Branch deletion confirmation",
                                             MessageBoxButtons.YesNo);
        if (confirmDeletion == DialogResult.Yes)
        {
            // Delete timeline branch settings
            using (var statement = DB.Query("DELETE FROM settings WHERE game = @game AND branch = @branch"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.ExecuteNonQuery();
            }

            // Delete timeline associated to this branch
            using (var statement = DB.Query("DELETE FROM timeline WHERE game = @game AND branch = @branch"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.ExecuteNonQuery();
            }

            // Delete notes related to this branch
            using (var statement = DB.Query("DELETE FROM notes WHERE game = @game AND branch = @branch"))
            {
                statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
                statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
                statement.ExecuteNonQuery();
            }

            // Retrieve current branch to delete
            string branch_to_delete = Git.CurrentBranch();

            // Switch back to Timeline root branch
            TimeTravel.SwitchBranch("root");

            // Delete git branch
            Git.Delete_Branch(Game.Path, branch_to_delete);

            MessageBox.Show("Timeline branch has been deleted!");

            // Reload the node
            Initialize_Timeline_Root();
        }
        else
        {
            MessageBox.Show("Timeline deletion aborted!");
        }

    }

    static void SwitchTimelineBranchButton_Click(object? sender, EventArgs e)
    {
        Game.UI.BottomPanel?.Controls.Clear();

        Button? node_to_delete = Game.UI.FindControlByName(Game.UI.TopPanel, "saveNotes", typeof(Button));
        if (node_to_delete != null)
        {
            node_to_delete.Hide();

        }

        var groupBox_overview = new System.Windows.Forms.GroupBox();
        groupBox_overview.Location = new System.Drawing.Point(10, 120);
        groupBox_overview.Text = $"Branches overview";
        groupBox_overview.ForeColor = Color.Orange;
        groupBox_overview.AutoSize = true;

        Label description = new()
        {
            Text = $"Please quit your game before proceeding."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "If you ever switch branch while your game is running, please select [Quit without saving] to exit your game."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Once your game is closed, you may proceed and switch branch."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"You care currently on branch : [{Git.CurrentBranch()}]",
            Dock = DockStyle.Fill
        };

        if (Git.Count_branches() == 1)
        {
            Label x = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(150, 50),
                Text = "There are currently no branch."
            };

            groupBox_overview.Controls.Add(x);
        }
        else
        {
            IEnumerable<string> branches = Git.List_branches();

            int yPosition = 35; // Starting Y position for the first button
            const int BUTTON_HEIGHT = 30; // Height of each button
            const int BUTTON_WIDTH = 150;  // Width of each button
            const int X_POSITION = 10;     // X position for all buttons

            branches.ToList().ForEach(branch =>
            {

                Button branchButton = new Button
                {
                    Text = branch,
                    Name = $"btn_{branch}",  // Unique name for each button
                    Location = new Point(X_POSITION, yPosition),
                    Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT),
                    // Optional: Add tooltip
                    UseVisualStyleBackColor = true,
                    BackColor = (branch == Git.CurrentBranch()) ? Color.DarkGreen : Color.IndianRed,
                    ForeColor = Game.UI.ForeColor,
                    Padding = new(2),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                // Add click event handler
                branchButton.Click += (sender, e) =>
                {
                    var confirmDeletion = MessageBox.Show($"Please exit your game before switching to another branch.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and switch branch?",
                                         $"Switch to branch {branch}",
                                         MessageBoxButtons.YesNo);
                    if (confirmDeletion == DialogResult.Yes)
                    {
                        TimeTravel.SwitchBranch(branch);
                    }
                    else
                    {
                        return;
                    }

                };

                // Add button to form's controls
                groupBox_overview.Controls.Add(branchButton);

                // Increment Y position for next button
                yPosition += BUTTON_HEIGHT + 5; // 5 pixels spacing between buttons
            });
        }

        AutoSizeGroupBox(groupBox_overview);
        Game.UI.BottomPanel?.Controls.Add(groupBox_overview);
        Game.UI.BottomPanel?.Controls.Add(description);

    }

    static void SaveNotesButton_Click(TextBox notes)
    {
        DB.SaveTimelineNotes(notes.Text);
        notes.BackColor = Color.LightYellow;
    }

    static void TimeTravelButton_Click(object? send, EventArgs e)
    {
        Game.UI.BottomPanel?.Controls.Clear();

        // Remove "Save Notes" Button - no longer valid in this context
        GroupBox? topPanel_groupBox = Game.UI.FindControlByName(Game.UI.TopPanel, "groupBox_timeline_node", typeof(GroupBox));
        Button? saveNotesButton = Game.UI.FindControlByName(topPanel_groupBox ?? new GroupBox(), "saveNotes", typeof(Button));
        topPanel_groupBox?.Controls.Remove(saveNotesButton);

        var groupBox_TimeTravelActions = new System.Windows.Forms.GroupBox();

        Label description = new()
        {
            Text = $"This node is part of your Timeline - {Game.Name}"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"This Timeline is part of the following branch : [{Git.CurrentBranch()}]"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may do the following actions:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Permanently undo this turn and subsequent turns."
            + System.Environment.NewLine
            + "Replay this turn."
            + System.Environment.NewLine
            + "Branch off to alternate Timeline."
            + System.Environment.NewLine
            + "Create a new game based on this snapshot.",
            Dock = DockStyle.Fill
        };

        Button UndoButton = new()
        {
            Location = new System.Drawing.Point(10, 20),
            Text = "Undo",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button ReplayButton = new()
        {
            Location = new System.Drawing.Point(100, 20),
            Text = "Replay",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button BranchOffButton = new()
        {
            Location = new System.Drawing.Point(10, 50),
            Text = "Branch off",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button NewGameButton = new()
        {
            Location = new System.Drawing.Point(100, 50),
            Text = "New game",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_TimeTravelActions.Location = new System.Drawing.Point(5, 175);
        groupBox_TimeTravelActions.Size = new System.Drawing.Size(200, 90);
        groupBox_TimeTravelActions.Text = "Actions";
        groupBox_TimeTravelActions.ForeColor = Color.Orange;

        groupBox_TimeTravelActions.Controls.Add(UndoButton);
        groupBox_TimeTravelActions.Controls.Add(ReplayButton);
        groupBox_TimeTravelActions.Controls.Add(BranchOffButton);
        groupBox_TimeTravelActions.Controls.Add(NewGameButton);

        Game.UI.BottomPanel?.Controls.Add(groupBox_TimeTravelActions);
        Game.UI.BottomPanel?.Controls.Add(description);

        UndoButton.Click += new EventHandler(UndoButton_Click);
        BranchOffButton.Click += new EventHandler(BranchOffButton_Click);
        ReplayButton.Click += new EventHandler(ReplayButton_Click);
    }

    static void UndoButton_Click(object? send, EventArgs e)
    {
        // Display UI and confirm user action
        Game.UI.BottomPanel?.Controls.Clear();

        // Gather information about the upcoming node(s) deletion
        List<string> undo_nodes_name = TimeTravel.Undo();

        int undo_node_count = undo_nodes_name.Count;

        Label description = new()
        {
            Text = $"Please quit your game before proceeding."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "If you ever undo this turn while your game is running, please select [Quit without saving] to exit your game."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Once your game is closed, you may proceed to permanently delete the following snapshot(s) from existence.",
            Dock = DockStyle.Fill
        };

        var groupBox_undo_log = new System.Windows.Forms.GroupBox();
        groupBox_undo_log.Location = new System.Drawing.Point(5, 130);
        groupBox_undo_log.Text = $"Undoing {undo_node_count} snapshot(s)";
        groupBox_undo_log.ForeColor = Color.Orange;
        groupBox_undo_log.AutoSize = true;

        Label undo_log = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill
        };

        if (undo_node_count == 0)
        {
            undo_log.Text = "This node is the initial snapshot of your branch, you cannot undo it.";
        }
        else
        {
            undo_nodes_name.ForEach(node_name => undo_log.Text += $"{node_name + System.Environment.NewLine}");

            Button ProceedUnDoButton = new()
            {
                Location = new System.Drawing.Point(5, 100),
                Text = "Proceed and delete.",
                BackColor = Color.IndianRed,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            Game.UI.BottomPanel?.Controls.Add(ProceedUnDoButton);
            ProceedUnDoButton.Click += (sender, e) =>
            {
                var confirmDeletion = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before undoing this turn.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and undo this turn?",
                $"Confirm deletion",
                MessageBoxButtons.YesNo);
                if (confirmDeletion == DialogResult.Yes)
                {

                    TimeTravel.Undo(true);
                }
                else
                {
                    return;
                }

            };
        }

        groupBox_undo_log.Controls.Add(undo_log);
        AutoSizeGroupBox(groupBox_undo_log);
        Game.UI.BottomPanel?.Controls.Add(groupBox_undo_log);
        Game.UI.BottomPanel?.Controls.Add(description);
    }

    private static void AutoSizeGroupBox(GroupBox groupBox)
    {
        int maxWidth = 0;
        int totalHeight = groupBox.Padding.Top;

        foreach (Control control in groupBox.Controls)
        {
            // Calculate the right edge of the control (X + Width)
            int controlRight = control.Left + control.Width + 30;
            maxWidth = Math.Max(maxWidth, controlRight);

            // Calculate the total height required
            totalHeight += control.Height + control.Margin.Bottom;
        }

        totalHeight += groupBox.Padding.Bottom;

        // Set the new size for the GroupBox
        groupBox.Width = maxWidth + groupBox.Padding.Right;
        groupBox.Height = totalHeight;
    }

    static void BranchOffButton_Click(object? send, EventArgs e)
    {
        // Display UI and confirm user action
        Game.UI.BottomPanel?.Controls.Clear();

        Label description = new()
        {
            Text = $"You are about to create a new branch based on the following snapshot:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"Turn: {Game.UI.SelectedNode?.Name} & branch: [{Git.CurrentBranch()}]"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Fill in the name of your new branch and proceed with the creation.",
            Dock = DockStyle.Fill
        };

        Label branch_description = new()
        {
            Text = "A branch is like an alternate storyline within a timeline."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Imagine you’re writing a novel, and the main story is the published book, the [root] branch."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "At some point, you decide to explore a different plot idea, but you don’t want to mess up the main storyline."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "So, you create an alternate story (a branch) based on the main timeline events."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "In this alternate story, you can make changes to the plot, develop characters differently, or experiment with new ideas."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "As you work, your changes stay within this alternate story, leaving the main storyline (root) untouched."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "In short, a branch allows you to explore different what-if scenarios in your game",
            Dock = DockStyle.Fill
        };

        var groupBox_branch_description = new System.Windows.Forms.GroupBox();
        groupBox_branch_description.Location = new System.Drawing.Point(5, 200);
        groupBox_branch_description.Size = new System.Drawing.Size(520, 250);
        groupBox_branch_description.Text = "What is a branch?";
        groupBox_branch_description.ForeColor = Color.Orange;
        groupBox_branch_description.Controls.Add(branch_description);
        groupBox_branch_description.Dock = DockStyle.Bottom;

        Button CreateBranchButton = new()
        {
            Location = new System.Drawing.Point(5, 150),
            Size = new System.Drawing.Size(100, 25),
            Text = "Create branch",
            BackColor = Color.Purple,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            TabIndex = 2
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

        var groupBox_branch_name_textbox = new System.Windows.Forms.GroupBox();
        groupBox_branch_name_textbox.Location = new System.Drawing.Point(5, 100);
        groupBox_branch_name_textbox.Size = new System.Drawing.Size(130, 50);
        groupBox_branch_name_textbox.Text = "branch name";
        groupBox_branch_name_textbox.ForeColor = Color.Orange;
        groupBox_branch_name_textbox.Controls.Add(textBox_branch_name);

        Game.UI.BottomPanel?.Controls.Add(CreateBranchButton);
        Game.UI.BottomPanel?.Controls.Add(groupBox_branch_name_textbox);
        Game.UI.BottomPanel?.Controls.Add(groupBox_branch_description);
        Game.UI.BottomPanel?.Controls.Add(description);

        textBox_branch_name.TextChanged += (sender, e) => Game.UI.TextBox_branch_name_TextChanged(textBox_branch_name, branchName_errorProvider);
        textBox_branch_name.Validating += (sender, e) => Game.UI.TextBox_branch_name_Validating(sender, e, textBox_branch_name);

        CreateBranchButton.Click += (sender, e) =>
        {
            var confirmBranchCreation = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before branching this turn.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and create a new branch?",
            $"Confirm branch creation",
            MessageBoxButtons.YesNo);
            if (confirmBranchCreation == DialogResult.Yes && !string.IsNullOrWhiteSpace(textBox_branch_name.Text.Trim()))
            {

                TimeTravel.BranchOff(textBox_branch_name.Text.Trim());
            }
            else if (confirmBranchCreation == DialogResult.No)
            {
                return;
            }
            else if (string.IsNullOrWhiteSpace(textBox_branch_name.Text.Trim()))
            {
                MessageBox.Show("A branch name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

    }
    static void ReplayButton_Click(object? send, EventArgs e)
    {
        // Display UI and confirm user action
        Game.UI.BottomPanel?.Controls.Clear();

        Label description = new()
        {
            Text = $"You are about to replay this specific point in time based on the following snapshot:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"Turn: {Game.UI.SelectedNode?.Name} & branch: [{Git.CurrentBranch()}]",
            Dock = DockStyle.Fill
        };

        Label replay_description = new()
        {
            Text = "This is a temporary state which may become permanent based on your approval of the outcome."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may initiate a replay session from any available snapshots based on your current timeline."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Once enabled, at the end of each new turn you will be asked to make the following choice:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "[Persist], [Discard], [Continue] or [Exit]"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may play as many turns as you wish however they will only persist if you decide to do so."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Selecting [Persist] will save your replay session to a new branch and exit replay mode."
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
        groupBox_replay_description.Size = new System.Drawing.Size(520, 280);
        groupBox_replay_description.Text = "How does replay work?";
        groupBox_replay_description.ForeColor = Color.Orange;
        groupBox_replay_description.Controls.Add(replay_description);
        groupBox_replay_description.Dock = DockStyle.Bottom;

        Button EnableReplayButton = new()
        {
            Location = new System.Drawing.Point(5, 90),
            Size = new System.Drawing.Size(100, 25),
            Text = "Enable replay",
            BackColor = Color.Purple,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            TabIndex = 2
        };

        Game.UI.BottomPanel?.Controls.Add(EnableReplayButton);
        Game.UI.BottomPanel?.Controls.Add(groupBox_replay_description);
        Game.UI.BottomPanel?.Controls.Add(description);
        EnableReplayButton.Click += (sender, e) =>
        {
            var confirmEnableReplay = MessageBox.Show($"Please exit your Dominion game [{Game.Name}] before starting a replay session.{System.Environment.NewLine + System.Environment.NewLine} Do you want to proceed and enable replay mode?",
            $"Confirm replay mode",
            MessageBoxButtons.YesNo);

            if (confirmEnableReplay == DialogResult.Yes)
            {

                TimeTravel.ReplayMode.Enable();
            }
            else
            {
                return;
            }

        };

    }

    private static void Notes_TextChanged(TextBox notes)
    {
        if (notes.BackColor != Color.PaleVioletRed)
        {
            notes.BackColor = Color.PaleVioletRed;
        }
    }

    private static void Initialize_Settings_DB()
    {
        using var statement = DB.Query("CREATE TABLE IF NOT EXISTS settings (game VARCHAR(23), branch VARCHAR(100), auto_commit BOOLEAN, prefix VARCHAR(10), suffix VARCHAR(10), turn INT, sq_turn DOUBLE, compound_turn DOUBLE, replay_mode BOOLEAN, PRIMARY KEY (game, branch))");
        statement.ExecuteNonQuery();
    }

    public static void Retrieve_Settings()
    {

        using var statement = DB.Query("SELECT auto_commit, prefix, suffix, turn, sq_turn, compound_turn, replay_mode FROM settings WHERE game = @game AND branch = @branch");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        DB.ReadData(statement, DB.LoadSettingsData);

    }

    private static void Initialize_Timelines_DB()
    {
        using var statement = DB.Query("CREATE TABLE IF NOT EXISTS timeline (game VARCHAR(23), branch VARCHAR(100), node_name VARCHAR(43), node_seq INT, compound_turn DOUBLE, commit_hash TEXT NOT NULL, PRIMARY KEY (game, branch, node_name))");
        statement.ExecuteNonQuery();
    }

    private static Dictionary<int, string> Retrieve_TimelineNodes()
    {
        var timeline_nodes = new Dictionary<int, string> { };

        using var statement = DB.Query("SELECT node_name, node_seq FROM timeline WHERE game = @game AND branch = @branch ORDER BY node_seq");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        SqliteDataReader timeline = statement.ExecuteReader();

        while (timeline.Read())
        {
            timeline_nodes.Add(Convert.ToInt32(timeline["node_seq"]), (string)timeline["node_name"]);
        }

        return timeline_nodes;
    }

    public static void Refresh_Timeline_Nodes()
    {
        if (Git.Exist(Game.Path))
        {
            Dictionary<int, string> timeline_nodes = Retrieve_TimelineNodes();
            Game.UI.Timeline_history?.Nodes.Clear();

            foreach (var dict in timeline_nodes)
            {
                TreeNode newTimelineNode = new(dict.Value);
                newTimelineNode.Name = dict.Value;
                Game.UI.Timeline_history?.Nodes.Add(newTimelineNode);
            }

            Game.UI.Timeline_history?.ExpandAll();

            // Scroll to the bottom
            if (Game.UI.Timeline_history?.Nodes.Count > 0)
            {
                Game.UI.Timeline_history.Nodes[Game.UI.Timeline_history.Nodes.Count - 1].EnsureVisible();
            }
        }
    }

    private static void Initialize_Notes_DB()
    {
        using var statement = DB.Query("CREATE TABLE IF NOT EXISTS notes (game VARCHAR(23), branch VARCHAR(100), node_name VARCHAR(43), notes TEXT, PRIMARY KEY (game, branch, node_name))");
        statement.ExecuteNonQuery();
    }

    private static string? Retrieve_Timeline_Notes()
    {

        using var statement = DB.Query("SELECT notes FROM notes WHERE game = @game AND branch = @branch AND node_name = @node_name");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
        var data = statement.ExecuteScalar();
        string? Notes = (data != null) ? data.ToString() : "Add a description.";
        return Notes;
    }

    public static void Manual_Snapshot_Node()
    {
        if (!Game.Settings.Auto_commit && !Game.Settings.Replay_Mode)
        {
            TreeNode? node_to_delete = Game.UI.FindNodeByName(Game.UI.TreeViewLeft.Nodes, "new_snapshot");
            if (node_to_delete == null)
            {
                TreeNode newCommitNode = new("New snapshot");
                newCommitNode.Name = "new_snapshot";
                Game.UI.TreeViewLeft?.Nodes.Add(newCommitNode);
            }
        }
        else if (Game.Settings.Auto_commit || Game.Settings.Replay_Mode)
        {
            TreeNode? node_to_delete = Game.UI.FindNodeByName(Game.UI.TreeViewLeft.Nodes, "new_snapshot");
            if (node_to_delete != null)
            {
                Game.UI.TreeViewLeft.Nodes.Remove(node_to_delete);
            }
        }
    }

    public static void Replay_Mode_Node()
    {
        if (!Game.Settings.Auto_commit && Game.Settings.Replay_Mode)
        {
            TreeNode replayNode = new("Replay Mode");
            replayNode.Name = "replay_mode";
            TreeNode? timeline_root = Game.UI.FindNodeByName(Game.UI.TreeViewLeft.Nodes, "timeline_root");
            if (timeline_root != null)
            {
                // Disable selection of timeline_root (user is unable to interact with the timeline_root node)
                Game.UI.Timeline_history.Text = "Timeline - Replay Mode";
                Game.UI.Timeline_history.ForeColor = SystemColors.GrayText;
                Game.UI.beforeSelectHandler = (s, e) => { if (e.Node?.Name == "timeline_root") e.Cancel = true; };
                Game.UI.TreeViewLeft.BeforeSelect += Game.UI.beforeSelectHandler;
            }
            Game.UI.TreeViewLeft.Nodes.Add(replayNode);

            // @ HACK to force selection of replay_node
            Game.UI.ForceNodeSelection(replayNode);
        }
        else if (Game.Settings.Auto_commit || !Game.Settings.Replay_Mode)
        {
            TreeNode? node_to_delete = Game.UI.FindNodeByName(Game.UI.TreeViewLeft.Nodes, "replay_mode");
            if (node_to_delete != null)
            {
                Game.UI.TreeViewLeft.Nodes.Remove(node_to_delete);
                Game.UI.TreeViewLeft.ExpandAll();
                Game.UI.TreeViewLeft.PerformLayout();

                // Re-enable selection of timeline_root (user is now able to interact with timeline_root node)
                Game.UI.Timeline_history.Text = "Timeline - " + Game.Name;
                Game.UI.TreeViewLeft.BeforeSelect -= Game.UI.beforeSelectHandler;

                // @ HACK to force selection of timeline_root
                Game.UI.ForceNodeSelection(Game.UI.TreeViewLeft.Nodes["timeline_root"]);
            }
        }
    }
}

