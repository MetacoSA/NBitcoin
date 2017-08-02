rm "bin\release\" -Recurse -Force
dotnet pack HashLib.csproj --configuration Release
dotnet nuget push "bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

