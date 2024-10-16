namespace Panopticon;

public partial class LoadTimeline : Form
{

    public LoadTimeline()
    {
        this.ClientSize = new System.Drawing.Size(500, 150);
        this.Text = "Load Timeline";
        this.BackColor = Game.UI.Theme;

        InitializeAboutSection();
    }

    private void InitializeAboutSection()
    {

        Label About = new()
        {
            Dock = DockStyle.Top,
            Text = "You are about to load an existing Timeline."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You must first select an existing Dominion game by navigating to the <savedgames> folder."
            + System.Environment.NewLine
            + "Default location @ C:\\Users\\USERNAME\\AppData\\Roaming\\Dominions6\\savedgames",
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 100,
            ForeColor = Game.UI.ForeColor
        };

        Button BrowseButton = new()
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Text = "Browse...",
            BackColor = Color.LightCoral,
            Padding = new(2),
        };

        Controls.Add(About);
        Controls.Add(BrowseButton);
        BrowseButton.Click += new EventHandler(this.BrowseButton_Click);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using FolderBrowserDialog FolderBrowserDialog = new();
        if (FolderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            // Set Game properties
            Game.Path = FolderBrowserDialog.SelectedPath;
            Game.Name = Path.GetFileName(FolderBrowserDialog.SelectedPath);

            // Validate if a Timeline has already been created (via git)
            if (Git.Exist(Game.Path))
            {
                // No timeline detected, lets proceeded by binding the next EventHandler
                var ContinueButton = sender as Button ?? throw new ArgumentException();
                ContinueButton.Text = "Load Timeline: " + Game.Name;
                ContinueButton.BackColor = Color.LightGreen;
                ContinueButton.Click -= BrowseButton_Click;
                ContinueButton.Click += new EventHandler(this.ContinueButton_Click);

            }
            else
            {
                MessageBox.Show("The game you selected does not have an active Timeline." + System.Environment.NewLine + "Please use the New Timeline option.");
            }

        }
    }

    private void ContinueButton_Click(object? sender, EventArgs e)
    {
        var TimeLine = new Timeline
        {
            Location = this.Location,
            StartPosition = FormStartPosition.CenterScreen
        };
        TimeLine.FormClosing += delegate { this.Show(); };
        TimeLine.Show();
        this.Hide();

        // Reset EventHandler back to BrowseButton
        var ContinueButton = sender as Button ?? throw new ArgumentException();
        ContinueButton.Click -= ContinueButton_Click;
        ContinueButton.Click += new EventHandler(this.BrowseButton_Click);
        ContinueButton.Text = "Browse...";
        ContinueButton.BackColor = Color.LightCoral;
    }

}