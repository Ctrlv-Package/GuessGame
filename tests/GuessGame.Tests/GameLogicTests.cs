using Xunit;
using GuessGame.Gui;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace GuessGame.Tests
{
    public class GameLogicTests
    {
        [Fact]
        public void Test_NewGame_InitializesCorrectly()
        {
            using var form = new MainForm();
            Assert.NotEqual(0, form.MaxRange);
            Assert.Equal(0, form.Attempts);
        }

        [Theory]
        [InlineData(1, 100, 50, "Too low!", 75)]  // Binary search test case
        [InlineData(1, 100, 75, "Too high!", 62)]
        public void Test_GuessLogic(int min, int max, int guess, string expectedResult, int nextGuess)
        {
            using var form = new MainForm();
            form.SetTestMode(true);  // This will be a new method we need to add
            form.SetTargetNumber(nextGuess);  // This will be a new method we need to add
            
            var result = form.ProcessGuess(guess);
            Assert.Equal(expectedResult, result);
            Assert.Equal(1, form.Attempts);
        }

        [Fact]
        public void Test_ScoreRecording()
        {
            using var form = new MainForm();
            form.SetTestMode(true);
            form.SetTargetNumber(50);

            // Simulate winning the game
            var result = form.ProcessGuess(50);
            Assert.Equal("Correct!", result);

            // Verify score is recorded correctly
            var scores = form.GetScores();  // This will be a new method we need to add
            Assert.Contains(scores, s => s.Attempts == 1);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public void Test_ValidGuesses(int guess)
        {
            using var form = new MainForm();
            form.SetTestMode(true);
            form.SetTargetNumber(50);

            var result = form.ProcessGuess(guess);
            Assert.NotNull(result);  // Should return some result
            Assert.True(form.Attempts > 0);  // Should increment attempts
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(-1)]
        public void Test_InvalidGuesses(int guess)
        {
            using var form = new MainForm();
            form.SetTestMode(true);
            form.SetTargetNumber(50);

            Assert.Throws<ArgumentOutOfRangeException>(() => form.ProcessGuess(guess));
            Assert.Equal(0, form.Attempts);  // Should not increment attempts for invalid guesses
        }
    }
}
