mkdir ..\Editor\bin\Debug\net6.0-windows
xcopy /S dependencies\*.dll ..\Editor\bin\Debug\net6.0-windows\

mkdir ..\Editor\bin\Release\net6.0-windows
xcopy /S dependencies\*.dll ..\Editor\bin\Release\net6.0-windows\

mkdir ..\Player\bin\Release\net6.0-windows
xcopy /S dependencies\*.dll ..\Player\bin\Release\net6.0-windows\publish

Xcopy /E /I default-layouts ..\.t3\layouts\

Xcopy /E /I default-themes ..\.t3\themes\
copy gradients.json ..\.t3\

copy default-homecanvas\HomeCanvas.cs ..\Operators\Types\
copy default-homecanvas\HomeCanvas_dab61a12-9996-401e-9aa6-328dd6292beb.t3 ..\Operators\Types\
copy default-homecanvas\HomeCanvas_dab61a12-9996-401e-9aa6-328dd6292beb.t3ui ..\Operators\Types\
