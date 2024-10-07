namespace Panopticon;
public class Snapshot
{
    public static void InitializeComponent()
    {
        var groupBox_snapshot = new System.Windows.Forms.GroupBox();

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

        Game.UI.TopPanel?.Controls.Clear();
        Game.UI.TopPanel?.Controls.Add(groupBox_snapshot);

        Game.UI.BottomPanel?.Controls.Clear();
        Game.UI.BottomPanel?.Controls.Add(description);

        NewSnapshotButton.Click += new EventHandler(NewSnapshotButton_Click);
    }

    static void NewSnapshotButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("New snapshot!");
    }
}