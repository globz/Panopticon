using Microsoft.Data.Sqlite;

namespace Panopticon;
public class Settings
{
    public static void InitializeComponent()
    {
        var groupBox_settings = new System.Windows.Forms.GroupBox();

        Button CreationModeButton = new()
        {
            Location = new System.Drawing.Point(20, 20),
            Text = "Creation mode",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        Button NamingConventionButton = new()
        {
            Location = new System.Drawing.Point(20, 53),
            Text = "Naming convention",
            BackColor = Color.LightSteelBlue,
            ForeColor = Game.UI.ForeColor,
            Padding = new(2),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        groupBox_settings.Controls.Add(CreationModeButton);
        groupBox_settings.Controls.Add(NamingConventionButton);
        groupBox_settings.Location = new System.Drawing.Point(10, 5);
        groupBox_settings.Size = new System.Drawing.Size(220, 115);
        groupBox_settings.Text = "Settings";
        groupBox_settings.ForeColor = Color.Orange;

        var settings_description = "Please configure your settings before creating your Timeline."
        + System.Environment.NewLine
        + System.Environment.NewLine
        + "Once configured, navigate to [Timeline - " + Game.Name + "] via the left pane and initialize your Timeline.";

        if (Git.Exist(Game.Path))
        {
            settings_description = "You may adjust your current turn if it does not reflect your current in-game turn."
            + System.Environment.NewLine
            + System.Environment.NewLine
            + $"This configuration applies to [Timeline - " + Game.Name + "] on branch [" + Git.CurrentBranch() + "]";
        }


        Label description = new()
        {
            Text = $"{settings_description}",
            Dock = DockStyle.Fill
        };

        Game.UI.TopPanel?.Controls.Clear();
        Game.UI.TopPanel?.Controls.Add(groupBox_settings);

        Game.UI.BottomPanel?.Controls.Clear();
        Game.UI.BottomPanel?.Controls.Add(description);

        CreationModeButton.Click += new EventHandler(CreationModeButton_Click);
        NamingConventionButton.Click += new EventHandler(NamingConventionButton_Click);

        static void CreationModeButton_Click(object? sender, EventArgs e)
        {
            InitializeCreationMode();
        }

        static void NamingConventionButton_Click(object? sender, EventArgs e)
        {
            InitializeNamingConvention();
        }

        static void InitializeCreationMode()
        {
            Label description = new()
            {
                Text = "Please select a creation mode."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "[Auto] = New Timeline nodes (snapshot) will be added after each new turn."
                + System.Environment.NewLine
                + "[Manual] = You must manually create new Timeline nodes (snapshot) whenever you see fit.",
                Dock = DockStyle.Fill
            };

            var groupBox_creationMode = new System.Windows.Forms.GroupBox();
            var radioButton_autoMode = new System.Windows.Forms.RadioButton();
            var radioButton_manualMode = new System.Windows.Forms.RadioButton();

            groupBox_creationMode.Location = new System.Drawing.Point(30, 100);
            groupBox_creationMode.Size = new System.Drawing.Size(150, 105);
            groupBox_creationMode.Text = "Creation Mode";
            groupBox_creationMode.ForeColor = Color.Orange;

            radioButton_autoMode.Location = new System.Drawing.Point(31, 53);
            radioButton_autoMode.Size = new System.Drawing.Size(70, 30);
            radioButton_autoMode.Name = "AutoMode";
            radioButton_autoMode.Text = "Auto";
            radioButton_autoMode.ForeColor = Game.UI.ForeColor;

            radioButton_manualMode.Location = new System.Drawing.Point(31, 20);
            radioButton_manualMode.Name = "ManualMode";
            radioButton_manualMode.Size = new System.Drawing.Size(90, 30);
            radioButton_manualMode.Text = "Manual";
            radioButton_manualMode.ForeColor = Game.UI.ForeColor;

            // Determine user Auto commit state
            if (!Game.Settings.Auto_commit)
            {
                radioButton_manualMode.Checked = true;
            }
            else
            {
                radioButton_autoMode.Checked = true;
            }

            // Disable Auto commit radio button when replay mode is active
            if (Game.Settings.Replay_Mode)
            {
                radioButton_autoMode.Enabled = false;
                radioButton_autoMode.Paint += new PaintEventHandler(Disabled_Text_Override_Paint);
            }

            static void Disabled_Text_Override_Paint(object? sender, PaintEventArgs e)
            {
                if (sender is RadioButton rb)
                {
                    e.Graphics.Clear(rb.BackColor); // Clear background
                    TextRenderer.DrawText(e.Graphics, rb.Text, rb.Font,
                        rb.ClientRectangle, Color.Red, TextFormatFlags.Left);
                }
            }                 

            groupBox_creationMode.Controls.Add(radioButton_autoMode);
            groupBox_creationMode.Controls.Add(radioButton_manualMode);

            Game.UI.BottomPanel?.Controls.Clear();
            Game.UI.BottomPanel?.Controls.Add(groupBox_creationMode);
            Game.UI.BottomPanel?.Controls.Add(description);

            radioButton_autoMode.CheckedChanged += new EventHandler(RB_CreationMode_CheckedChanged);
            radioButton_manualMode.CheckedChanged += new EventHandler(RB_CreationMode_CheckedChanged);

            static void RB_CreationMode_CheckedChanged(object? sender, EventArgs e)
            {
                RadioButton rb = sender as RadioButton ?? throw new ArgumentException();

                if (rb.Checked && rb != null)
                {
                    switch (rb.Name)
                    {
                        case "ManualMode":
                            Game.Settings.Auto_commit = false;
                            // Enable Manual Snapshot Node
                            Timeline.Manual_Snapshot_Node();
                            break;
                        case "AutoMode":
                            Game.Settings.Auto_commit = true; 
                            // Disable Manual Snapshot Node
                            Timeline.Manual_Snapshot_Node();
                            break;
                        default:
                            break;
                    }
                    DB.SaveAllSettings();
                }
            }


        }

        static void InitializeNamingConvention()
        {

            Label description = new()
            {
                Text = "Please define your prefix, suffix and current game turn."
                + System.Environment.NewLine
                + System.Environment.NewLine
                + "These values will be appended to your game name on each new turn."
                + System.Environment.NewLine
                + "This string is used when naming a new timeline node."
                + System.Environment.NewLine
                + "Please input the current turn of your selected game."
                + System.Environment.NewLine
                + "Current turn will auto increment on its own on each new snapshot."
                + System.Environment.NewLine,
                Dock = DockStyle.Fill
            };

            var saveSettingsButton = new System.Windows.Forms.Button();

            var textBoxField_prefix = new TextBoxField("Prefix:")
            {
                TabIndex = 1,
                Location = new System.Drawing.Point(5, 125),
                ForeColor = Game.UI.ForeColor,
                Text = Game.Settings.Prefix ?? "",
                MaxLength = 10
            };

            var textBoxField_suffix = new TextBoxField("Suffix:")
            {
                TabIndex = 2,
                Location = new System.Drawing.Point(5, 150),
                ForeColor = Game.UI.ForeColor,
                Text = Game.Settings.Suffix ?? "",
                MaxLength = 10
            };

            Label updatedName = new()
            {
                Text = "Your Timeline nodes will named after the following pattern: "
                + System.Environment.NewLine
                + Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn,
                Location = new System.Drawing.Point(5, 300),
                Size = new System.Drawing.Size(500, 100),
                ForeColor = Color.Orange
            };

            var numericUpDownField_turn = new NumericUpDownField("Current Turn:", updatedName)
            {
                Value = Game.Settings.Turn,
                Minimum = 1,
                Maximum = 9999,
                TabIndex = 3,
                Location = new System.Drawing.Point(5, 185),
                ForeColor = Game.UI.ForeColor,
            };

            saveSettingsButton.Location = new System.Drawing.Point(10, 225);
            saveSettingsButton.Size = new System.Drawing.Size(200, 25);
            saveSettingsButton.Text = "Save";

            Game.UI.BottomPanel?.Controls.Clear();
            Game.UI.BottomPanel?.Controls.Add(saveSettingsButton);
            Game.UI.BottomPanel?.Controls.Add(numericUpDownField_turn);
            Game.UI.BottomPanel?.Controls.Add(textBoxField_suffix);
            Game.UI.BottomPanel?.Controls.Add(textBoxField_prefix);
            Game.UI.BottomPanel?.Controls.Add(updatedName);
            Game.UI.BottomPanel?.Controls.Add(description);

            textBoxField_prefix.Leave += (sender, e) => TextBoxField_prefix_Leave(textBoxField_prefix, e, updatedName);
            textBoxField_suffix.Leave += (sender, e) => TextBoxField_suffix_Leave(textBoxField_suffix, e, updatedName);
            saveSettingsButton.Click += SaveSettingsButton_Click;

            static void TextBoxField_prefix_Leave(object sender, EventArgs e, Label updatedName)
            {
                var _textBoxField_prefix = sender as TextBoxField ?? throw new ArgumentException();
                Game.Settings.Prefix = _textBoxField_prefix.Text ?? "";
                updatedName.Text = "Your Timeline nodes will named after the following pattern: "
                            + System.Environment.NewLine
                            + Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
                updatedName.Invalidate();
                updatedName.Update();
                updatedName.Refresh();
                Application.DoEvents();
            }

            static void TextBoxField_suffix_Leave(object sender, EventArgs e, Label updatedName)
            {
                var _textBoxField_suffix = sender as TextBoxField ?? throw new ArgumentException();
                Game.Settings.Suffix = _textBoxField_suffix.Text ?? "";
                updatedName.Text = "Your Timeline nodes will named after the following pattern: "
                            + System.Environment.NewLine
                            + Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
                updatedName.Invalidate();
                updatedName.Update();
                updatedName.Refresh();
                Application.DoEvents();
            }

            static void SaveSettingsButton_Click(object? sender, EventArgs e)
            {
                DB.SaveAllSettings();
                MessageBox.Show("Saved successfully!");
            }

        }

    }

    internal class TextBoxField : TableLayoutPanel
    {
        private readonly TextBox _textBox;

        public new string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public int MaxLength
        {
            get => _textBox.MaxLength;
            set => _textBox.MaxLength = value;
        }

        public TextBoxField(string labelText)
        {
            var label = new Label { Text = labelText, AutoSize = true };
            var labelMargin = label.Margin;
            labelMargin.Top = 8;
            label.Margin = labelMargin;
            _textBox = new TextBox { Dock = DockStyle.Fill };

            AutoSize = true;

            ColumnCount = 2;
            RowCount = 1;
            ColumnStyles.Add(new ColumnStyle());
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle());
            Controls.Add(label, 0, 0);
            Controls.Add(_textBox, 1, 0);
        }
    }

    internal class NumericUpDownField : TableLayoutPanel
    {
        private readonly NumericUpDown _numericUpDown;
        public required decimal Value
        {
            get => _numericUpDown.Value;
            set => _numericUpDown.Value = value;
        }

        public required decimal Minimum
        {
            get => _numericUpDown.Minimum;
            set => _numericUpDown.Minimum = value;
        }

        public decimal Maximum
        {
            get => _numericUpDown.Maximum;
            set => _numericUpDown.Maximum = value;
        }

        public NumericUpDownField(string labelText, Label _label)
        {
            var label = new Label { Text = labelText, AutoSize = true };
            var labelMargin = label.Margin;
            labelMargin.Top = 8;
            label.Margin = labelMargin;
            _numericUpDown = new NumericUpDown { Dock = DockStyle.Fill };
            _numericUpDown.ValueChanged += (sender, e) => NumericUpDownField_ValueChanged(sender, e, _label);

            ColumnCount = 2;
            RowCount = 1;
            ColumnStyles.Add(new ColumnStyle());
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle());
            Controls.Add(label, 0, 0);
            Controls.Add(_numericUpDown, 1, 0);
        }

        static void NumericUpDownField_ValueChanged(object? sender, EventArgs e, Label updatedName)
        {
            var _numericUpDownField = sender as NumericUpDown ?? throw new ArgumentException();
            Game.Settings.Turn = (int)_numericUpDownField.Value;
            updatedName.Text = "Your Timeline nodes will named after the following pattern: "
                        + System.Environment.NewLine
                        + Game.Settings.Prefix + Game.Name + Game.Settings.Suffix + Game.Settings.Turn;
            updatedName.Invalidate();
            updatedName.Update();
            updatedName.Refresh();
            Application.DoEvents();
        }

    }

}