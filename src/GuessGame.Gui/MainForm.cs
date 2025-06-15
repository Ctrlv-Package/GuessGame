using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Media;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GuessGame.Gui.Properties;

namespace GuessGame.Gui
{
    public partial class MainForm : Form
    {
        private Label _promptLabel, _resultLabel, _attemptsLabel, _timerLabel, _bestScoreLabel;
        private ListBox _leaderboardBox;
        private ProgressBar _progressBar;
        private TextBox _inputBox;
        private Button _guessButton;
        private ComboBox _difficultyBox, _languageBox;
        private Random _random = new Random();
        private string[] _successMessages => Strings.SuccessMessages.Split(';');
        private int _target, _attempts, _bestScore = int.MaxValue, _maxRange = 100;
        private Stopwatch _stopwatch = new Stopwatch();
        private Color _defaultBackColor;
        private const string BestScoreFile = "bestscore.txt";
        private const string LeaderboardFile = "leaderboard.txt";
        private SoundPlayer _winPlayer = new SoundPlayer(Path.Combine("Sounds", "win.wav"));
        private SoundPlayer _losePlayer = new SoundPlayer(Path.Combine("Sounds", "lose.wav"));

        private bool _isInitializing = true;

        public MainForm()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;

            try
            {
                InitializeComponent();
                Load += (_, _) => StartNewGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during form initialization:\n" + ex, "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            try
            {
                Text = "🎯 " + Strings.WindowTitle;
                Icon = SystemIcons.Information;
                ClientSize = new Size(700, 500);
                StartPosition = FormStartPosition.CenterScreen;
                Font = new Font(Font.FontFamily, 14);
                _defaultBackColor = BackColor;

                _promptLabel = new Label { Text = Strings.GuessPrompt, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };

                _difficultyBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = new Font(Font.FontFamily, 14), Height = 40 };
                _difficultyBox.Items.AddRange(new[] { Strings.Easy, Strings.Medium, Strings.Hard });
                _difficultyBox.SelectedIndex = 1;
                _difficultyBox.SelectedIndexChanged += (_, _) => ChangeDifficulty();

                _languageBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = new Font(Font.FontFamily, 14), Height = 40 };
                _languageBox.Items.AddRange(new[] { "English", "Español", "Русский" });
                _languageBox.SelectedIndexChanged += LanguageBox_SelectedIndexChanged;
                _languageBox.SelectedIndex = GetLanguageIndex();

                _inputBox = new TextBox { Font = new Font(Font.FontFamily, 14), Margin = new Padding(0, 5, 10, 5), MinimumSize = new Size(100, 35) };

                _guessButton = new Button
                {
                    Text = Strings.Guess,
                    Width = 120,
                    Height = 55,
                    BackColor = Color.MediumSlateBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseCompatibleTextRendering = true
                };
                _guessButton.Click += OnGuess;
                AcceptButton = _guessButton;

                _resultLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                _attemptsLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                _timerLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                _bestScoreLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                _leaderboardBox = new ListBox { Dock = DockStyle.Fill, Height = 80 };
                _progressBar = new ProgressBar { Dock = DockStyle.Fill, Maximum = 100 };

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 10 };
                for (int i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100 / 10F));

                var langPanel = new TableLayoutPanel { Anchor = AnchorStyles.None, AutoSize = true };
                langPanel.Controls.Add(_languageBox);
                layout.Controls.Add(langPanel, 0, 0);

                layout.Controls.Add(_promptLabel, 0, 1);

                var difficultyPanel = new TableLayoutPanel { Anchor = AnchorStyles.None, AutoSize = true };
                difficultyPanel.Controls.Add(_difficultyBox);
                layout.Controls.Add(difficultyPanel, 0, 2);

                var inputPanel = new TableLayoutPanel
                {
                    Anchor = AnchorStyles.None,
                    Size = new Size(300, 60),
                    ColumnCount = 2,
                    RowCount = 1,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };

                inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                var inputBoxPanel = new Panel { Height = 55, Width = 140 };
                inputBoxPanel.Controls.Add(_inputBox);
                _inputBox.Dock = DockStyle.Fill;

                var guessButtonPanel = new Panel { Height = 55, Width = 140 };
                guessButtonPanel.Controls.Add(_guessButton);
                _guessButton.Dock = DockStyle.Fill;

                inputPanel.Controls.Add(inputBoxPanel, 0, 0);
                inputPanel.Controls.Add(guessButtonPanel, 1, 0);

                layout.Controls.Add(inputPanel, 0, 3);

                layout.Controls.Add(_resultLabel, 0, 4);
                layout.Controls.Add(_progressBar, 0, 5);
                layout.Controls.Add(_attemptsLabel, 0, 6);
                layout.Controls.Add(_timerLabel, 0, 7);
                layout.Controls.Add(_bestScoreLabel, 0, 8);
                layout.Controls.Add(_leaderboardBox, 0, 9);

                Controls.Add(layout);

                _isInitializing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initialization failed: " + ex.Message);
                throw;
            }
        }

        private void LanguageBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing) return;
            SwitchLanguage();
        }

        private void ChangeDifficulty()
        {
            _maxRange = _difficultyBox.SelectedIndex switch { 0 => 50, 1 => 100, 2 => 500, _ => 100 };
            StartNewGame();
        }

        private void SwitchLanguage()
        {
            try
            {
                var selected = _languageBox.SelectedItem?.ToString();
                var culture = selected switch
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
            catch (Exception ex)
            {
                MessageBox.Show("Language switch failed: " + ex.Message);
            }
        }

        private int GetLanguageIndex()
        {
            var lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            return lang switch
            {
                "es" => 1,
                "ru" => 2,
                _ => 0
            };
        }

        private void StartNewGame()
        {
            _target = _random.Next(1, _maxRange + 1);
            _attempts = 0;
            _resultLabel.Text = string.Empty;
            _inputBox.Text = string.Empty;
            _attemptsLabel.Text = Strings.Attempts + ": 0";
            _timerLabel.Text = Strings.Time + ": 0s";
            LoadScores();
            BackColor = _defaultBackColor;
            _progressBar.Value = 0;
            _stopwatch.Restart();
            _inputBox.Focus();
        }

        private void LoadScores()
        {
            try
            {
                if (File.Exists(BestScoreFile))
                {
                    var text = File.ReadAllText(BestScoreFile).Trim();
                    if (int.TryParse(text, out var best)) _bestScore = best;
                }
                _bestScoreLabel.Text = Strings.BestScore + $": {_bestScore}";

                _leaderboardBox.Items.Clear();
                if (File.Exists(LeaderboardFile))
                {
                    foreach (var line in File.ReadAllLines(LeaderboardFile))
                        _leaderboardBox.Items.Add(line);
                }
                if (_leaderboardBox.Items.Count == 0)
                    _leaderboardBox.Items.Add(Strings.NoScores);
            }
            catch { /* ignore */ }
        }

        private void RecordScore(int time)
        {
            try
            {
                if (_attempts < _bestScore)
                {
                    _bestScore = _attempts;
                    File.WriteAllText(BestScoreFile, _bestScore.ToString());
                }

                var name = Environment.UserName;
                var entry = $"{name},{_attempts},{time}";
                File.AppendAllText(LeaderboardFile, entry + Environment.NewLine);
            }
            catch { /* ignore */ }
        }

        private void OnGuess(object? sender, EventArgs e)
        {
            string input = _inputBox.Text;
            _inputBox.Clear();

            if (int.TryParse(input, out int guess))
            {
                _attempts++;
                _attemptsLabel.Text = Strings.Attempts + $": {_attempts}";

                int distance = Math.Abs(guess - _target);
                if (distance == 0) BackColor = Color.LightGreen;
                else if (distance <= 5) BackColor = Color.LightGoldenrodYellow;
                else if (distance <= 10) BackColor = Color.Khaki;
                else BackColor = Color.LightCoral;

                _progressBar.Value = Math.Min(100, 100 - (distance * 100 / _maxRange));

                if (guess == _target)
                {
                    _stopwatch.Stop();
                    _winPlayer.Play();
                    var time = (int)_stopwatch.Elapsed.TotalSeconds;
                    var msg = _successMessages[_random.Next(_successMessages.Length)];

                    RecordScore(time);
                    MessageBox.Show($"{msg}\n{Strings.Attempts}: {_attempts}\n{Strings.Time}: {time}s", Strings.CongratsTitle);
                    StartNewGame();
                    return;
                }
                else
                {
                    _losePlayer.Play();
                    _resultLabel.Text = guess < _target ? Strings.TooLow : Strings.TooHigh;
                    if (_attempts >= 3)
                    {
                        if (distance <= 10 && distance > 0)
                            _resultLabel.Text += " (" + Strings.Within10 + ")";
                        else
                            _resultLabel.Text += guess < _target ? " ↑" : " ↓";
                    }
                }

                _timerLabel.Text = Strings.Time + $": {(int)_stopwatch.Elapsed.TotalSeconds}s";
            }
            else
            {
                _resultLabel.Text = Strings.InvalidInput;
            }

            _inputBox.Focus();
        }
    }
}
