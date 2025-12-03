# Scrcpy Launcher

A modern, dark-themed GUI launcher for [scrcpy](https://github.com/Genymobile/scrcpy) that simplifies managing multiple Android devices.

![Scrcpy Launcher](Scrcpy_logo.svg.png)

## Features

*   **Auto-Update**: Automatically checks for and downloads the latest version of `scrcpy` from GitHub.
*   **Multi-Device Support**: View and control multiple connected devices simultaneously.
*   **Visual Interface**:
    *   **Live Previews**: See a screenshot of each connected device.
    *   **Device Details**: Displays Model, Brand, Android Version, and Serial Number.
    *   **Dark Theme**: Modern, eye-friendly user interface.
*   **One-Click Launch**: Select multiple devices and launch them all at once.

## Usage

1.  Download the latest release or compile from source.
2.  Run `LaunchScrcpy.exe`.
3.  Connect your Android devices via USB (ensure USB Debugging is enabled).
4.  The application will automatically detect devices and download `scrcpy` if needed.
5.  Select the devices you want to control by clicking on their cards.
6.  Click **LAUNCH SELECTED**.

## Building from Source

### Prerequisites
*   Windows OS
*   .NET Framework 4.5 or later (Pre-installed on most Windows systems)

### Compilation
Run the included `compile.bat` script:

```bat
compile.bat
```

This will generate `LaunchScrcpy.exe` in the same directory.

## License

This project is a launcher for [scrcpy](https://github.com/Genymobile/scrcpy).
Scrcpy is developed by Genymobile.
