using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using GuessGame.Gui.Properties;

namespace GuessGame.Gui
{
    public partial class MainForm : Form
    {
        private Label _promptLabel, _resultLabel, _attemptsLabel, _timerLabel, _bestScoreLabel;
        private ProgressBar _progressBar;
        private TextBox _inputBox;
        private Button _guessButton;
        private ComboBox _difficultyBox, _languageBox, _themeBox;
        private DataGridView _leaderboardGrid;

        private readonly Random _random = new();
        private int _target, _attempts, _bestScore = int.MaxValue, _maxRange = 100;
        private readonly Stopwatch _stopwatch = new();
        private Color _defaultBackColor;
        private const string BestScoreFile = "bestscore.txt";
        private string LeaderboardFile => _difficultyBox.SelectedIndex switch
        {
            0 => "leaderboard_easy.txt",
            1 => "leaderboard_medium.txt",
            2 => "leaderboard_hard.txt",
            _ => "leaderboard_unknown.txt"
        };

        private readonly SoundPlayer _winPlayer = new(Path.Combine("Sounds", "win.wav"));
        private static readonly string[] _loseSounds = Directory.GetFiles(Path.Combine("Sounds"), "lose*.wav");
        private bool _isInitializing = true;

        public MainForm()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
            InitializeComponent();
            Load += (_, _) => StartNewGame();
        }

        private void InitializeComponent()
        {
            Text = "🎯 " + Strings.WindowTitle;
            ClientSize = new Size(800, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Font = new Font("Segoe UI", 14);
            _defaultBackColor = Color.WhiteSmoke;
            BackColor = _defaultBackColor;

            // Top bar: language, difficulty, theme
            _languageBox = CreateComboBox(new[] { "English", "Español", "Русский" }, GetLanguageIndex());
            _languageBox.SelectedIndexChanged += LanguageBox_SelectedIndexChanged;

            _difficultyBox = CreateComboBox(new[] { Strings.Easy, Strings.Medium, Strings.Hard }, 1);
            _difficultyBox.SelectedIndexChanged += (_, _) => ChangeDifficulty();

            _themeBox = CreateComboBox(new[] { "Light Mode", "Dark Mode" }, 0);
            _themeBox.SelectedIndexChanged += (_, _) => ToggleTheme();

            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 5,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            // Add spacing columns between controls
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Left spacing
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Right spacing
            
            void SetupComboBox(ComboBox box)
            {
                box.Width = 180;
                box.Anchor = AnchorStyles.None;
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.Font = new Font("Segoe UI", 12);
                box.Cursor = Cursors.Hand;
                box.BackColor = Color.White;
                box.FlatStyle = FlatStyle.Flat;
            }
            
            SetupComboBox(_languageBox);
            SetupComboBox(_difficultyBox);
            SetupComboBox(_themeBox);
            
            topPanel.Controls.Add(_languageBox, 1, 0);
            topPanel.Controls.Add(_difficultyBox, 2, 0);
            topPanel.Controls.Add(_themeBox, 3, 0);

            // Prompt
            _promptLabel = new Label
            {
                Text = Strings.GuessPrompt,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(0, 10, 0, 10)
            };

            // Input + Button centered below prompt
            _inputBox = new TextBox
            {
                Font = new Font("Segoe UI", 16),
                Width = 200,
                Height = 35,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            _inputBox.Enter += (s, e) => _inputBox.BackColor = Color.FromArgb(240, 248, 255);  // Light blue when focused
            _inputBox.Leave += (s, e) => _inputBox.BackColor = Color.White;
            _inputBox.TextChanged += (_, _) => _guessButton.Enabled = !string.IsNullOrWhiteSpace(_inputBox.Text);

            _guessButton = new Button
            {
                Text = Strings.Guess,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Width = 140,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _guessButton.FlatAppearance.BorderSize = 0;
            _guessButton.MouseEnter += (s, e) => _guessButton.BackColor = Color.FromArgb(0, 100, 200);
            _guessButton.MouseLeave += (s, e) => _guessButton.BackColor = Color.FromArgb(0, 120, 215);
            _guessButton.Click += OnGuess;
            AcceptButton = _guessButton;
            _guessButton.Enabled = false;

            var inputPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 15, 0, 15)
            };
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            
            var inputContainer = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            inputContainer.Controls.AddRange(new Control[] { _inputBox, _guessButton });
            _guessButton.Margin = new Padding(10, 0, 0, 0);
            
            inputPanel.Controls.Add(inputContainer, 1, 0);

            // Game status
            _resultLabel = CreateCenterLabel();
            _attemptsLabel = CreateCenterLabel();
            _timerLabel = CreateCenterLabel();
            _bestScoreLabel = CreateCenterLabel();
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 25,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous,
                MarqueeAnimationSpeed = 0,  // Disable marquee animation
                ForeColor = Color.FromArgb(0, 120, 215)  // Match button color
            };

            // Leaderboard
            var leaderboardLabel = new Label
            {
                Text = "🏆 Leaderboard",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

            _leaderboardGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 14),
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 230),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(250, 250, 250)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    SelectionBackColor = Color.LightGray,
                    SelectionForeColor = Color.Black
                }
            };
            _leaderboardGrid.Columns.Add("Name", "Name");
            _leaderboardGrid.Columns.Add("Attempts", "Attempts");
            _leaderboardGrid.Columns.Add("Time", "Time (s)");

            // Main layout
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 12 };
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Top Panel
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Prompt
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Input panel
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Result
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Attempts
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Timer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Best Score
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Leaderboard title with padding
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Spacing
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Leaderboard
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0)); // No bottom spacer needed

            layout.Controls.Add(topPanel, 0, 0);
            layout.Controls.Add(_promptLabel, 0, 1);
            layout.Controls.Add(inputPanel, 0, 2);
            layout.Controls.Add(_resultLabel, 0, 3);
            layout.Controls.Add(_progressBar, 0, 4);
            layout.Controls.Add(_attemptsLabel, 0, 5);
            layout.Controls.Add(_timerLabel, 0, 6);
            layout.Controls.Add(_bestScoreLabel, 0, 7);
            layout.Controls.Add(leaderboardLabel, 0, 8);
            layout.Controls.Add(new Label(), 0, 9);
            layout.Controls.Add(_leaderboardGrid, 0, 10);

            Controls.Add(layout);
            _isInitializing = false;
        }

        private ComboBox CreateComboBox(string[] items, int defaultIndex)
        {
            var combo = new ComboBox();
            combo.Items.AddRange(items);
            combo.SelectedIndex = defaultIndex;
            return combo;
        }

        private Label CreateCenterLabel()
        {
            return new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12)
            };
        }

        private void StartNewGame()
        {
            _target = _random.Next(1, _maxRange + 1);
            _attempts = 0;
            _resultLabel.Text = "";
            _inputBox.Text = "";
            _attemptsLabel.Text = Strings.Attempts + ": 0";
            _timerLabel.Text = Strings.Time + ": 0s";
            LoadScores();
            BackColor = _defaultBackColor;
            _progressBar.Value = 0;
            _stopwatch.Restart();
            _inputBox.Focus();
        }

        private void OnGuess(object? sender, EventArgs e)
        {
            if (!int.TryParse(_inputBox.Text, out int guess))
            {
                _resultLabel.Text = Strings.InvalidInput;
                return;
            }

            _inputBox.Clear();
            _attempts++;
            _attemptsLabel.Text = Strings.Attempts + $": {_attempts}";
            int distance = Math.Abs(guess - _target);
            BackColor = distance switch
            {
                0 => Color.LightGreen,
                <= 5 => Color.LightGoldenrodYellow,
                <= 10 => Color.Khaki,
                _ => Color.LightCoral
            };
            _progressBar.Value = Math.Clamp(100 - distance * 100 / _maxRange, 0, 100);

            if (guess == _target)
            {
                _stopwatch.Stop();
                _winPlayer.Play();
                int time = (int)_stopwatch.Elapsed.TotalSeconds;
                RecordScore(time);
                MessageBox.Show($"🎉 You guessed it!\nAttempts: {_attempts}\nTime: {time}s", Strings.CongratsTitle);
                StartNewGame();
            }
            else
            {
                new SoundPlayer(_loseSounds[_random.Next(_loseSounds.Length)]).Play();
                _resultLabel.Text = guess < _target ? Strings.TooLow : Strings.TooHigh;
            }

            _timerLabel.Text = Strings.Time + $": {(int)_stopwatch.Elapsed.TotalSeconds}s";
        }

        private void ToggleTheme()
        {
            bool dark = _themeBox.SelectedIndex == 1;
            var bg = dark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            var fg = dark ? Color.White : Color.Black;

            foreach (Control c in ControlsRecursive(this))
            {
                c.BackColor = bg;
                c.ForeColor = fg;
            }

            BackColor = _defaultBackColor = bg;
            _guessButton.BackColor = dark ? Color.FromArgb(0, 100, 200) : Color.FromArgb(0, 120, 215);
        }

        private IEnumerable<Control> ControlsRecursive(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;
                foreach (var grandChild in ControlsRecursive(child))
                    yield return grandChild;
            }
        }

        private void ChangeDifficulty()
        {
            _maxRange = _difficultyBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 500, _ => 100 };
            StartNewGame();
        }

        private void LanguageBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing) return;

            var culture = _languageBox.SelectedItem?.ToString() switch
            {
                "Español" => "es",
                "Русский" => "ru",
                _ => "en"
            };

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            Controls.Clear();
            _isInitializing = true;
            InitializeComponent();
            _isInitializing = false;
            StartNewGame();
        }

        private int GetLanguageIndex() =>
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName switch
            {
                "es" => 1,
                "ru" => 2,
                _ => 0
            };

        private void LoadScores()
        {
            _leaderboardGrid.Rows.Clear();
            if (File.Exists(BestScoreFile) && int.TryParse(File.ReadAllText(BestScoreFile).Trim(), out int best))
                _bestScore = best;

            _bestScoreLabel.Text = Strings.BestScore + $": {_bestScore}";

            if (!File.Exists(LeaderboardFile)) return;

            var entries = File.ReadAllLines(LeaderboardFile)
                .Select(line => line.Split(','))
                .Where(parts => parts.Length == 3 && int.TryParse(parts[1], out _) && int.TryParse(parts[2], out _))
                .Select(parts => new ScoreEntry(parts[0], int.Parse(parts[1]), int.Parse(parts[2])))
                .OrderBy(e => e.Attempts).ThenBy(e => e.Time).Take(10);

            foreach (var e in entries)
                _leaderboardGrid.Rows.Add(e.Name, e.Attempts, e.Time);
        }

        private void RecordScore(int time)
        {
            if (_attempts < _bestScore)
            {
                _bestScore = _attempts;
                File.WriteAllText(BestScoreFile, _bestScore.ToString());
            }

            string name = PromptForName();
            File.AppendAllText(LeaderboardFile, $"{name},{_attempts},{time}{Environment.NewLine}");
        }

        private string PromptForName()
        {
            using var form = new Form
            {
                Text = Strings.WindowTitle,
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(20)
            };

            var label = new Label
            {
                Text = "Enter your name for the leaderboard:",
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Location = new Point(30, 25)
            };

            var box = new TextBox
            {
                Location = new Point(30, 60),
                Width = 320,
                Height = 30,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create button container panel for centered alignment
            var buttonPanel = new TableLayoutPanel
            {
                Width = 320,
                Height = 45,
                Location = new Point(30, 100),
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            // Set up column styles for proper spacing and centering
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // Left spacing
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Cancel button
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Save button
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // Right spacing

            var ok = new Button
            {
                Text = "Save Score",
                DialogResult = DialogResult.OK,
                Width = 110,
                Height = 35,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            ok.FlatAppearance.BorderSize = 0;

            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 110,
                Height = 35,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            cancel.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.Add(cancel, 1, 0);
            buttonPanel.Controls.Add(ok, 2, 0);

            // Add hover effects
            ok.MouseEnter += (s, e) => ok.BackColor = Color.FromArgb(0, 100, 200);
            ok.MouseLeave += (s, e) => ok.BackColor = Color.FromArgb(0, 120, 215);
            cancel.MouseEnter += (s, e) => cancel.BackColor = Color.FromArgb(220, 220, 220);
            cancel.MouseLeave += (s, e) => cancel.BackColor = Color.LightGray;

            form.Controls.AddRange(new Control[] { label, box, buttonPanel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            // Focus the text box when the form opens
            form.Shown += (s, e) => box.Focus();

            return form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(box.Text)
                ? box.Text.Trim()
                : "Anonymous";
        }
    }

    public class ScoreEntry
    {
        public string Name { get; }
        public int Attempts { get; }
        public int Time { get; }
        public ScoreEntry(string name, int attempts, int time) => (Name, Attempts, Time) = (name, attempts, time);
    }
}
