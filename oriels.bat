dotnet build
pause
cd bin/Debug
@REM echo "zipping into dofdev site"
@REM 7z u oriels.zip %cd%/netcoreapp3.1
@REM Xcopy "oriels.zip" "C:/dofdev/Web Development/dofdev/res/oriels.zip" /F /Y
cd netcoreapp3.1
oriels.exe