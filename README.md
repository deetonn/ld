
# Project Overview

This project is a .NET 7.0 solution consisting of three main projects:

1. ld: The core library containing the parser, lexer and interpreter.
2. playground: An executable project that serves as a playground for testing and debugging.
3. ld.tests: A test suite that we aim to keep as much coverage as possible.
Core Libraries

The ld project uses the following NuGet packages:

- Pastel version 4.1.0
- Spectre.Console version 0.47.0
  
## Playground

The playground project is an executable project that references the ld library. It also uses the Pastel and Spectre.Console NuGet packages.
Edit the test_script.ld to see changes in real-time.
## Tests

The ld.tests project is a test project for the ld library. It uses the MSTest framework.
## Building

The solution can be built using Visual Studio 2022 or later, or using the .NET CLI with the dotnet build command.
Known Issues

## Contributing

Contributions are welcome. Please submit a pull request or create an issue to discuss any changes you wish to make.
License

This project is licensed under the MIT License.
