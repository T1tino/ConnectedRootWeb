# Imagen base de .NET 9 + SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base

# Instalar Node.js (para el simulador)
RUN apt-get update && apt-get install -y curl gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Crear usuario vscode (como en las imágenes oficiales de devcontainer)
RUN useradd -ms /bin/bash vscode
USER vscode

WORKDIR /workspace
