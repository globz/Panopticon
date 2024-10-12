using System.DirectoryServices;
using System.Security.Cryptography.X509Certificates;
using LibGit2Sharp;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using SQLitePCL;

namespace Panopticon;

public partial class Timeline : Form
{
    public Timeline()
    {
        // DPI scaling
        AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

        InitializeComponent();

        // Initialize the settings database
        Initialize_Settings_DB();

        // Retrieve saved settings
        Retrieve_Settings();

        // Refresh Timeline nodes
        Refresh_Timeline_Nodes();

        // Enable automatic Turn tracking if needed
        TurnTrackerWorker turnTracker = new TurnTrackerWorker();
        turnTracker.Watch();

        // Enable manual snapshot mode if needed
        Enable_Manual_Snapshot();

    }

    private void InitializeComponent()
    {
        var verticalSplitContainer = new System.Windows.Forms.SplitContainer();
        var treeViewLeft = new System.Windows.Forms.TreeView();
        var horizontalSplitContainer = new System.Windows.Forms.SplitContainer();
        var topPanel = new System.Windows.Forms.Panel();
        var bottomPanel = new System.Windows.Forms.Panel();

        // DPI scaling
        verticalSplitContainer.AutoScaleMode = AutoScaleMode.Dpi;
        verticalSplitContainer.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        horizontalSplitContainer.AutoScaleMode = AutoScaleMode.Dpi;
        horizontalSplitContainer.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);

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

    protected void TreeViewLeft_AfterSelect(object? sender, System.Windows.Forms.TreeViewEventArgs e)
    {
        // Update reference to the current selected node
        Game.UI.SelectedNode = e.Node;

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
                Snapshot.InitializeComponent();
                break;
            default:
                Initialize_Timeline_Node(e.Node?.Name);
                break;
        }

    }

    private static void Initialize_Timeline_Root()
    {
        var groupBox_timeline_root = new System.Windows.Forms.GroupBox();

        // Validate if a Timeline has already been created (via git)
        if (Git.Exist(Game.Path))
        {
            Game.UI.TopPanel?.Controls.Clear();
            Game.UI.BottomPanel?.Controls.Clear();

            Label description = new()
            {
                Text = $"This is the root of Timeline - {Game.Name}."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + $"This Timeline is part of the following branch : {Git.CurrentBranch()}"
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
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            groupBox_timeline_root.Controls.Add(DeleteTimelineButton);
            groupBox_timeline_root.Controls.Add(SwitchTimelineBranchButton);
            groupBox_timeline_root.Controls.Add(SaveDescriptionButton);
            groupBox_timeline_root.Location = new System.Drawing.Point(10, 5);
            groupBox_timeline_root.Size = new System.Drawing.Size(220, 125);
            groupBox_timeline_root.Text = "Timeline - " + Game.Name;
            groupBox_timeline_root.ForeColor = Color.Orange;

            TextBox DescriptionBox = new()
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                Dock = DockStyle.Bottom,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new System.Drawing.Size(100, 250),
                BackColor = Color.LightYellow
            };

            // Retrieve DescriptionBox Text
            DescriptionBox.Text = Retrieve_Timeline_Notes();

            Game.UI.TopPanel?.Controls.Add(groupBox_timeline_root);
            Game.UI.BottomPanel?.Controls.Add(DescriptionBox);
            Game.UI.BottomPanel?.Controls.Add(description);
            Game.UI.HorizontalSplitContainer.SplitterDistance = 150;

            DeleteTimelineButton.Click += new EventHandler(DeleteTimelineButton_Click);
            SwitchTimelineBranchButton.Click += new EventHandler(SwitchTimelineBranchButton_Click);
            SaveDescriptionButton.Click += (sender, e) => SaveNotesButton_Click(DescriptionBox);
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
            + $"This node is part of the following branch : {Git.CurrentBranch()}"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You may do the following actions:"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "Add notes about this turn."
            + System.Environment.NewLine
            + "Time travel to this turn.",
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
            Location = new System.Drawing.Point(40, 53),
            Text = "Save Notes",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_timeline_node.Controls.Add(TimeTravelButton);
        groupBox_timeline_node.Controls.Add(SaveNotesButton);
        groupBox_timeline_node.Location = new System.Drawing.Point(10, 5);
        groupBox_timeline_node.Size = new System.Drawing.Size(220, 115);
        groupBox_timeline_node.Text = nodeName;
        groupBox_timeline_node.ForeColor = Color.Orange;

        TextBox NotesBox = new()
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            Dock = DockStyle.Bottom,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Size = new System.Drawing.Size(100, 250),
            BackColor = Color.LightYellow,
        };

        // Retrieve NotesBox Text
        NotesBox.Text = Retrieve_Timeline_Notes();

        Game.UI.TopPanel?.Controls.Add(groupBox_timeline_node);
        Game.UI.BottomPanel?.Controls.Add(NotesBox);
        Game.UI.BottomPanel?.Controls.Add(description);

        TimeTravelButton.Click += new EventHandler(TimeTravelButton_Click);
        SaveNotesButton.Click += (sender, e) => SaveNotesButton_Click(NotesBox);
    }

    static void CreateTimelineButton_Click(object? sender, EventArgs e)
    {

        // Initialize the timelines database
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

        // Save current commit information to timelines DB
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
            DB.Open();
            SqliteCommand statement = DB.Query("DELETE FROM settings WHERE game = @game");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.ExecuteNonQuery();
            DB.Close();

            // Delete timeline
            DB.Open();
            statement = DB.Query("DELETE FROM timelines WHERE game = @game");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.ExecuteNonQuery();
            DB.Close();

            // Delete notes
            DB.Open();
            statement = DB.Query("DELETE FROM notes WHERE game = @game");
            statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
            statement.ExecuteNonQuery();
            DB.Close();

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

    static void SwitchTimelineBranchButton_Click(object? sender, EventArgs e)
    {
        Game.UI.BottomPanel?.Controls.Clear();
    }

    static void SaveNotesButton_Click(TextBox notes)
    {
        DB.SaveTimelineNotes(notes.Text);
        MessageBox.Show("Timeline description saved!");
    }

    static void TimeTravelButton_Click(object? send, EventArgs e)
    {
        Game.UI.BottomPanel?.Controls.Clear();

        var groupBox_TimeTravelActions = new System.Windows.Forms.GroupBox();

        // TODO 
        // check if commit hash matches the HEAD = Block Replay action
        // Permenantly undo this turn = Delete the node for timeline_history
        // Cannot undo node_seq 1 (block action) ~ still guard the code of this case
        // Replay this turn (block action when selected node is HEAD)
        // Branch off
        // Create new game

        Label description = new()
        {
            Text = $"This node is part of your Timeline - {Game.Name}"
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"This Timeline is part of the following branch : {Git.CurrentBranch()}"
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
    }

    static void UndoButton_Click(object? send, EventArgs e)
    {
        Game.UI.BottomPanel?.Controls.Clear();

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
            Console.WriteLine($"node_seq_start & end: {node_seq}");
        }
        else
        {
            // Build range sequence to be deleted
            int[] range_seq_to_be_deleted = Enumerable.Range((int)node_seq, (int)absolute_delta).ToArray();
            range_seq_to_be_deleted.ToList().ForEach(i => Console.WriteLine($" range_seq_to_be_deleted: {i.ToString()}"));
            node_seq_start = range_seq_to_be_deleted.First();
            node_seq_end = range_seq_to_be_deleted.Max();
        }

        // Do not undo the first commit (Delete and rebuild a new timeline instead!)
        if (node_seq != 1)
        {

            // Destructive actions below:

            // git reset --hard using the parent_node commit_hash
            Git.ResetHard(commit_hash);

            // Retrieve node_name(s) associated with this commit_hash along with subsequent nodes
            List<string> timeline_nodes_name = new List<string>();
            DB.Open();
            statement = DB.Query("SELECT node_name FROM timelines WHERE game = @game AND branch = @branch AND node_seq between @node_seq_start AND @node_seq_end");
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
        else
        {
            MessageBox.Show("You cannot do this action.");
        }
    }

    private static void Initialize_Settings_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS settings (game VARCHAR(23) PRIMARY KEY, auto_commit BOOLEAN, prefix VARCHAR(10), suffix VARCHAR(10), turn INT, sq_turn DOUBLE, compound_turn DOUBLE)").ExecuteNonQuery();
        DB.Close();
    }

    private static void Retrieve_Settings()
    {
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT auto_commit, prefix, suffix, turn, sq_turn, compound_turn FROM settings WHERE game = @game");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        DB.ReadData(statement, DB.LoadSettingsData);
        DB.Close();
    }

    private static void Initialize_Timelines_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS timelines (game VARCHAR(23), branch VARCHAR(100), node_name VARCHAR(43), node_seq INT, commit_hash TEXT NOT NULL, PRIMARY KEY (game, branch, node_name))").ExecuteNonQuery();
        DB.Close();
    }

    private static Dictionary<int, string> Retrieve_TimelineNodes()
    {
        var timeline_nodes = new Dictionary<int, string> { };

        DB.Open();
        SqliteCommand statement = DB.Query("SELECT node_name, node_seq FROM timelines WHERE game = @game AND branch = @branch ORDER BY node_seq");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        SqliteDataReader timeline = statement.ExecuteReader();

        while (timeline.Read())
        {
            timeline_nodes.Add(Convert.ToInt32(timeline["node_seq"]), (string)timeline["node_name"]);
        }
        DB.Close();

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
        }
    }

    private static void Initialize_Notes_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS notes (game VARCHAR(23), branch VARCHAR(100), node_name VARCHAR(43), notes TEXT, PRIMARY KEY (game, branch, node_name))").ExecuteNonQuery();
        DB.Close();
    }

    private static string? Retrieve_Timeline_Notes()
    {
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT notes FROM notes WHERE game = @game AND branch = @branch AND node_name = @node_name");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.Parameters.Add("@branch", SqliteType.Text).Value = Git.CurrentBranch();
        statement.Parameters.Add("@node_name", SqliteType.Text).Value = Game.UI.SelectedNode?.Name;
        var data = statement.ExecuteScalar();
        string? Notes = (data != null) ? data.ToString() : "Add a description.";
        DB.Close();
        return Notes;
    }

    public static void Enable_Manual_Snapshot()
    {
        if (!Game.Settings.Auto_commit)
        {
            TreeNode newCommitNode = new("New snapshot");
            newCommitNode.Name = "new_snapshot";
            Game.UI.TreeViewLeft?.Nodes.Add(newCommitNode);
        }
    }

}

