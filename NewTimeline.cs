using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using LibGit2Sharp;

namespace Panopticon;

public partial class NewTimeline : Form
{

    public NewTimeline()
    {
        this.ClientSize = new System.Drawing.Size(500, 150);
        this.Text = "New Timeline";
        this.BackColor = Game.UI.Theme;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        InitializeAboutSection();
    }

    private void InitializeAboutSection()
    {

        Label About = new()
        {
            Dock = DockStyle.Top,
            Text = "You are about to create a new Timeline."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + "You must first select an existing Dominion game by navigating to the <savedgames> folder."
            + System.Environment.NewLine
            + "Default location @ C:\\Users\\USERNAME\\AppData\\Roaming\\Dominions6\\savedgames",
            TextAlign = ContentAlignment.MiddleCenter,
            //BackColor = Color.Honeydew, // Color.Honeydew
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

            // Validate if a Timeline has already created
            if (Repository.IsValid(Game.Path))
            {
                MessageBox.Show("The game you selected already has an active Timeline."+ System.Environment.NewLine + "Please use the Load option.");
            } 
            else
            {
                // No timeline detected, lets proceeded by binding the next EventHandler
                var ContinueButton = sender as Button ?? throw new ArgumentException();
                ContinueButton.Text = "Configure Timeline: " + Game.Name;
                ContinueButton.BackColor = Color.LightGreen;
                ContinueButton.Click -= BrowseButton_Click;
                ContinueButton.Click += new EventHandler(this.ContinueButton_Click);
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