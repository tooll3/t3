@RD /S /Q "..\T3-Standalone"
mkdir "..\T3-Standalone"

Xcopy "Resources" "..\T3-Standalone\Resources" /E /H /C /I
Xcopy ".Variations" "..\T3-Standalone\.Variations" /E /H /C /I
Xcopy "Operators" "..\T3-Standalone\Operators" /E /H /C /I
Xcopy "Editor\bin\Release\net6.0-windows" "..\T3-Standalone\" /E /H /C /I
Xcopy "StartT3\bin\Release\net6.0-windows" "..\T3-Standalone\" /E /H /C /I
Xcopy "Player\bin\Release\net6.0-windows" "..\T3-Standalone\Player\bin\Release\net6.0-windows" /E /H /C /I
    
copy "Editor\bin\Release\net6.0-windows\Operators.dll" "..\T3-Standalone\Operators_Reference.dll"

mkdir "..\T3-Standalone\.t3"
Xcopy "Install\default-layouts" "..\T3-Standalone\.t3\layouts" /E /H /C /I

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


@RD /S /Q "..\T3-Standalone\runtimes\linux-x64"
@RD /S /Q "..\T3-Standalone\runtimes\osx-universal"
@RD /S /Q "..\T3-Standalone\runtimes\osx-x64"
@RD /S /Q "..\T3-Standalone\runtimes\win-x86"

@RD /S /Q "..\T3-Standalone\Resources\user\wake"
@RD /S /Q "..\T3-Standalone\Resources\user\pixtur\Vj\Faces"