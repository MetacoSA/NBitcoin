del *.nupkg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin\NBitcoin.csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Portable\NBitcoin.Portable(portable-net45+netcore45+wpa81+MonoAndroid1+MonoTouch1).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\NBitcoin.Android\NBitcoin.Android.csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj"

nuGet pack NBitcoin.nuspec

#forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE"
#(((dir *.nupkg).Name) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
#$ver = $Matches.Item(0)
#git tag -a "v$ver" -m "$ver"
#git push --tags