git clone https://github.com/couchbasedeps/dotnet-tpl35
cd dotnet-tpl35 
git checkout 7f3473625bca33586ae1b74cc75945ed582def47
cd ..
msbuild NBitcoin/NBitcoin.csproj /p:Configuration=Release