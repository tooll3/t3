# Building a Tooll3 Installer

## Preface

We're using [Inno Setup](https://jrsoftware.org/isinfo.php) to generate a feature-complete exe-installer that includes all dependencies and installs the Window Graphic Tools. Although not as generic as other solutions like [WiX](https://wixtoolset.org/), it was simple to set up, works out of the box, and gets the job done. In the long run, a CI/CD solution for other platforms would be great.

## Setup

### Build Project
1. Clone `git@github.com:tooll3/t3.git` and switch to the `main` branch.
2. Open the project in Rider or Visual Studio.
3. Make sure you're in release mode.
4. Rebuild the solution. The result should be a valid, feature-complete build in `.\Editor\bin\Release\net9.0-windows\`.

### Download Dependencies

The installer will look for the dependencies listed in `installer.iss`. As of writing this, these are:

- [dotnet-sdk-9.0.102-win-x64.exe](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-9.0.102-windows-x64-installer)
- [VC_redist.x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe)

Download these files into the  `Installer\downloads\` folder, so you have this folder structure:

```
Editor\bin\Release\net9.0-windows\

Installer\dependencies\
Installer\dependencies\downloads\dotnet-sdk-9.0.102-win-x64.exe
Installer\dependencies\downloads\VC_redist.x64.exe
```

### Download and Install Inno Setup

1. Download from [here](https://jrsoftware.org/isdl.php) and install.
2. Part of the Tooll solution is the `Installer\` folder, which contains `installer.iss`.
3. Open the script with Inno Setup by double-clicking it.
4. Click the blue play button to start the build process and wait several minutes.
5. The installer will run for testing.
6. The output artifact is located at `Installer\Output\`.