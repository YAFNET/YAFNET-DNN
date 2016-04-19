@SET FrameworkDir=C:\Windows\Microsoft.NET\Framework\v4.0.30319
@SET FrameworkVersion=v4.0.30319
@SET FrameworkSDKDir=
@SET PATH=%FrameworkDir%;%FrameworkSDKDir%;%PATH%
@SET LANGDIR=EN

.nuget\nuget.exe restore ..\yafsrc\yetanotherforum.net.sln

msbuild.exe YAF.DNN.Module.sln /p:Configuration=Release /t:Clean;Build /p:WarningLevel=0;CreatePackages=true /flp1:logfile=errors.txt;errorsonly %1 %2 %3 %4 %5 %6 %7 %8 %9 