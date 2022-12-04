#!/bin/bash

# https://www.mono-project.com/download/stable/#download-lin-ubuntu
if ! mono --version &> /dev/null
then
  echo "Installing mono..."
  apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
  apt install apt-transport-https ca-certificates
  echo "deb https://download.mono-project.com/repo/ubuntu stable-xenial main" | tee /etc/apt/sources.list.d/mono-official-stable.list
  apt update

  apt install -y mono-devel

  echo
  mono --version | head -n 1
fi

if ! ./dotnet3/dotnet --version &> /dev/null
then
  echo "Installing dotnet..."
  curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
  chmod +x dotnet-install.sh
  # COULD use ./dotnet-install.sh -c 6.0 -InstallDir ./dotnet6
  ./dotnet-install.sh -c 3.1 -InstallDir ./dotnet3

  echo
  ./dotnet3/dotnet --version
fi

