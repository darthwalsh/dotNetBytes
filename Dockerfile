# Easiest to put Dockerfile for Web/ in the root directory:
# https://github.com/dotnet/dotnet-docker/tree/main/samples/complexapp
ARG DOTNET_OS_VERSION="-alpine"
ARG DOTNET_SDK_VERSION=6.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}${DOTNET_OS_VERSION} AS build

WORKDIR /src/
COPY ./Web ./Web
COPY ./Lib ./Lib

WORKDIR /src/Web
RUN dotnet restore
RUN dotnet publish -c Release -o /app


# MAYBE add a unit test layer, once linux OS is supported: https://github.com/darthwalsh/dotNetBytes/compare/main...linux

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_SDK_VERSION}
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [ "dotnet", "Web.dll" ]
