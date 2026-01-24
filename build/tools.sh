#!/usr/bin/env bash

set -eou pipefail

# function to print error messages
error() {
	echo "Error: $1" >&2
	exit 1
}

# use the directory where the script is called as the root directory
ROOT_DIR="$(pwd)"

# checks if dotnet is installed
if ! command -v dotnet &> /dev/null; then
	error "dotnet CLI is not installed. Please install it from https://dotnet.microsoft.com/download"
fi

# ensure tool-manifest exists
if [ ! -f "$ROOT_DIR/.config/dotnet-tools.json" ]; then
	echo "Creating tool-manifest in $ROOT_DIR..."
	dotnet new tool-manifest --output "$ROOT_DIR" > /dev/null
fi

# install cake.tool as a local tool if not already installed
if ! dotnet tool list --local | grep -q "cake.tool"; then
	echo "Installing Cake.Tool in $ROOT_DIR..."
	dotnet tool install Cake.Tool --tool-manifest "$ROOT_DIR/.config/dotnet-tools.json"
else
	echo "Cake.Tool is already installed in $ROOT_DIR."
fi

# install dotnet-reportgenerator-globaltool as a local tool if not already installed
if ! dotnet tool list --local | grep -q "dotnet-reportgenerator-globaltool"; then
	echo "Installing dotnet-reportgenerator-globaltool in $ROOT_DIR..."
	dotnet tool install dotnet-reportgenerator-globaltool --tool-manifest "$ROOT_DIR/.config/dotnet-tools.json"
else
	echo "ReportGenerator is already installed in $ROOT_DIR."
fi