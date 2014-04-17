del *.nupkg 
nuGet pack NBitcoin.csproj -Build -Properties Configuration=Release
forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE