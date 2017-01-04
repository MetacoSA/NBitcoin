del *.nupkg

## build the project
C:\"Program Files (x86)"\MSBuild\14.0\Bin\msbuild.exe  "..\nStratis\nStratis.csproj" -p:Configuration=Release

## build in netcore
cd ..\nStratis.NETCore
dotnet restore
dotnet build -c Release

## create the nuspec file
cd ..\nStratis
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj"

## enable debug in to the package by including the .pdb files
.\GitLink.exe ".." -ignore "nStratis.portable.tests,common,nStratis.tests,build"

## package the code
..\.nuget\nuGet pack nStratis.nuspec -Symbols

## push to nuget
forfiles /m *.nupkg /c "cmd /c ..\.nuget\NuGet.exe push @FILE -source https://api.nuget.org/v3/index.json"

## publish a tag to git
#(((dir *.nupkg).Name) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
#$ver = $Matches.Item(0)
#git tag -a "v$ver" -m "$ver"
#git push --tags