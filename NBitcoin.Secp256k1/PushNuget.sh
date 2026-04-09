#!/bin/bash
set -euo pipefail
rm -rf "bin/Release"
dotnet pack --configuration Release --include-symbols -p:SymbolPackageFormat=snupkg
package=$(find ./bin/Release -name "*.nupkg" -type f | head -n 1)
dotnet nuget push "$package" --source "https://api.nuget.org/v3/index.json" --api-key "$NUGET_API_KEY"
ver=$(basename "$package" | sed -E 's/.*\.([0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)*).*/\1/')
git tag -a "NBitcoin.Secp256k1/v$ver" -m "NBitcoin.Secp256k1/$ver"
git push --tags
