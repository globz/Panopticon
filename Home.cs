using System.ComponentModel.Design.Serialization;
using System.Drawing.Printing;
using System.Reflection;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.PropertyGridInternal;
using LibGit2Sharp;
using System.Security.Policy;
using Accessibility;
using System.Diagnostics.Tracing;

namespace Panopticon;

public partial class Home : Form
{

    public Home()
    {
        this.ClientSize = new System.Drawing.Size(800, 800);
        this.Text = "Dominion: Panopticon";

        InitializeFeatures();
        InitializeAboutSection();
        InitializeBackground();
    }

    private void InitializeFeatures()
    {
        // Global styling
        Padding padding = new(2);
        const int height = 30;

        Button NewTimeLineButton = new()
        {
            Dock = DockStyle.Top,
            Height = height,
            Text = "New Timeline",
            BackColor = Color.LightGreen,
            Padding = padding,
            TabIndex = 1
        };

        Button LoadTimeLineButton = new()
        {
            Dock = DockStyle.Top,
            Height = height,
            Text = "Load Timeline",
            BackColor = Color.LightSteelBlue,
            Padding = padding,
            TabIndex = 2
        };

        Button BattleLogAnalyzer = new()
        {
            Dock = DockStyle.Top,
            Height = height,
            Text = "Battle Log Analyzer (Under development)",
            BackColor = Color.Plum,
            Padding = padding,
            TabIndex = 3
        };

        Controls.Add(BattleLogAnalyzer);
        Controls.Add(LoadTimeLineButton);
        Controls.Add(NewTimeLineButton);

        NewTimeLineButton.Click += new EventHandler(this.NewTimeLine_Click);
        LoadTimeLineButton.Click += new EventHandler(this.LoadTimeLine_Click);
    }

    private void NewTimeLine_Click(object? sender, EventArgs e)
    {
        var NewTimeLine = new NewTimeline
        {
            Location = this.Location,
            StartPosition = FormStartPosition.CenterScreen
        };
        NewTimeLine.FormClosing += delegate { this.Show(); };
        NewTimeLine.Show();
        this.Hide();
    }

    private void LoadTimeLine_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Load TimeLine");
    }

    private void InitializeAboutSection()
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

        Label About = new()
        {
            Dock = DockStyle.Bottom,
            Text = "Coded by globz - https://github.com/globz - v" + appVersion,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Game.UI.ForeColor,
            Height = 50
        };

        Controls.Add(About);
    }

    private void InitializeBackground()
    {
        Image bg_source = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\Assets\background.png");
        BackColor = Game.UI.Theme; //Color.GhostWhite;
        Image bg = RoundCorners(bg_source, 250, Game.UI.Theme);
        BackgroundImage = bg;
        BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
    }

    private static Bitmap RoundCorners(Image StartImage, int CornerRadius, Color BackgroundColor)
    {
        CornerRadius *= 2;
        Bitmap RoundedImage = new Bitmap(StartImage.Width, StartImage.Height);
        using (Graphics g = Graphics.FromImage(RoundedImage))
        {
            g.Clear(BackgroundColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Brush brush = new TextureBrush(StartImage);
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
            gp.AddArc(0, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
            g.FillPath(brush, gp);
            return RoundedImage;
        }
    }
}


