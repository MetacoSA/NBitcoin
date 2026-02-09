#!/bin/bash
set -euo pipefail
rm -rf "bin/release/"
dotnet pack --configuration Release --include-symbols -p:SymbolPackageFormat=snupkg
package=$(find ./bin/Release -name "*.nupkg" -type f | head -n 1)
dotnet nuget push "$package" --source "https://api.nuget.org/v3/index.json"
ver=$(basename "$package" | sed -E 's/[^.]*\.[^.]*\.([0-9]+(\.[0-9]+){1,3}).*/\1/')
git tag -a "NBitcoin.Altcoins/v$ver" -m "NBitcoin.Altcoins/$ver"
git push --tags
