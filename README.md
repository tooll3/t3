# Tooll 3 - A realtime animation toolkit

[![Discord](https://img.shields.io/discord/823853172619083816.svg?style=for-the-badge)](https://discord.gg/YmSyQdeH3S)
[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](https://github.com/alelievr/Mixture/blob/master/LICENSE)

[![tooll-screenshot](https://user-images.githubusercontent.com/1732545/173256422-a4ef9894-d954-4bc3-8c24-000bfbe1c3ad.png)](https://www.youtube.com/watch?v=PrxhwOC9hLw "Tooll3 - A quick overview")


## Installation
A standalone version is in development. Although it's working already, it needs further clean up and testing.
The preview build of the 3.3.0 version can be found [here](https://github.com/still-scene/t3/releases/tag/v3.3.0). Please report any issues you might encounter.

This means that you need an IDE, like Visual Studio or Rider, to build and run it. This is free and not as difficult as it might sound.

### Dependencies

1. We only test on Win10. But Win11 might work too.
2. If you don't have a .net IDE installed already download and install the [Community Edition of Visual Studio  v16.11 (or later)](https://visualstudio.microsoft.com/downloads/).
   In the installer make sure to select the features...
   1. .net Desktop Application development
   2. .net 4.7.1  (on the right side)
4. Install [.net 5.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-5.0.404-windows-x64-installer)
3. You might also want to download and install a git client like https://git-fork.com/ . Alternatively, you can install the bare bone git scm.
4. On Windows 10, you also need to install [Windows Graphics tools](https://docs.microsoft.com/en-us/windows/uwp/gaming/use-the-directx-runtime-and-visual-studio-graphics-diagnostic-features)

### Cloning the repository

#### If you don't have a GitHub account 
Ideally, it would be better to sign-up. It's free and only takes a minute or so. This will allow you share your changes with the community. If not, do the following:

1. Make sure that you have git scm installed (see above)
2. Right file explorer right click on the folder you want T3 to install in and select **Open git bash here**
3. Clone:
```git clone git@github.com:still-scene/t3.git```
Note: As of 2021-11-05 we no longer use submodules, so you don't have to care about setting up those.


#### If you have a GitHub account

   1. If you have a GitHub account, we recommend using ssh. Make sure you have an ssh-key installed correctly. GitHub has [excellent documentation](https://docs.github.com/en/github/authenticating-to-github/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account) on that topic.
      
   2. With Fork you just clone the repository.
   
   3. If you're using the command line

      ```git clone 
      git clone  git@github.com:still-scene/t3.git
      ```

### Completing the installation

   1. Start `Install/install.bat` To initialize some dependencies and the default view layouts. If you cleaned your solution with visual studio, you might need to run the install.bat script again.

## Building and starting

   1. Open `t3.sln`
   2. In the Solution Explorer right click on **T3** to open the Properties panel. Under the section **Debug**, change the **Working  directory** for all build modes to `..`. This is important, because the Resources folder needs to be on the same logical level as the starting directory. If this is not match you will experience errors like "t3.ico" not found.
   3. Start the project in Debug or Release mode

## Get Help
To get started read the [documentation](https://github.com/still-scene/t3/wiki/user-interface) or watch [tutorial videos](https://www.youtube.com/watch?v=eH2E02U6P5Q&list=PLj-rnPROvbn3LigXGRSDvmLtgTwmNHcQs&index=4)

If you have questions or feedback, please join us on discord: https://discord.gg/YmSyQdeH3S 





