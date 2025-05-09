using System.Reflection;
using System.Drawing.Drawing2D;

namespace Panopticon;

public partial class Home : Form
{

    public Home()
    {
        this.ClientSize = new System.Drawing.Size(800, 800);
        this.Text = "Dominion: Panopticon";
        
        // Load App icon
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string icon_path = Path.Combine(basePath, "Assets", "app.ico");
        Icon = new Icon(icon_path);

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
        var LoadTimeLine = new LoadTimeline
        {
            Location = this.Location,
            StartPosition = FormStartPosition.CenterScreen
        };
        LoadTimeLine.FormClosing += delegate { this.Show(); };
        LoadTimeLine.Show();
        this.Hide();
    }

    private void InitializeAboutSection()
    {
        // Get informationalVersion from the current build
        // Strip git commit (e.g., +b5fa5fe4...) via Split
        var informationalVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "Unknown";

        Label About = new()
        {
            Dock = DockStyle.Bottom,
            Text = $"Coded by globz - https://github.com/globz - v{informationalVersion}",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Game.UI.ForeColor,
            Height = 50
        };

        Controls.Add(About);
    }

    private Image? LoadBackgroundImage()
    {
        try
        {
            // Use the executable's directory as the base path
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string imagePath = Path.Combine(basePath, "Assets", "background.png");

            // Check if the file exists
            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"Image not found: {imagePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Load the image
            return Image.FromFile(imagePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    private void InitializeBackground()
    {
        Image? bg_source = LoadBackgroundImage();
        BackColor = Game.UI.Theme;
        if (bg_source != null)
        {
            Image bg = RoundCorners(bg_source, 250, Game.UI.Theme);
            BackgroundImage = bg;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
        }
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


