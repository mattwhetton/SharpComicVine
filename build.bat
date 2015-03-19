@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)
 
set version=1.0.5-pre
REM if not "%PackageVersion%" == "" (
REM    set version=%PackageVersion%
REM )
 
set nuget=
if "%nuget%" == "" (
	set nuget=nuget
)
 
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild SharpComicVine\SharpComicVine.csproj /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=diag /nr:false
 
REM mkdir Build
REM mkdir Build\lib
REM mkdir Build\lib\net40
 
%nuget% pack "SharpComicVine\SharpComicVine.csproj"  -p Configuration="%config%" -Version %version% 
REM %nuget% pack "SharpComicVine\SharpComicVine.nuspec" -NoPackageAnalysis -verbosity detailed -o Build -Version %version% -p Configuration="%config%"