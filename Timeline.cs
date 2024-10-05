using LibGit2Sharp;
using Microsoft.Data.Sqlite;

namespace Panopticon;

public partial class Timeline : Form
{
    public Timeline()
    {
        // DPI scaling
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

        InitializeComponent();

        // Initialize the settings database
        Initialize_Settings_DB();

        // Retrieve saved settings
        Retrieve_Settings();

        // Refresh Timeline nodes
        Refresh_Timeline_Nodes();

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
                Settings.InitializeSettings();
                break;
            case "timeline_root":
                Initialize_Timeline_Root();
                break;
            default:
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
                Text = "This is the root of your Timeline."
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
                Location = new System.Drawing.Point(40, 50),
                Text = "Switch Branch",
                BackColor = Color.LightSteelBlue,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            Button SaveDescriptionButton = new()
            {
                Location = new System.Drawing.Point(40, 80),
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
            groupBox_timeline_root.Size = new System.Drawing.Size(220, 115);
            groupBox_timeline_root.Text = "Timeline - " + Game.Name;
            groupBox_timeline_root.ForeColor = Color.Orange;

            TextBox DescriptionBox = new()
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                Dock = DockStyle.Bottom,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new System.Drawing.Size(100, 300),
                BackColor = Color.LightSteelBlue
            };

            // Retrieve DescriptionBox Text
            DescriptionBox.Text = Retrieve_Timeline_Notes();

            Game.UI.TopPanel?.Controls.Add(groupBox_timeline_root);
            Game.UI.BottomPanel?.Controls.Add(DescriptionBox);
            Game.UI.BottomPanel?.Controls.Add(description);

            DeleteTimelineButton.Click += new EventHandler(DeleteTimelineButton_Click);
            SwitchTimelineBranchButton.Click += new EventHandler(SwitchTimelineBranchButton_Click);
            SaveDescriptionButton.Click += (sender, e) => SaveDescriptionButton_Click(sender, e, DescriptionBox);
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
        Git.Commit(Game.Path, Git.commit_title);

        // Rename master branch to root
        using var repo = new Repository(Game.Path);
        repo.Branches.Rename("master", "root");

        // Save current commit information to timelines DB
        DB.SaveTimeline();

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
        MessageBox.Show("Switching branch menu");
    }

    static void SaveDescriptionButton_Click(object? sender, EventArgs e, TextBox description)
    {
        DB.SaveTimelineNotes(description.Text);
        MessageBox.Show("Timeline description saved!");
    }

    private static void Initialize_Settings_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS settings (game VARCHAR(23) PRIMARY KEY, auto_commit BOOLEAN, prefix VARCHAR(10), suffix VARCHAR(10), turn DECIMAL)").ExecuteNonQuery();
        DB.Close();
    }

    private static void Retrieve_Settings()
    {
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT auto_commit, prefix, suffix, turn FROM settings WHERE game = @game");
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
        SqliteCommand statement = DB.Query("SELECT node_name, node_seq FROM timelines WHERE game = @game AND branch = @branch");
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

    private static void Refresh_Timeline_Nodes()
    {
        if (Git.Exist(Game.Path))
        {
            Dictionary<int, string> timeline_nodes = Retrieve_TimelineNodes();

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

}

