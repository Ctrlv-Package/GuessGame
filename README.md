# GuessGame

This repository contains a simple number guessing game written in C#. The game picks a random number between 1 and 100 and prompts the player to guess until the correct number is chosen.

## Building and running

Ensure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed (version 8.0 or newer). To build and run the game from the command line:

```bash
# Restore dependencies and build
dotnet build

# Run the game
dotnet run --project src/GuessGame
```

## Gameplay

When you run the program you will be asked to guess a number between 1 and 100. After each guess the program indicates whether your guess was too high or too low until you find the correct number. The program also tells you how many attempts you needed.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.