using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    public class MainForm : Form
    {
        private readonly Label _promptLabel;
        private readonly TextBox _inputBox;
        private readonly Button _guessButton;
        private readonly Label _resultLabel;
        private readonly Label _attemptsLabel;
        private readonly Random _random = new Random();
        private readonly string[] _successMessages = new[] { "Great job!", "Awesome!", "Well done!" };
        private int _target;
        private int _attempts;

        public MainForm()
        {
            Text = "Guess Game";
            ClientSize = new Size(500, 200);

            _promptLabel = new Label { Text = "Guess a number (1-100):", AutoSize = true, Location = new Point(10, 10) };
            _inputBox = new TextBox { Location = new Point(10, 35), Width = 100 };
            _guessButton = new Button { Text = "Guess", Location = new Point(120, 33) };
            _resultLabel = new Label { AutoSize = true, Location = new Point(10, 70) };
            _attemptsLabel = new Label { AutoSize = true, Location = new Point(10, 95) };

            _guessButton.Click += OnGuess;
            AcceptButton = _guessButton;

            Controls.AddRange(new Control[] { _promptLabel, _inputBox, _guessButton, _resultLabel, _attemptsLabel });

            StartNewGame();
        }

        private void StartNewGame()
        {
            _target = _random.Next(1, 101);
            _attempts = 0;
            _resultLabel.Text = string.Empty;
            _inputBox.Text = string.Empty;
            _attemptsLabel.Text = "Attempts: 0";
        }

        private void OnGuess(object? sender, EventArgs e)
        {
            if (int.TryParse(_inputBox.Text, out int guess))
            {
                _attempts++;
                _attemptsLabel.Text = $"Attempts: {_attempts}";
                if (guess < _target)
                {
                    _resultLabel.Text = "Too low!";
                }
                else if (guess > _target)
                {
                    _resultLabel.Text = "Too high!";
                }
                else
                {
                    string msg = _successMessages[_random.Next(_successMessages.Length)];
                    MessageBox.Show($"{msg} You guessed it in {_attempts} attempts.", "Congratulations");
                    StartNewGame();
                }
            }
            else
            {
                _resultLabel.Text = "Enter a valid number.";
            }
        }
    }
}