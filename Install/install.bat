mkdir ..\T3\bin\Debug\
copy dependencies\*.dll ..\T3\bin\Debug\

mkdir ..\T3\bin\Release\
copy dependencies\*.dll ..\T3\bin\Release\

Xcopy /E /I default-layouts ..\.t3
Xcopy /E /I default-dashboard ..\Operators\Types
