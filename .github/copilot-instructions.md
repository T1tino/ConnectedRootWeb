# Copilot Instructions for ConnectedRoot Web

## Project Overview
ConnectedRoot Web is an urban garden monitoring system using IoT, built with .NET 9 (ASP.NET Core MVC + REST API) and Node.js (IoT simulator). Data is stored in MongoDB. The project is containerized for development using Docker and VS Code Dev Containers.

## Architecture
- **ConnectedRoot/**: .NET backend (MVC, REST API)
  - `Controllers/`: REST and MVC controllers
  - `Models/`: Data models for MongoDB
  - `Services/`: Business logic and MongoDB access
  - `Views/`: Razor views
  - `wwwroot/`: Static assets
- **Simulador-ConnectedRoot/proyecto/**: Node.js IoT simulator
  - `server.js`: REST API for simulated sensor data
  - `public/`: Web dashboard and simulation scripts
- **MongoDB**: Stores all domain data (users, gardens, sensors, readings, alerts)

## Key Workflows
- **Build/Run Backend**: Use `dotnet run` in `ConnectedRoot/` or VS Code tasks (`build`, `publish`, `watch`).
- **Run Simulator**: Use `npm start` in `Simulador-ConnectedRoot/proyecto/`.
- **Dev Container**: Open in VS Code, re-open in container, run `./start-backend.sh` and `./start-simulator.sh`.
- **MongoDB Connection**: Connection string and database name are set in `appsettings.json` and `.env` files.

## Patterns & Conventions
- **MongoDB Access**: All DB operations are handled via services in `ConnectedRoot/Services/` (e.g., `MongoDbService.cs`). Models in `ConnectedRoot/Models/` map to MongoDB collections.
- **Controllers**: REST endpoints are in `Controllers/`, typically using dependency injection for services.
- **Views**: Razor views in `Views/` follow MVC conventions.
- **Simulador**: Uses Express.js and Mongoose for simulated sensor data, with REST endpoints for batch data insertions.
- **Environment Config**: Use `.env` for Node.js and `appsettings.json` for .NET. MongoDB credentials are shared across both.

## Integration Points
- **IoT Simulator → Backend**: Simulator sends sensor data to backend via REST API endpoints.
- **Backend → MongoDB**: All CRUD operations for domain entities are persisted in MongoDB.
- **Frontend**: Razor views and static assets served from `ConnectedRoot/wwwroot/`.

## Examples
- **MongoDB Service**: See `ConnectedRoot/Services/MongoDbService.cs` for DB access patterns.
- **Sensor Data Flow**: Simulator (`server.js`) posts readings to backend API, which stores them in MongoDB.
- **Config**: MongoDB connection string in `ConnectedRoot/appsettings.json` and simulator `.env`.

## Troubleshooting
- If data is not inserted into MongoDB:
  - Check connection strings in config files
  - Ensure MongoDB is running and accessible
  - Review service logic in `MongoDbService.cs` and controller usage
  - Check for errors in backend and simulator logs

---
For more details, see `README.md` and key files referenced above. Please update this file if major architectural changes are made.
