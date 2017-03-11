del *.nupkg
&("C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe") "..\NBitcoin\NBitcoin.csproj" -p:Configuration=Release
cd ..\NBitcoin.NETCore
dotnet restore
dotnet build -c Release
cd ..\NBitcoin
&("C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe") "..\Build\Deploy.csproj"

.\GitLink.exe ".." -ignore "nbitcoin.portable.tests,common,nbitcoin.tests,build"

nuGet pack NBitcoin.nuspec

forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE -source https://api.nuget.org/v3/index.json"
(((dir *.nupkg).Name) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
$ver = $Matches.Item(0)
git tag -a "v$ver" -m "$ver"
git push --tags