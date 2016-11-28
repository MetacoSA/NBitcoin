del *.nupkg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\nStratis\nStratis.csproj" -p:Configuration=Release
cd ..\nStratis.NETCore
dotnet restore
dotnet build -c Release
cd ..\nStratis
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj"

.\GitLink.exe ".." -ignore "nStratis.portable.tests,common,nStratis.tests,build"

..\.nuget\nuGet pack nStratis.nuspec

forfiles /m *.nupkg /c "cmd /c ..\.nuget\NuGet.exe push @FILE -source https://api.nuget.org/v3/index.json"
(((dir *.nupkg).Name) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
$ver = $Matches.Item(0)
git tag -a "v$ver" -m "$ver"
git push --tags