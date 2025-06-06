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
        private readonly Random _random = new Random();
        private int _target;
        private int _attempts;

        public MainForm()
        {
            Text = "Guess Game";
            ClientSize = new Size(250, 120);

            _promptLabel = new Label { Text = "Guess a number (1-100):", AutoSize = true, Location = new Point(10, 10) };
            _inputBox = new TextBox { Location = new Point(10, 35), Width = 100 };
            _guessButton = new Button { Text = "Guess", Location = new Point(120, 33) };
            _resultLabel = new Label { AutoSize = true, Location = new Point(10, 70) };

            _guessButton.Click += OnGuess;

            Controls.AddRange(new Control[] { _promptLabel, _inputBox, _guessButton, _resultLabel });

            StartNewGame();
        }

        private void StartNewGame()
        {
            _target = _random.Next(1, 101);
            _attempts = 0;
            _resultLabel.Text = string.Empty;
            _inputBox.Text = string.Empty;
        }

        private void OnGuess(object? sender, EventArgs e)
        {
            if (int.TryParse(_inputBox.Text, out int guess))
            {
                _attempts++;
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
                    MessageBox.Show($"Correct! You guessed it in {_attempts} attempts.", "Congratulations");
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