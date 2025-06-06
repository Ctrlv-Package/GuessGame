using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    public class MainForm : Form
    {
        private readonly Label _promptLabel, _resultLabel, _attemptsLabel, _timerLabel;
        private readonly TextBox _inputBox;
        private readonly Button _guessButton;
        private readonly ComboBox _difficultyBox;
        private readonly Random _random = new Random();
        private readonly string[] _successMessages = new[]
        {
            "Great job!", "Awesome!", "Well done!",
            "You read my mind!", "Are you psychic?", "Boom! Nailed it!", "You must be cheating 😆"
        };
        private int _target, _attempts, _bestScore = int.MaxValue, _maxRange = 100;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Color _defaultBackColor;
        private const string BestScoreFile = "bestscore.txt";
        private const string LeaderboardFile = "leaderboard.txt";

        public MainForm()
        {
            Text = "🎯 Guessing Game Deluxe";
            Icon = SystemIcons.Information;
            ClientSize = new Size(500, 300);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font(Font.FontFamily, 14);
            _defaultBackColor = BackColor;

            _promptLabel = new Label
            {
                Text = "Guess a number:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _difficultyBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font(Font.FontFamily, 14),
                Height = 40
            };
            _difficultyBox.Items.AddRange(new[] { "Easy (1–50)", "Medium (1–100)", "Hard (1–500)" });
            _difficultyBox.SelectedIndex = 1;
            _difficultyBox.SelectedIndexChanged += (_, _) => ChangeDifficulty();

            _inputBox = new TextBox
            {
                Font = new Font(Font.FontFamily, 14),
                Margin = new Padding(0, 5, 10, 5),
                MinimumSize = new Size(100, 35)
            };

            _guessButton = new Button
            {
                Text = "Guess",
                Width = 100,
                Height = 40,
                BackColor = Color.MediumSlateBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                Padding = new Padding(5, 2, 5, 2)
            };

            _resultLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            _attemptsLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            _timerLabel = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };

            _guessButton.Click += OnGuess;
            AcceptButton = _guessButton;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100 / 6F));

            layout.Controls.Add(_promptLabel, 0, 0);

            var difficultyPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                ColumnCount = 1
            };
            difficultyPanel.Controls.Add(_difficultyBox, 0, 0);
            layout.Controls.Add(difficultyPanel, 0, 1);

            var inputPanel = new FlowLayoutPanel { Anchor = AnchorStyles.None, AutoSize = true };
            inputPanel.Controls.Add(_inputBox);
            inputPanel.Controls.Add(_guessButton);
            layout.Controls.Add(inputPanel, 0, 2);

            layout.Controls.Add(_resultLabel, 0, 3);
            layout.Controls.Add(_attemptsLabel, 0, 4);
            layout.Controls.Add(_timerLabel, 0, 5);

            Controls.Add(layout);

            LoadBestScore();
            StartNewGame();
        }

        private void ChangeDifficulty()
        {
            _maxRange = _difficultyBox.SelectedIndex switch
            {
                0 => 50,
                1 => 100,
                2 => 500,
                _ => 100
            };
            StartNewGame();
        }

        private void StartNewGame()
        {
            _target = _random.Next(1, _maxRange + 1);
            _attempts = 0;
            _resultLabel.Text = string.Empty;
            _inputBox.Text = string.Empty;
            _attemptsLabel.Text = "Attempts: 0";
            _timerLabel.Text = "Time: 0s";
            BackColor = _defaultBackColor;
            _stopwatch.Restart();
            _inputBox.Focus();
        }

        private void OnGuess(object? sender, EventArgs e)
        {
            string input = _inputBox.Text;
            _inputBox.Clear();

            if (int.TryParse(input, out int guess))
            {
                _attempts++;
                _attemptsLabel.Text = $"Attempts: {_attempts}";

                int distance = Math.Abs(guess - _target);

                // Color feedback
                if (distance == 0) BackColor = Color.LightGreen;
                else if (distance <= 5) BackColor = Color.LightGoldenrodYellow;
                else if (distance <= 10) BackColor = Color.Khaki;
                else BackColor = Color.LightCoral;

                if (guess == _target)
                {
                    _stopwatch.Stop();
                    int timeTaken = (int)_stopwatch.Elapsed.TotalSeconds;
                    SystemSounds.Exclamation.Play();

                    string msg = _successMessages[_random.Next(_successMessages.Length)];
                    string bestScoreMsg = "";

                    if (_attempts < _bestScore)
                    {
                        _bestScore = _attempts;
                        bestScoreMsg = "\n🎉 New Best Score!";
                        SaveBestScore();
                    }

                    SaveToLeaderboard(_attempts, timeTaken);

                    MessageBox.Show($"{msg} You guessed it in {_attempts} attempts and {timeTaken} seconds.{bestScoreMsg}", "🏆 Congratulations");

                    ShowLeaderboard();
                    StartNewGame();
                    return;
                }
                else
                {
                    SystemSounds.Hand.Play();
                    _resultLabel.Text = guess < _target ? "Too low!" : "Too high!";
                    _timerLabel.Text = $"Time: {(int)_stopwatch.Elapsed.TotalSeconds}s";
                }
            }
            else
            {
                _resultLabel.Text = "Enter a valid number.";
            }

            _inputBox.Focus();
        }

        private void LoadBestScore()
        {
            if (File.Exists(BestScoreFile) && int.TryParse(File.ReadAllText(BestScoreFile), out int score))
                _bestScore = score;
        }

        private void SaveBestScore()
        {
            File.WriteAllText(BestScoreFile, _bestScore.ToString());
        }

        private void SaveToLeaderboard(int attempts, int timeTaken)
        {
            string name = Prompt("Enter your name for the leaderboard:");
            if (string.IsNullOrWhiteSpace(name)) name = "Anonymous";

            File.AppendAllText(LeaderboardFile, $"{name},{attempts},{timeTaken}{Environment.NewLine}");
        }

        private void ShowLeaderboard()
        {
            if (!File.Exists(LeaderboardFile)) return;

            var lines = File.ReadAllLines(LeaderboardFile)
                .Select(line => line.Split(','))
                .Where(parts => parts.Length == 3)
                .Select(parts => new { Name = parts[0], Attempts = parts[1], Time = parts[2] })
                .OrderBy(x => int.Parse(x.Attempts))
                .ThenBy(x => int.Parse(x.Time))
                .Take(5);

            var sb = new StringBuilder("🏅 Top 5 Leaderboard:\n");
            foreach (var entry in lines)
                sb.AppendLine($"{entry.Name} - {entry.Attempts} attempts in {entry.Time}s");

            MessageBox.Show(sb.ToString(), "Leaderboard");
        }

        private string Prompt(string message)
        {
            var prompt = new Form() { Width = 300, Height = 150, Text = "Enter Name" };
            var textBox = new TextBox() { Left = 50, Top = 20, Width = 200 };
            var button = new Button() { Text = "OK", Left = 100, Width = 100, Top = 50, DialogResult = DialogResult.OK };
            var label = new Label() { Text = message, Left = 50, Top = 0, Width = 200 };

            prompt.Controls.Add(label);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(button);
            prompt.AcceptButton = button;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}
