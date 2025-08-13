#!/bin/bash
set -e

echo "Restaurando dependencias .NET..."
dotnet restore ConnectedRoot/ConnectedRoot.csproj

echo "Instalando dependencias del simulador (Simulador/proyecto)..."
cd Simulador/proyecto
npm install
cd ../..

echo "Instalando dependencias raíz (para scripts npm)..."
npm install

echo "¡Entorno listo! Usa 'npm run start:all' para iniciar backend y simulador."
