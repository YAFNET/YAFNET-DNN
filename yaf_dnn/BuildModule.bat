@SET FrameworkSDKDir=
@SET PATH=%FrameworkDir%;%FrameworkSDKDir%;%PATH%
@SET LANGDIR=EN

.nuget\nuget.exe restore YAF.DNN.Module.sln

"C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe" YAF.DNN.Module.sln /p:Configuration=Release /t:Clean;Build /p:WarningLevel=0;CreatePackages=false /flp1:logfile=errors.txt;errorsonly %1 %2 %3 %4 %5 %6 %7 %8 %9 