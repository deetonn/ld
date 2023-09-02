
# Project Overview

This project is a .NET 7.0 solution consisting of three main projects:

1. ld: The core library of the solution.
2. playground: An executable project that references the ld library.
3. ld.tests: A test project for the ld library.
Core Libraries

The ld project uses the following NuGet packages:

- Pastel version 4.1.0
- Spectre.Console version 0.47.0
Playground

The playground project is an executable project that references the ld library. It also uses the Pastel and Spectre.Console NuGet packages.
Tests

The ld.tests project is a test project for the ld library. It uses the MSTest framework.
Building

The solution can be built using Visual Studio 2022 or later, or using the .NET CLI with the dotnet build command.
Known Issues

There is a known issue with the lexer not providing correct line numbers. This is currently being worked on.
Contributing

Contributions are welcome. Please submit a pull request or create an issue to discuss any changes you wish to make.
License

This project is licensed under the MIT License.
