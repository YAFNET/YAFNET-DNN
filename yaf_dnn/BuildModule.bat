@SET FrameworkSDKDir=
@SET PATH=%FrameworkDir%;%FrameworkSDKDir%;%PATH%
@SET LANGDIR=EN
@SET MSBUILDPATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MsBuild.exe"
@SET CONFIGURATION=Release



%MSBUILDPATH% YAF.DNN.Module.sln /t:restore /p:Configuration=Release /t:Clean;Build /p:WarningLevel=0;CreateDnnPackages=true /flp1:logfile=errors.txt;errorsonly %1 %2 %3 %4 %5 %6 %7 %8 %9 