# GuessGame

This repository contains a simple number guessing game written in C#. The game picks a random number between 1 and 100 and prompts the player to guess until the correct number is chosen.

## Building and running

Ensure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed (version 8.0 or newer). To build and run the games:

```bash
# Restore dependencies and build
dotnet build src/GuessGame.sln

# Run the console game
dotnet run --project src/GuessGame

# Run the GUI version
dotnet run --project src/GuessGame.Gui
```

## Gameplay

When you run the program you will be asked to guess a number between 1 and 100. After each guess the program indicates whether your guess was too high or too low until you find the correct number. The program also tells you how many attempts you needed.

## License

This project is licensed under the MIT License. See [src/LICENSE](src/LICENSE) for details.

## Troubleshooting

### No sound on Linux/macOS

The GUI uses `System.Media.SystemSounds` to play Windows sound events when you
guess. These events are only available on platforms that provide the Windows
sound theme. On Linux or macOS you might not hear any audio when using the GUI
version of the game. If you want audible feedback on those systems, replace the
calls to `SystemSounds.*.Play()` with a custom `SoundPlayer` playing a `.wav`
file or ensure your desktop environment supports these system sounds.