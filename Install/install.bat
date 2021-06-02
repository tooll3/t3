mkdir ..\T3\bin\Debug\
copy dependencies\*.dll ..\T3\bin\Debug\

mkdir ..\T3\bin\Release\
copy dependencies\*.dll ..\T3\bin\Release\

Xcopy /E /I default-layouts ..\.t3

copy default-dashboard\Dashboard_dab61a12-9996-401e-9aa6-328dd6292beb.t3 ..\Operators\Types\
copy default-dashboard\Dashboard_dab61a12-9996-401e-9aa6-328dd6292beb.t3ui ..\Operators\Types\
