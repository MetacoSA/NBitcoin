#!/bin/bash

sudo apt remove --purge dotnet*
sudo apt remove --purge aspnetcore*

rm -f /usr/bin/dotnet
sudo ln "$HOME/.dotnet/dotnet" /usr/bin/dotnet

wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh

# https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version 9.0.101 --channel LTS

[[ "${ADDITIONAL_RUNTIME:-}" ]] \
    && echo "Additional Runtime needed: $ADDITIONAL_RUNTIME" \
    && ./dotnet-install.sh --version "$ADDITIONAL_RUNTIME" --runtime dotnet --channel LTS

