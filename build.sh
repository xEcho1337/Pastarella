#!/bin/bash

mkdir -p out

cd src/Pastarella || exit 1

echo "Building..."

dotnet publish -c Release -r win-arm64   -o ../../out/win-arm64
dotnet publish -c Release -r win-x64     -o ../../out/win-x64
dotnet publish -c Release -r osx-arm64   -o ../../out/osx-arm64
dotnet publish -c Release -r linux-x64   -o ../../out/linux-x64
dotnet publish -c Release -r linux-arm64 -o ../../out/linux-arm64

echo "Done!"
