mkdir ..\T3\bin\Debug\
copy dependencies\bass.dll ..\T3\bin\Debug\
copy dependencies\basswasapi.dll ..\T3\bin\Debug\

mkdir ..\T3\bin\Release\
copy dependencies\bass.dll ..\T3\bin\Release\
copy dependencies\basswasapi.dll ..\T3\bin\Release\

Xcopy /E /I default-layouts ..\.t3
