rem please read https://github.com/tooll3/t3/wiki/StandAloneBuilds

@RD /S /Q "..\T3-Standalone"
mkdir "..\T3-Standalone"

Xcopy "Resources" "..\T3-Standalone\Resources" /E /H /C /I
Xcopy ".Variations" "..\T3-Standalone\.Variations" /E /H /C /I
Xcopy "Operators\Resources" "..\T3-Standalone\Operators\Resources" /E /H /C /I
Xcopy "Operators\Types" "..\T3-Standalone\Operators\Types" /E /H /C /I
Xcopy "Operators\Utils" "..\T3-Standalone\Operators\Utils" /E /H /C /I
Xcopy "Editor\bin\Release\net6.0-windows" "..\T3-Standalone\" /E /H /C /I
Xcopy "StartT3\bin\Release\net6.0-windows" "..\T3-Standalone\" /E /H /C /I
Xcopy "Install\dependencies" "..\T3-Standalone\Player\bin\Release\net6.0-windows\" /E /H /C /I

mkdir "..\T3-Standalone\Player\bin\Release\net6.0-windows\publish"
copy "Player\bin\Release\net6.0-windows\publish\Player.exe"                    "..\T3-Standalone\Player\bin\Release\net6.0-windows\publish\Player.exe"
copy "Editor\bin\Release\net6.0-windows\Svg.dll"  "..\T3-Standalone\Player\bin\Release\net6.0-windows\publish\Svg.dll"
copy "Editor\bin\Release\net6.0-windows\Operators.dll" "..\T3-Standalone\Operators_Reference.dll"

mkdir "..\T3-Standalone\.t3"
Xcopy "Install\default-layouts" "..\T3-Standalone\.t3\layouts" /E /H /C /I
Xcopy "Install\default-themes" "..\T3-Standalone\.t3\Themes" /E /H /C /I
copy "Install\gradients.json" "..\T3-Standalone\.t3\gradients.json"
copy "Install\resolutions.json" "..\T3-Standalone\.t3\resolutions.json"

@RD /S /Q "..\T3-Standalone\cs"
@RD /S /Q "..\T3-Standalone\de"
@RD /S /Q "..\T3-Standalone\es"
@RD /S /Q "..\T3-Standalone\fr"
@RD /S /Q "..\T3-Standalone\it"
@RD /S /Q "..\T3-Standalone\ja"
@RD /S /Q "..\T3-Standalone\ko"
@RD /S /Q "..\T3-Standalone\pl"
@RD /S /Q "..\T3-Standalone\pt-BR"
@RD /S /Q "..\T3-Standalone\ref"
@RD /S /Q "..\T3-Standalone\ru"
@RD /S /Q "..\T3-Standalone\tr"
@RD /S /Q "..\T3-Standalone\zh-Hans"
@RD /S /Q "..\T3-Standalone\zh-Hant"


@RD /S /Q "..\T3-Standalone\runtimes\freebsd"
@RD /S /Q "..\T3-Standalone\runtimes\illumos"
@RD /S /Q "..\T3-Standalone\runtimes\ios"
@RD /S /Q "..\T3-Standalone\runtimes\linux-arm"
@RD /S /Q "..\T3-Standalone\runtimes\linux-arm64"
@RD /S /Q "..\T3-Standalone\runtimes\linux-x64"
@RD /S /Q "..\T3-Standalone\runtimes\linux"
@RD /S /Q "..\T3-Standalone\runtimes\osx-arm64"
@RD /S /Q "..\T3-Standalone\runtimes\osx-universal"
@RD /S /Q "..\T3-Standalone\runtimes\osx-x64"
@RD /S /Q "..\T3-Standalone\runtimes\osx"
@RD /S /Q "..\T3-Standalone\runtimes\solaris"
@RD /S /Q "..\T3-Standalone\runtimes\tvos"
@RD /S /Q "..\T3-Standalone\runtimes\unix"
@RD /S /Q "..\T3-Standalone\runtimes\win-arm64"
@RD /S /Q "..\T3-Standalone\runtimes\win-x86"
@RD /S /Q "..\T3-Standalone\runtimes\win"

@RD /S /Q "..\T3-Standalone\Resources\user\wake"
@RD /S /Q "..\T3-Standalone\Resources\user\pixtur\Vj\Faces"
@RD /S /Q "..\T3-Standalone\Resources\user\still\proj-monsters"
@RD /S /Q "..\T3-Standalone\Resources\user\still\carpe-diem"
@RD /S /Q "..\T3-Standalone\Resources\user\1x\mars-express\references"
@RD /S /Q "..\T3-Standalone\Resources\user\1x\mars-express\sequence"
@RD /S /Q "..\T3-Standalone\Resources\user\ff\persona"

