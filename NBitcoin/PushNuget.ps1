del *.nupkg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin\NBitcoin.csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Portable\NBitcoin.Portable(Profile111).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Portable\NBitcoin.Portable(Profile259).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Portable\NBitcoin(MonoAndroid).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Portable\NBitcoin(Mono).csproj" -p:Configuration=Release
cd ..\NBitcoin.NETCore
dotnet restore
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj" /t:SetCoreVersion
dotnet build -c Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj" /t:SetCoreVersionReverse
cd ..\NBitcoin
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj"

.\GitLink.exe ".." -ignore "nbitcoin.portable.tests,common,nbitcoin.tests,newtonsoft.json.portable"

nuGet pack NBitcoin.nuspec
nuGet pack NBitcoin.Mono.nuspec

forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE"
(((dir *.nupkg).Name).Item(0) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
$ver = $Matches.Item(0)
git tag -a "v$ver" -m "$ver"
git push --tags