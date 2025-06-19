using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    public static class GameSettings
    {
        public static readonly Color DefaultBackColor = SystemColors.Control;
        public static readonly Color DefaultForeColor = SystemColors.ControlText;
        public static readonly string SoundsDirectory = "Sounds";
        public static readonly string WinSoundPath = Path.Combine(SoundsDirectory, "win.wav");
        public static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    public class ScoreEntry
    {
        public required string Name { get; set; }
        public int Attempts { get; set; }
        public int Time { get; set; }
    }

    public partial class MainForm : Form
    {
        private readonly string _scoresPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.json");
        private readonly Random _random = new();
        private readonly Stopwatch _stopwatch = new();
        private readonly SoundPlayer _winPlayer = new(GameSettings.WinSoundPath);
        private readonly List<ScoreEntry> _scores = new();
        private bool _isInitializing;
        private int _maxRange = 100;
        private int _target;
        private int _attempts;

        private readonly Label _promptLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12) };
        private readonly Label _resultLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12) };
        private readonly Label _attemptsLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12) };
        private readonly Label _timerLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12) };
        private readonly Label _leaderboardLabel = new() { Text = "Leaderboard", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label _languageLabel = new() { Text = "Language", Font = new Font("Segoe UI", 10) };
        private readonly Label _difficultyLabel = new() { Text = "Difficulty", Font = new Font("Segoe UI", 10) };
        private readonly Label _themeLabel = new() { Text = "Theme", Font = new Font("Segoe UI", 10) };

        private readonly TextBox _inputBox = new()
        {
            Width = 100,
            Font = new Font("Segoe UI", 12),
            BackColor = Color.White,
            ForeColor = Color.Black,
            TextAlign = HorizontalAlignment.Center,
            MaxLength = 4,
            BorderStyle = BorderStyle.FixedSingle
        };
        private readonly Button _guessButton = new() { Text = "Guess", Width = 120, Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(120, 35), Padding = new Padding(10, 5, 10, 5) };
        private readonly ColorProgressBar _progressBar = new()
        {
            Height = 35,
            Margin = new Padding(20, 10, 20, 10),
            MinimumSize = new Size(0, 35),
            Maximum = 100,
            Value = 0,
            Dock = DockStyle.Fill
        };

        private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly ComboBox _difficultyBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly ComboBox _themeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };

        private readonly DataGridView _leaderboardGrid = new()
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };

        private static readonly string[] _loseSounds = Directory.Exists(GameSettings.SoundsDirectory) ? 
            Directory.GetFiles(GameSettings.SoundsDirectory, "lose*.wav") : Array.Empty<string>();

        public MainForm()
        {
            InitializeComponent();
            _isInitializing = true;

            _promptLabel.Text = "Enter a number between 1 and 100:";
            _resultLabel.Text = "";
            _attemptsLabel.Text = "Attempts: 0";
            _timerLabel.Text = "Time: 0s";
            // Initialize combo boxes
            _languageBox.BeginUpdate();
            _difficultyBox.BeginUpdate();
            _themeBox.BeginUpdate();

            _languageBox.Items.AddRange(new[] { "English", "Español", "Русский" });
            _difficultyBox.Items.AddRange(new[] { "Easy (1-50)", "Medium (1-100)", "Hard (1-500)" });
            _themeBox.Items.AddRange(new[] { "Light", "Dark" });
            _themeBox.SelectedIndex = 0;
            _difficultyBox.SelectedIndex = 0;
            _themeBox.SelectedIndex = 0;

            _languageBox.EndUpdate();
            _difficultyBox.EndUpdate();
            _themeBox.EndUpdate();

            _difficultyBox.SelectedIndex = 1;
            _languageBox.SelectedIndex = 0;
            _themeBox.SelectedIndex = 0;

            _languageBox.SelectedIndexChanged += LanguageBox_SelectedIndexChanged;
            _difficultyBox.SelectedIndexChanged += DifficultyBox_SelectedIndexChanged;
            _themeBox.SelectedIndexChanged += (s, e) =>
            {
                if (_isInitializing) return;
                ToggleTheme();
            };

            _inputBox.TextAlign = HorizontalAlignment.Center;
            _inputBox.MaxLength = 3;
            _inputBox.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                    e.Handled = true;

                if (e.KeyChar == (char)Keys.Enter && _guessButton.Enabled)
                {
                    e.Handled = true;
                    _ = OnGuessAsync(s, e);
                }
            };
            _inputBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    e.Handled = true;
            };

            _guessButton.BackColor = Color.FromArgb(0, 120, 215);
            _guessButton.ForeColor = Color.White;
            _guessButton.FlatStyle = FlatStyle.Flat;
            _guessButton.FlatAppearance.BorderSize = 0;
            _guessButton.Cursor = Cursors.Hand;
            _guessButton.Click += async (s, e) => await OnGuessAsync(s, e);
            _guessButton.Dock = DockStyle.Fill;

            AcceptButton = _guessButton;

            _leaderboardGrid.Columns.Add("Name", "Player");
            _leaderboardGrid.Columns.Add("Attempts", "Attempts");
            _leaderboardGrid.Columns.Add("Time", "Time (s)");
            _leaderboardGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _leaderboardGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _leaderboardGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _leaderboardGrid.ReadOnly = true;
            _leaderboardGrid.AllowUserToAddRows = false;
            _leaderboardGrid.AllowUserToDeleteRows = false;
            _leaderboardGrid.AllowUserToResizeRows = false;
            _leaderboardGrid.RowHeadersVisible = false;
            _leaderboardGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _leaderboardGrid.EnableHeadersVisualStyles = false;
            UpdateLeaderboard(); // Set initial colors

            _isInitializing = false;
            LoadSettings();
            LoadScoresAsync().ConfigureAwait(false);
            StartNewGameAsync().ConfigureAwait(false);
        }

        private void InitializeComponent()
        {
            Text = " Guess Game";
            ClientSize = new Size(800, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = GameSettings.DefaultBackColor;

            // Top bar: language, difficulty, theme
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240),
                ColumnCount = 3,
                RowCount = 2
            };

            // Equal width columns
            for (int i = 0; i < 3; i++)
            {
                topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            }

            // Row styles for labels and combo boxes
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Labels
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Combo boxes

            // Add labels
            var labelStyle = new Action<Label>(label => {
                label.Font = new Font("Segoe UI", 10);
                label.ForeColor = Color.FromArgb(64, 64, 64);
                label.TextAlign = ContentAlignment.BottomCenter;
                label.Dock = DockStyle.Fill;
            });

            var languageLabel = new Label { Text = "Language" };
            var difficultyLabel = new Label { Text = "Difficulty" };
            var themeLabel = new Label { Text = "Theme" };

            labelStyle(_languageLabel);
            labelStyle(_difficultyLabel);
            labelStyle(_themeLabel);

            topPanel.Controls.Add(_languageLabel, 0, 0);
            topPanel.Controls.Add(_difficultyLabel, 1, 0);
            topPanel.Controls.Add(_themeLabel, 2, 0);

            void SetupComboBox(ComboBox box)
            {
                box.Width = 150;
                box.Dock = DockStyle.Fill;
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.Font = new Font("Segoe UI", 11);
                box.Cursor = Cursors.Hand;
                box.BackColor = SystemColors.Window;
                box.FlatStyle = FlatStyle.Flat;
                box.ForeColor = SystemColors.WindowText;
            }
            
            SetupComboBox(_languageBox);
            SetupComboBox(_difficultyBox);
            SetupComboBox(_themeBox);
            
            topPanel.Controls.Add(_languageBox, 0, 1);
            topPanel.Controls.Add(_difficultyBox, 1, 1);
            topPanel.Controls.Add(_themeBox, 2, 1);

            // Prompt
            _promptLabel.Text = "Enter your guess:";
            _promptLabel.Dock = DockStyle.Top;
            _promptLabel.Height = 60;
            _promptLabel.Padding = new Padding(0, 10, 0, 10);
            _promptLabel.TextAlign = ContentAlignment.MiddleCenter;

            // Input + Button centered below prompt
            _inputBox.TextAlign = HorizontalAlignment.Center;
            _inputBox.Font = new Font("Segoe UI", 16);
            _inputBox.Width = 150;
            _inputBox.Height = 35;
            _inputBox.BorderStyle = BorderStyle.FixedSingle;
            _inputBox.BackColor = Color.White;
            _inputBox.Margin = new Padding(10);
            _inputBox.Enter += (s, e) => _inputBox.BackColor = Color.FromArgb(240, 248, 255);  // Light blue when focused
            _inputBox.Leave += (s, e) => _inputBox.BackColor = Color.White;
            _inputBox.TextChanged += (_, _) => _guessButton.Enabled = !string.IsNullOrWhiteSpace(_inputBox.Text);
            _inputBox.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                    e.Handled = true;

                if (e.KeyChar == (char)Keys.Enter && _guessButton.Enabled)
                {
                    e.Handled = true;
                    _ = OnGuessAsync(s, e);
                }
            };
            _inputBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    e.Handled = true;
            };

            var gamePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = true
            };

            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var inputPanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10)
            };
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            inputPanel.Controls.Add(_inputBox, 0, 0);
            inputPanel.Controls.Add(_guessButton, 1, 0);

            // Progress bar panel for fixed height
            var progressPanel = new Panel
            {
                Height = 35,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 10)
            };
            progressPanel.Controls.Add(_progressBar);

            gamePanel.Controls.Add(_promptLabel);
            gamePanel.Controls.Add(inputPanel);
            gamePanel.Controls.Add(_resultLabel);
            gamePanel.Controls.Add(progressPanel);
            gamePanel.Controls.Add(_attemptsLabel);
            gamePanel.Controls.Add(_timerLabel);

            // Leaderboard
            var leaderboardPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 300,
                Padding = new Padding(10),
                ColumnCount = 1,
                RowCount = 2
            };

            leaderboardPanel.Controls.Add(_leaderboardLabel, 0, 0);
            leaderboardPanel.Controls.Add(_leaderboardGrid, 0, 1);

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.SetColumnSpan(topPanel, 2);
            mainLayout.Controls.Add(gamePanel, 0, 1);
            mainLayout.Controls.Add(leaderboardPanel, 1, 1);

            Controls.Add(mainLayout);

            Text = "Guess Game";
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            ResumeLayout(false);
        }

        private static ComboBox CreateComboBox(string[] items, int selectedIndex, EventHandler handler)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12)
            };
            combo.Items.AddRange(items);
            combo.SelectedIndex = selectedIndex;
            combo.SelectedIndexChanged += handler;
            return combo;
        }

        private static Label CreateLabel(string text, int fontSize = 12)
        {
            var label = new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", fontSize),
                Padding = new Padding(5),
                AutoSize = false,
                Height = 30
            };
            return label;
        }

        private async Task StartNewGameAsync()
        {
            _target = _random.Next(1, _maxRange + 1);
            _attempts = 0;
            _resultLabel.Text = "";
            _progressBar.Value = 0;
            _progressBar.ProgressColor = Color.FromArgb(0, 120, 215); // Reset to default blue
            _inputBox.Clear();
            _inputBox.Focus();
            _stopwatch.Restart();
            _guessButton.Enabled = true;
            _inputBox.Enabled = true;
            
            // Update labels with current language
            switch (_languageBox.SelectedIndex)
            {
                case 1: // Spanish
                    _promptLabel.Text = $"Adivina un número entre 1 y {_maxRange}:";
                    _attemptsLabel.Text = $"Intentos: {_attempts}";
                    break;
                case 2: // Russian
                    _promptLabel.Text = $"Угадайте число от 1 до {_maxRange}:";
                    _attemptsLabel.Text = $"Попыток: {_attempts}";
                    break;
                default: // English
                    _promptLabel.Text = $"Guess a number between 1 and {_maxRange}:";
                    _attemptsLabel.Text = $"Attempts: {_attempts}";
                    break;
            }
        }

        private void SaveSettings()
        {
            var settings = new { Theme = _themeBox.SelectedIndex, Language = _languageBox.SelectedIndex };
            try
            {
                File.WriteAllText(GameSettings.SettingsPath, JsonSerializer.Serialize(settings));
            }
            catch
            {
                // Ignore save errors
            }
        }

        private async void LoadSettings()
        {
            try
            {
                if (File.Exists(GameSettings.SettingsPath))
                {
                    var settings = JsonSerializer.Deserialize<dynamic>(File.ReadAllText(GameSettings.SettingsPath));
                    if (settings != null)
                    {
                        _isInitializing = true; // Prevent theme toggle during load
                        _themeBox.SelectedIndex = settings.GetProperty("Theme").GetInt32();
                        _isInitializing = false;
                        ToggleTheme(); // Apply theme immediately
                        await LoadScoresAsync(); // Reload scores after theme change
                    }
                }
            }
            catch
            {
                // Use default settings on load error
            }
        }

        private async Task LoadScoresAsync()
        {
            if (File.Exists(_scoresPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_scoresPath);
                    _scores.Clear();
                    _scores.AddRange(JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? new List<ScoreEntry>());
                    UpdateLeaderboard();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading scores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task SaveScoresAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_scoresPath, json);
                await LoadScoresAsync(); // Reload to refresh the grid
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving scores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateLeaderboard()
        {
            // Set grid colors based on theme
            var isDark = _themeBox.SelectedIndex == 1;
            var bgColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            var fgColor = isDark ? Color.White : Color.Black;
            
            _leaderboardGrid.BackgroundColor = bgColor;
            _leaderboardGrid.DefaultCellStyle.BackColor = bgColor;
            _leaderboardGrid.DefaultCellStyle.ForeColor = fgColor;
            _leaderboardGrid.ColumnHeadersDefaultCellStyle.BackColor = bgColor;
            _leaderboardGrid.ColumnHeadersDefaultCellStyle.ForeColor = fgColor;
            _leaderboardGrid.EnableHeadersVisualStyles = false;

            // Set selection colors for better visibility in dark mode
            _leaderboardGrid.DefaultCellStyle.SelectionBackColor = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Highlight;
            _leaderboardGrid.DefaultCellStyle.SelectionForeColor = fgColor;

            _leaderboardGrid.Rows.Clear();
            foreach (var score in _scores.OrderBy(s => s.Attempts).ThenBy(s => s.Time).Take(10))
            {
                var attemptsText = _languageBox.SelectedIndex switch
                {
                    1 => $"{score.Attempts} intentos",
                    2 => $"{score.Attempts} попыток",
                    _ => $"{score.Attempts} attempts"
                };
                
                var timeText = _languageBox.SelectedIndex switch
                {
                    1 => $"{score.Time}s",
                    2 => $"{score.Time}с",
                    _ => $"{score.Time}s"
                };
                
                _leaderboardGrid.Rows.Add(score.Name, attemptsText, timeText);
            }
        }

        private IEnumerable<Control> ControlsRecursive(Control? parent = null)
        {
            var controls = (parent?.Controls ?? Controls).Cast<Control>();
            return controls.SelectMany(c => ControlsRecursive(c)).Concat(controls);
        }

        private async Task OnGuessAsync(object? sender, EventArgs e)
        {
            if (!int.TryParse(_inputBox.Text, out int guess))
            {
                MessageBox.Show($"Please enter a valid number between 1 and {_maxRange}.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _inputBox.SelectAll();
                _inputBox.Focus();
                return;
            }

            if (guess < 1 || guess > _maxRange)
            {
                MessageBox.Show($"Please enter a number between 1 and {_maxRange}.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _inputBox.SelectAll();
                _inputBox.Focus();
                return;
            }

            _attempts++;
            _attemptsLabel.Text = $"Attempts: {_attempts}";
            
            // Calculate how close we are to the target using a non-linear scale
            int distance = Math.Abs(guess - _target);
            
            // Use different scaling based on difficulty level
            double maxError = _maxRange / 2.0; // Max reasonable error (half the range)
            double normalizedDistance = Math.Min(distance, maxError) / maxError;
            
            // Apply non-linear scaling to make colors more dramatic
            double proximity = Math.Pow(1.0 - normalizedDistance, 2);
            int percentage = (int)(proximity * 100);

            // Calculate color based on proximity
            Color progressColor;
            if (proximity < 0.2) // Very far (bright red)
            {
                progressColor = Color.FromArgb(255, 0, 0);
            }
            else if (proximity < 0.4) // Far (orange-red)
            {
                progressColor = Color.FromArgb(255, 69, 0);
            }
            else if (proximity < 0.6) // Medium (orange)
            {
                progressColor = Color.FromArgb(255, 140, 0);
            }
            else if (proximity < 0.8) // Getting closer (yellow)
            {
                progressColor = Color.FromArgb(255, 215, 0);
            }
            else if (proximity < 0.95) // Close (yellow-green)
            {
                progressColor = Color.FromArgb(154, 205, 50);
            }
            else // Very close (bright green)
            {
                progressColor = Color.FromArgb(0, 255, 0);
            }
            _progressBar.ProgressColor = progressColor;

            // Smoothly animate to the new value with easing
            var currentValue = _progressBar.Value;
            var totalSteps = 15;
            var stepDelay = 15; // milliseconds
            
            for (int i = 0; i < totalSteps; i++)
            {
                // Use easing function for smoother animation
                var t = (i + 1.0) / totalSteps;
                var easedT = 1 - Math.Pow(1 - t, 3); // Ease out cubic
                var newValue = currentValue + (percentage - currentValue) * easedT;
                _progressBar.Value = (int)Math.Round(newValue);
                await Task.Delay(stepDelay);
            }
            _progressBar.Value = percentage; // Ensure we end exactly at target

            if (guess == _target)
            {
                _stopwatch.Stop();
                try { _winPlayer.Play(); } catch { /* Ignore sound errors */ }
{
    try
    {
        var json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_scoresPath, json);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error saving scores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
                int time = (int)_stopwatch.Elapsed.TotalSeconds;
                await RecordScoreAsync(time);
                await SaveScoresAsync(); // Save and refresh leaderboard
                MessageBox.Show($"You guessed it!\nAttempts: {_attempts}\nTime: {time}s", "Congratulations!");
                await StartNewGameAsync();
            }
            else
            {
                _resultLabel.Text = guess < _target ? "Too low!" : "Too high!";
                _resultLabel.ForeColor = Color.Red;
                
                // Play a random lose sound
                try
                {
                    if (_loseSounds.Length > 0)
                    {
                        var randomLoseSound = _loseSounds[_random.Next(_loseSounds.Length)];
                        using var player = new SoundPlayer(randomLoseSound);
                        player.Play();
                    }
                }
                catch
                {
                    // Ignore sound errors
                }
            }

            _inputBox.Clear();
            _inputBox.Focus();
            _timerLabel.Text = $"Time: {(int)_stopwatch.Elapsed.TotalSeconds}s";
        }

        private async Task RecordScoreAsync(int time)
        {
            var name = PromptForName();
            if (string.IsNullOrWhiteSpace(name)) return;

            var score = new ScoreEntry { Name = name, Attempts = _attempts, Time = time };
            _scores.Add(score);
            _scores.Sort((a, b) => a.Attempts == b.Attempts ? 
                a.Time.CompareTo(b.Time) : 
                a.Attempts.CompareTo(b.Attempts));

            try
            {
                var json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_scoresPath, json);
                UpdateLeaderboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving score: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleTheme()
        {
            var isDark = _themeBox.SelectedIndex == 1;
            SaveSettings();
            
            // Update leaderboard colors for the new theme
            UpdateLeaderboard();

            if (isDark)
            {
                BackColor = Color.FromArgb(30, 30, 30);
                ForeColor = Color.White;
                
                // Special handling for input box in dark mode
                _inputBox.BackColor = Color.White;
                _inputBox.ForeColor = Color.Black;
                
                // Special handling for combo boxes in dark mode
                void SetDarkComboBox(ComboBox box)
                {
                    box.BackColor = Color.White;
                    box.ForeColor = Color.Black;
                }
                
                SetDarkComboBox(_languageBox);
                SetDarkComboBox(_difficultyBox);
                SetDarkComboBox(_themeBox);
                
                // Special handling for progress bar in dark mode
                _progressBar.BackColor = Color.FromArgb(60, 60, 60);
                
                foreach (var control in ControlsRecursive())
                {
                    if (control == _inputBox || control == _progressBar ||
                        control == _languageBox || control == _difficultyBox || control == _themeBox) continue;
                    control.BackColor = Color.FromArgb(30, 30, 30);
                    control.ForeColor = Color.White;
                }
            }
            else
            {
                BackColor = GameSettings.DefaultBackColor;
                ForeColor = GameSettings.DefaultForeColor;
                
                // Reset input box colors
                _inputBox.BackColor = Color.White;
                _inputBox.ForeColor = Color.Black;
                
                // Reset combo box colors
                void SetLightComboBox(ComboBox box)
                {
                    box.BackColor = Color.White;
                    box.ForeColor = Color.Black;
                }
                
                SetLightComboBox(_languageBox);
                SetLightComboBox(_difficultyBox);
                SetLightComboBox(_themeBox);
                
                // Reset progress bar colors
                _progressBar.BackColor = Color.White;
                
                foreach (var control in ControlsRecursive())
                {
                    if (control == _inputBox || control == _progressBar ||
                        control == _languageBox || control == _difficultyBox || control == _themeBox) continue;
                    control.BackColor = GameSettings.DefaultBackColor;
                    control.ForeColor = GameSettings.DefaultForeColor;
                }
            }
        }

        private async void DifficultyBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing) return;

            _maxRange = _difficultyBox.SelectedIndex switch
            {
                0 => 50,    // Easy (1-50)
                1 => 100,   // Medium (1-100)
                2 => 500,   // Hard (1-500)
                _ => 100
            };
            await StartNewGameAsync();
        }

        private void LanguageBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing) return;
            
            // Store current selections before changing language
            var currentDifficulty = _difficultyBox.SelectedIndex;
            var currentTheme = _themeBox.SelectedIndex;

            // Preserve combo box styling
            void PreserveComboBoxStyle(ComboBox box)
            {
                box.BackColor = Color.White;
                box.ForeColor = Color.Black;
                box.FlatStyle = FlatStyle.Flat;
                box.Font = new Font("Segoe UI", 11);
                box.Cursor = Cursors.Hand;
            }
            
            switch (_languageBox.SelectedIndex)
            {
                case 0: // English
                    _promptLabel.Text = $"Guess a number between 1 and {_maxRange}:";
                    _guessButton.Text = "Guess";
                    _attemptsLabel.Text = $"Attempts: {_attempts}";
                    _timerLabel.Text = $"Time: {(int)_stopwatch.Elapsed.TotalSeconds}s";
                    _leaderboardLabel.Text = "Leaderboard";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[0]).HeaderText = "Player";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[1]).HeaderText = "Attempts";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[2]).HeaderText = "Time (s)";
                    _languageLabel.Text = "Language";
                    _difficultyLabel.Text = "Difficulty";
                    _themeLabel.Text = "Theme";
                    _difficultyBox.Items.Clear();
                    _difficultyBox.Items.AddRange(new[] { "Easy (1-50)", "Medium (1-100)", "Hard (1-500)" });
                    _difficultyBox.SelectedIndex = currentDifficulty;
                    _themeBox.Items.Clear();
                    _themeBox.Items.AddRange(new[] { "Light", "Dark" });
                    _themeBox.SelectedIndex = currentTheme;
                    
                    // Preserve styles
                    _guessButton.BackColor = Color.FromArgb(0, 120, 215);
                    _guessButton.ForeColor = Color.White;
                    _guessButton.FlatStyle = FlatStyle.Flat;
                    _guessButton.FlatAppearance.BorderSize = 0;
                    PreserveComboBoxStyle(_languageBox);
                    PreserveComboBoxStyle(_difficultyBox);
                    PreserveComboBoxStyle(_themeBox);
                    break;
                    
                case 1: // Spanish
                    _promptLabel.Text = $"Adivina un número entre 1 y {_maxRange}:";
                    _guessButton.Text = "Adivinar";
                    _attemptsLabel.Text = $"Intentos: {_attempts}";
                    _timerLabel.Text = $"Tiempo: {(int)_stopwatch.Elapsed.TotalSeconds}s";
                    _leaderboardLabel.Text = "Tabla de Posiciones";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[0]).HeaderText = "Jugador";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[1]).HeaderText = "Intentos";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[2]).HeaderText = "Tiempo (s)";
                    _languageLabel.Text = "Idioma";
                    _difficultyLabel.Text = "Dificultad";
                    _themeLabel.Text = "Tema";
                    _difficultyBox.Items.Clear();
                    _difficultyBox.Items.AddRange(new[] { "Fácil (1-100)", "Medio (1-500)", "Difícil (1-1000)" });
                    _difficultyBox.SelectedIndex = currentDifficulty;
                    _themeBox.Items.Clear();
                    _themeBox.Items.AddRange(new[] { "Claro", "Oscuro" });
                    _themeBox.SelectedIndex = currentTheme;
                    
                    // Preserve styles
                    _guessButton.BackColor = Color.FromArgb(0, 120, 215);
                    _guessButton.ForeColor = Color.White;
                    _guessButton.FlatStyle = FlatStyle.Flat;
                    _guessButton.FlatAppearance.BorderSize = 0;
                    PreserveComboBoxStyle(_languageBox);
                    PreserveComboBoxStyle(_difficultyBox);
                    PreserveComboBoxStyle(_themeBox);
                    break;
                    
                case 2: // Russian
                    _promptLabel.Text = $"Угадайте число от 1 до {_maxRange}:";
                    _guessButton.Text = "Угадать";
                    _attemptsLabel.Text = $"Попыток: {_attempts}";
                    _timerLabel.Text = $"Время: {(int)_stopwatch.Elapsed.TotalSeconds}с";
                    _leaderboardLabel.Text = "Таблица лидеров";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[0]).HeaderText = "Игрок";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[1]).HeaderText = "Попыток";
                    ((DataGridViewTextBoxColumn)_leaderboardGrid.Columns[2]).HeaderText = "Время (с)";
                    _languageLabel.Text = "Язык";
                    _difficultyLabel.Text = "Сложность";
                    _themeLabel.Text = "Тема";
                    _difficultyBox.Items.Clear();
                    _difficultyBox.Items.AddRange(new[] { "Легкий (1-100)", "Средний (1-500)", "Сложный (1-1000)" });
                    _difficultyBox.SelectedIndex = currentDifficulty;
                    _themeBox.Items.Clear();
                    _themeBox.Items.AddRange(new[] { "Светлая", "Темная" });
                    _themeBox.SelectedIndex = currentTheme;
                    
                    // Preserve styles
                    _guessButton.BackColor = Color.FromArgb(0, 120, 215);
                    _guessButton.ForeColor = Color.White;
                    _guessButton.FlatStyle = FlatStyle.Flat;
                    _guessButton.FlatAppearance.BorderSize = 0;
                    PreserveComboBoxStyle(_languageBox);
                    PreserveComboBoxStyle(_difficultyBox);
                    PreserveComboBoxStyle(_themeBox);
                    break;
            }
            StartNewGameAsync().ConfigureAwait(false);
        }

        private int GetLanguageIndex()
        {
            return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName switch
            {
                "es" => 1,
                "ru" => 2,
                _ => 0
            };
        }

        private string PromptForName()
        {
            var form = new Form
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Enter your name",
                StartPosition = FormStartPosition.CenterParent,
                Padding = new Padding(20)
            };

            var promptLabel = new Label
            {
                Text = "Enter your name to save your score:",
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var box = new TextBox
            {
                Width = 340,
                Height = 30,
                Location = new Point(20, 50),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };

            var buttonPanel = new TableLayoutPanel
            {
                Width = 340,
                Height = 45,
                Location = new Point(20, 100),
                ColumnCount = 3,
                RowCount = 1
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20)); // Spacing
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var ok = new Button
            {
                Text = "Save Score",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            ok.FlatAppearance.BorderSize = 0;

            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new Font("Segoe UI", 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            cancel.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.Add(ok, 0, 0);
            buttonPanel.Controls.Add(cancel, 2, 0);

            form.Controls.Add(promptLabel);
            form.Controls.Add(box);
            form.Controls.Add(buttonPanel);

            form.AcceptButton = ok;
            form.CancelButton = cancel;

            form.Shown += (s, e) => box.Focus();

            if (form.ShowDialog() == DialogResult.OK)
                return box.Text.Trim();
            return "Anonymous";
        }
    }
}
