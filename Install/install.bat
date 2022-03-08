mkdir ..\T3\bin\Debug\net5.0-windows
copy dependencies\*.dll ..\T3\bin\Debug\net5.0-windows\

mkdir ..\T3\bin\Release\net5.0-windows
copy dependencies\*.dll ..\T3\bin\Release\net5.0-windows\

mkdir ..\Player\bin\Release\net5.0-windows
copy dependencies\*.dll ..\Player\bin\Release\net5.0-windows\

Xcopy /E /I default-layouts ..\.t3

copy default-dashboard\Dashboard_dab61a12-9996-401e-9aa6-328dd6292beb.t3 ..\Operators\Types\
copy default-dashboard\Dashboard_dab61a12-9996-401e-9aa6-328dd6292beb.t3ui ..\Operators\Types\
