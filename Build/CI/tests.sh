#!/bin/bash
set -e

: "${BUILD_ARGS:=}"

dotnet build ./NBitcoin.Tests/NBitcoin.Tests.csproj \
             $BUILD_ARGS \
             -c Release \
             -f $Framework

dotnet test ./NBitcoin.Tests/NBitcoin.Tests.csproj \
            --no-build -v n -c Release \
            -f $Framework  \
            --filter "RestClient=RestClient|RPCClient=RPCClient|Protocol=Protocol|Core=Core|UnitTest=UnitTest|Altcoins=Altcoins|PropertyTest=PropertyTest" \
            -p:ParallelizeTestCollections=false  < /dev/null