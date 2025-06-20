using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GuessGame.Gui.Data
{
    public class GameDataManager
    {
        private const string DataDirectory = "GameData";
        private const string ScoresDirectory = "Scores";
        private static readonly JsonSerializerOptions JsonOptions = new() 
        { 
            WriteIndented = true 
        };

        public GameDataManager()
        {
            EnsureDirectoriesExist();
        }

        private static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(Path.Combine(DataDirectory, ScoresDirectory));
        }

        public async Task SaveBestScoreAsync(int score)
        {
            var path = Path.Combine(DataDirectory, "bestscore.json");
            try
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(score, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new GameDataException("Failed to save best score", ex);
            }
        }

        public async Task<int> LoadBestScoreAsync()
        {
            var path = Path.Combine(DataDirectory, "bestscore.json");
            try
            {
                if (!File.Exists(path))
                    return int.MaxValue;

                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<int>(json);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                throw new GameDataException("Failed to load best score", ex);
            }
        }

        public async Task SaveLeaderboardAsync(string difficulty, List<ScoreEntry> scores)
        {
            var path = Path.Combine(DataDirectory, ScoresDirectory, $"leaderboard_{difficulty.ToLower()}.json");
            try
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(scores, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new GameDataException($"Failed to save {difficulty} leaderboard", ex);
            }
        }

        public async Task<List<ScoreEntry>> LoadLeaderboardAsync(string difficulty)
        {
            var path = Path.Combine(DataDirectory, ScoresDirectory, $"leaderboard_{difficulty.ToLower()}.json");
            try
            {
                if (!File.Exists(path))
                    return new List<ScoreEntry>();

                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? new List<ScoreEntry>();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                throw new GameDataException($"Failed to load {difficulty} leaderboard", ex);
            }
        }
    }

    public class GameDataException : Exception
    {
        public GameDataException(string message, Exception? innerException = null) 
            : base(message, innerException)
        {
        }
    }
}
