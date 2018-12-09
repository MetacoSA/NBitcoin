rm "bin\release\" -Recurse -Force
dotnet pack --configuration Release  --include-symbols -p:SymbolPackageFormat=snupkg
$package=(ls .\bin\Release\*.nupkg).FullName
dotnet nuget push $package --source "https://api.nuget.org/v3/index.json"
$ver = ((ls .\bin\release -File)[0].Name -replace '([^\.\d]*\.)+(\d+(\.\d+){1,3}).*', '$2')
git tag -a "NBitcoin.TestFramework/v$ver" -m "NBitcoin.TestFramework/$ver"
git push --tags