mkdir ..\T3\bin\Debug\net6.0-windows
copy dependencies\*.dll ..\T3\bin\Debug\net6.0-windows\

mkdir ..\T3\bin\Release\net6.0-windows
copy dependencies\*.dll ..\T3\bin\Release\net6.0-windows\

mkdir ..\Player\bin\Release\net6.0-windows
copy dependencies\*.dll ..\Player\bin\Release\net6.0-windows\

Xcopy /E /I default-layouts ..\.t3\layouts\

copy default-homecanvas\HomeCanvas.cs ..\Operators\Types\
copy default-homecanvas\HomeCanvas_dab61a12-9996-401e-9aa6-328dd6292beb.t3 ..\Operators\Types\
copy default-homecanvas\HomeCanvas_dab61a12-9996-401e-9aa6-328dd6292beb.t3ui ..\Operators\Types\
