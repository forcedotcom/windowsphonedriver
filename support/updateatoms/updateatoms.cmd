@echo off
if "%1"=="" goto nodir
if not exist %1\javascript\windowsphone-driver md %1\javascript\windowsphone-driver
copy /y %~dp0\build.desc %1\javascript\windowsphone-driver > nul
pushd %1
call %1\go.bat //javascript/windowsphone-driver:atoms
popd
copy /y %1\build\javascript\windowsphone-driver\WebDriverAtoms.cs %~dp0\..\..\src\WindowsPhoneDriverBrowser > nul
goto end

:nodir
echo You must specify the root of the WebDriver project sources.

:end