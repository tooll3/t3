# Tooll 3 - A realtime animation toolkit


## Installation

As of now there is no stand-alone version available. This means that you need an IDE like Visual Studio or Rider to build and run it. This is free and not as difficult as it might sound.

### Dependencies

1. We only test on Win10. But Win7 might work too.
2. If you don't have a .net IDE installed already download and install the [Community Edition of Visual Studio](https://visualstudio.microsoft.com/downloads/).  
   In the installer make sure to select the features...
   1. .net Desktop Application development
   2. .net 4.7.1  (on the right side)
3. You might also want to download and install a git client like https://git-fork.com/ . Alternatively you can install the bare bone git scm.
4. On Windows 10 you also need to install [Windows Graphics tools](https://docs.microsoft.com/en-us/windows/uwp/gaming/use-the-directx-runtime-and-visual-studio-graphics-diagnostic-features)

### Cloning the repository

#### If you don't have a git account 
Ideally, it would be better to sign-up. It's free and only takes a minute or so. This will allow you share your changes with the community. If not, do the following:

1. Make sure that you have git scm installed (see above)

2. Right file explorer right click on the folder you want T3 to install in and select **Open git bash here**

3. Clone:
```git clone git@github.com:still-scene/t3.git```

4. After cloning adjust the file `.git\config` and replace these lines...
```
[submodule "Operators"]
	url = git@github.com:still-scene/Operators.git
[submodule "Resources"]
	url = git@github.com:still-scene/Resources.git
```
... with...
```
[submodule "Operators"]
	url = https://github.com/still-scene/Operators.git
[submodule "Resources"]
	url = https://github.com/still-scene/Resources.git
```

5. In the terminal window initialize and update the submodules...
```
git submodule init
git submodule update
```


#### If you have a git account

   1. If you have a git account, we recommend using ssh. Make sure you have an ssh-key installed correctly. Github has [excellent documentation](https://docs.github.com/en/github/authenticating-to-github/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account) on that topic.
      
   2. With Fork you just clone the repository. All submodule dependencies should be pulled automatically.

   3. If you're using the command line

      ```git clone 
      git clone  --recursive git@github.com:still-scene/t3.git
      ```

### Completing the installation

   1. Start `Install/install.bat` To initialize some dependencies and the default view layouts. If you cleaned your solution with visual studio, you might need to run the install.bat script again.

## Building and starting

   1. Open `t3.sln`
   2. In the Solution Explorer right click on **T3** to open the Properties panel. Under the section **Debug** change the **Working  directory** for all build modes to `../../..`. This is important, because the Resources folder needs to be on the same logical level as the starting directory. If this is not match you will experience errors like "t3.ico" not found.
   3. Start the project in Debug or Release mode







