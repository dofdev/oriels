@REM make console window active
@REM powershell -window normal -command ""
dotnet build
pause
cd bin/Debug
@REM echo "zipping into dofdev site"
@REM 7z u oriels.zip %cd%/netcoreapp3.1
@REM Xcopy "oriels.zip" "C:/dofdev/Web Development/dofdev/res/oriels.zip" /F /Y
cd net6.0
oriels.exe