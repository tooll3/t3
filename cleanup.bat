@echo off
setlocal

echo Searching and deleting "bin" and "obj" directories...

for /d /r %%d in (bin obj) do (
    if exist "%%d" (
        echo Deleting directory: "%%d"
        rmdir /s /q "%%d"
    )
)

echo Deletion complete.
endlocal
pause
