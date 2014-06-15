del *.nupkg 
nuGet pack NBitcoin.csproj -Build -Properties Configuration=Release -includereferencedprojects
forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE"
(((dir *.nupkg).Name) -match "[0-9]\.[0-9]\.[0-9]\.[0-9]")
$ver = $Matches.Item(0)
git tag -a "v$ver" -m "$ver"