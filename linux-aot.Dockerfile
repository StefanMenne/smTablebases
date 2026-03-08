# Nutze das offizielle .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env

# Installiere Toolchain
RUN apt-get update && apt-get install -y \
    clang zlib1g-dev libkrb5-dev libicu-dev build-essential

WORKDIR /src

COPY smTablebases .

RUN dotnet publish smTablebases/smTablebases.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishAot=true \
    --property WarningLevel=0 \
    -p:DefineConstants='RELEASE%3BRELEASEFINAL' \
    -o /out
    
    
    