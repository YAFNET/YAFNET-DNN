@SET CONFIGURATION=Release

"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -nologo -latest -property installationPath > temp.txt
set /p $MSBUILDROOT=<temp.txt
del temp.txt

"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -property installationVersion > temp.txt
Rem lower VS version
Rem set /p $MSBUILDVER=<temp.txt
Rem higher/unique VS
for /f "delims==" %%a in (temp.txt) do set $MSBUILDVER=%%a
del temp.txt

for /f "tokens=1 delims=." %%G in ("%$MSBUILDVER%") do set Current=%%G.0
Rem VS2017 => ~\MSBuild\15.0\Bin\MSBuild.exe
Rem VS2019 => ~\MsBuild\Current\Bin\MSBuild.exe
If "%Current%" NEQ "15.0" set Current=Current

@set $MSBUILDPATH="%$MSBUILDROOT%\MsBuild\%Current%\Bin\MSBuild.exe"

Rem dotnet restore does not restore packages.config packages (e.g. StackTraceBeautify used by YAF-SqlServer.csproj).
Rem They must go to yafsrc\packages (the HintPath location), so restore with SolutionDir pointing there.
%$MSBUILDPATH% ..\yafsrc\YetAnotherForum.NET\YAF-SqlServer.csproj -t:restore -p:RestorePackagesConfig=true -p:SolutionDir="%~dp0..\yafsrc\\" -nologo -v:minimal
%$MSBUILDPATH% YAF.DNN.Module.slnx -t:restore -nologo -v:minimal

%$MSBUILDPATH% YAF.DNN.Module.slnx /p:Configuration=Release /t:Build /p:WarningLevel=0;CreateDnnPackages=true /flp1:logfile=errors.txt;errorsonly %1 %2 %3 %4 %5 %6 %7 %8 %9 