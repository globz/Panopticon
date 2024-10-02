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
        Retrieve_Game_Settings();

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
        // Dispatch based on selected node name
        switch (e.Node?.Name)
        {
            case "settings":
                SuspendLayout();
                Settings.InitializeSettings();
                ResumeLayout(false);
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
                + "Add a description about your game."
                + System.Environment.NewLine
                + "Switch to an alternate branch from your root Timeline."
                + System.Environment.NewLine
                + "Permenantly delete your Timeline.",
                Dock = DockStyle.Fill
            };

            Button DeleteTimelineButton = new()
            {
                Location = new System.Drawing.Point(40, 40),
                Text = "Delete Timeline",
                BackColor = Color.LightSteelBlue,
                ForeColor = Game.UI.ForeColor,
                Padding = new(2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            groupBox_timeline_root.Controls.Add(DeleteTimelineButton);
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
                BackColor = Color.LightSteelBlue,
                Text = "Add a description."

            };

            Game.UI.TopPanel?.Controls.Add(groupBox_timeline_root);
            Game.UI.BottomPanel?.Controls.Add(DescriptionBox);
            Game.UI.BottomPanel?.Controls.Add(description);

            DeleteTimelineButton.Click += new EventHandler(DeleteTimelineButton_Click);
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

        // Initialize the timeline database
        Initialize_Timeline_DB();

        // Save all game settings in case they were not configured & saved by the user
        DB.SaveAllSettings();

        // Init repo
        Git.Init(Game.Path);

        // Commit all changes
        Git.Commit(Game.Path, Git.commit_title);

        // Child nodes should be created based on git history
        TreeNode newTimelineNode = new(Git.commit_title);
        newTimelineNode.Name = Git.commit_title;
        Game.UI.Timeline_history?.Nodes.Add(newTimelineNode);
        Game.UI.Timeline_history?.ExpandAll();

        MessageBox.Show("Timeline created successfully!");

        // Reload the node
        Initialize_Timeline_Root();
    }

    static void DeleteTimelineButton_Click(object? sender, EventArgs e)
    {
        // Delete timeline settings
        DB.Open();
        SqliteCommand statement = DB.Query("DELETE FROM settings WHERE game = @game");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.ExecuteNonQuery();
        DB.Close();

        // Delete timeline settings
        DB.Open();
        statement = DB.Query("DELETE FROM timeline WHERE game = @game");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        statement.ExecuteNonQuery();
        DB.Close();        

        // Delete git repo
        Git.Delete_Repo(Game.Path);

        // Delete child nodes from Timeline history
        Game.UI.Timeline_history?.Nodes.Clear();

        MessageBox.Show("Timeline deleted successfully!");

        // Reload the node
        Initialize_Timeline_Root();
    }

    private static void Initialize_Settings_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS settings (game VARCHAR(23) PRIMARY KEY, auto_commit BOOLEAN, prefix VARCHAR(10), suffix VARCHAR(10), turn DECIMAL)").ExecuteNonQuery();
        DB.Close();
    }

    private static void Retrieve_Game_Settings()
    {
        DB.Open();
        SqliteCommand statement = DB.Query("SELECT auto_commit, prefix, suffix, turn FROM settings WHERE game = @game");
        statement.Parameters.Add("@game", SqliteType.Text).Value = Game.Name;
        DB.ReadData(statement, DB.LoadSettingsData);
        DB.Close();
    }

    private static void Initialize_Timeline_DB()
    {
        DB.Open();
        DB.Query("CREATE TABLE IF NOT EXISTS timeline (game VARCHAR(23), branch VARCHAR(100), node_name VARCHAR(43), node_seq INT, commit_hash TEXT NOT NULL, notes TEXT, PRIMARY KEY (game, branch, node_name))").ExecuteNonQuery();
        DB.Close();
    }

}

