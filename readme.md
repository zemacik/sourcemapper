# SourceMapper - 

This project ...... .

**🛑 DISCLAIMER: This project was created in my spare time during 2 evenings just for fun. Do not ask me to make urgent
fixes or add new features. I may consider it, but I cannot promise anything. My time is limited. I have a job, a family,
dog, garden, etc.**

## Prerequisites

Before you begin, ensure you have met the following requirements:

1. You have installed the version of .NET Core SDK 7.0 or higher.

   To install .NET Core SDK, follow these steps:
    1. Download the .NET Core SDK installer from https://dotnet.microsoft.com/en-us/download/dotnet/7.0
    2. Run the installer and follow the instructions.
    3. Verify the installation by running the following command:
        ```
        dotnet --version
        ```

## Installation

To install and run this project, follow these steps:

1. Clone this repository to your local machine.
2. Navigate to the solution folder 'src'.
3. Install the required dependencies by running the following command:

    ```
    dotnet restore
    ```

4. Run the application by running the following command:

    ```
    dotnet build
    ```

## Usage

### Installation

To use this project acts as dotnet global tool, follow these steps:

1. Navigate to the solution folder 'src'.
2. Install the tool by running the following command:

    ```
    dotnet pack ..\src\SourceMapper -c Release -o nupkg
    dotnet tool install --global --add-source ..\src\SourceMapper\nupkg SourceMapper.App.CLI
    ```

3. Run the application by running the following command for the first time:

    ```
    sourcemapper --help
    ```

### Update

1. Navigate to the solution folder 'src'.
2. Update the tool by running the following command:

    ```
    dotnet pack ..\src\SourceMapper -c Release -o nupkg
    dotnet tool update --global --add-source ..\src\SourceMapper\nupkg SourceMapper.App.CLI
    ```

3. Run the application by running the following command for the first time:

    ```
    SourceMapper --help
    ```

## Command-line usage

TODO

## Components

TODO

## License

This project is released under the MIT License.
See [LICENSE](https://github.com/zemacik/SourceMapper/blob/main/LICENSE) for further details.
