@echo off
setlocal

set config=%~1


if "%config%"=="" (
	echo Build configuration DEBUG^|RELEASE should be specified as the first command line argument.
	goto :error
)

where dotnet.exe > nul
if errorlevel 1 (
	echo dotnet.exe not found. Please install .NET Core.
	goto :error
)


set outputFolder="%~dp0..\%config%"
call :ensureOutputFolder %outputFolder%
if errorlevel 1 goto :error


cd /d "%~dp0\..\oledump"
if errorlevel 1 (
	echo Failed to change to oledump project folder
	goto :error
)

if "%VCTargetsPath%"=="" (
	call :SetUpVCTargetsPath
	if errorlevel 1 goto :error
)

rem dotnet pack oledump.csproj --no-build -c "%config%"  --output %outputFolder%

msbuild -t:pack oledump.csproj


if errorlevel 1 goto :error

goto :eof

:SetUpVCTargetsPath

set tempFile="%temp%\props.txt"
where /R "%ProgramFiles(x86)%" microsoft.cpp.default.props > %tempFile%
if errorlevel 1 (
	echo Could not find microsoft.cpp.default.props
	exit /b 1
)
for /f "tokens=*" %%f in ('type %tempFile%') do set VCTargetsPath=%%~dpf

goto :eof

:ensureOutputFolder 
rem expects the output folder as %1
if exist "%~1\" goto :eof

md "%~1"
if errorlevel 1 (
	echo Failed to create output folder %~1
	exit /b 1
)

goto :eof

:error
echo ERROR
exit /b 1