# ConnectedRoot Web 🌱

Sistema de monitoreo de huertos urbanos con tecnología IoT, desarrollado con .NET 9 y Node.js.

## 🚀 Características

- **Backend .NET 9**: API REST + MVC para gestión completa de huertos
- **Simulador IoT**: Interfaz Node.js para simular sensores en tiempo real
- **Base de datos MongoDB**: Almacenamiento de lecturas y configuración
- **Dev Container**: Entorno de desarrollo completo con Docker

## 🏗️ Arquitectura

```
ConnectedRoot/              # Backend .NET MVC + API
├── Controllers/           # Controladores REST y MVC
├── Models/               # Modelos de datos
├── Services/            # Lógica de negocio
├── Views/              # Vistas Razor
└── wwwroot/           # Archivos estáticos

Simulador-ConnectedRoot/   # Simulador IoT Node.js
└── proyecto/
    ├── server.js         # API REST del simulador
    └── public/          # Interfaz web visual
        ├── Index.html   # Dashboard con gráficos
        └── JS/         # Scripts de simulación

.devcontainer/            # Configuración Docker
├── devcontainer.json    # Configuración VS Code
├── docker-compose.yml   # Servicios (MongoDB + dev)
└── Dockerfile.dev      # Imagen personalizada
```

## 🛠️ Tecnologías

### Backend (.NET)
- ASP.NET Core 9.0
- MongoDB.Driver 3.4.1
- Entity Framework Core
- Razor Pages + MVC

### Simulador (Node.js)
- Express.js 4.18.2
- Mongoose 8.0.3
- Chart.js (gráficos en tiempo real)
- Bootstrap 5.3.3

### Base de datos
- MongoDB 7.0
- Colecciones: usuarios, huertos, plantas, sensores, zonas, lecturas, alertas

## 🚀 Inicio rápido

### Con Dev Container (Recomendado)

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/T1tino/ConnectedRootWeb.git
   cd ConnectedRootWeb
   ```

2. **Abrir en VS Code**:
   ```bash
   code .
   ```

3. **Reabrir en Container**: VS Code detectará el dev container automáticamente

4. **Iniciar servicios**:
   ```bash
   # Backend .NET
   ./start-backend.sh
   
   # Simulador IoT (en otra terminal)
   ./start-simulator.sh
   ```

### Instalación manual

#### Requisitos
- .NET 9.0 SDK
- Node.js 16+
- MongoDB 7.0
- Git

#### Backend .NET
```bash
cd ConnectedRoot
dotnet restore
dotnet run --urls="http://localhost:5000;https://localhost:5001"
```

#### Simulador Node.js
```bash
cd Simulador-ConnectedRoot/proyecto
npm install
npm start
```

## 🌐 URLs de desarrollo

- **Backend ConnectedRoot**: http://localhost:5000
- **Backend HTTPS**: https://localhost:5001
- **Simulador IoT**: http://localhost:3000
- **API Lecturas**: http://localhost:3000/api/lecturas
- **MongoDB**: mongodb://appuser:apppassword@localhost:27017/connectedroot

## 📊 Funcionalidades

### Backend ConnectedRoot
- ✅ Gestión de usuarios y autenticación
- ✅ CRUD de huertos, plantas, sensores y zonas
- ✅ Dashboard con métricas y alertas
- ✅ API REST para dispositivos IoT
- ✅ Interfaz web responsiva

### Simulador IoT
- ✅ Simulación de 4 sensores (temperatura + humedad)
- ✅ Gráficos en tiempo real con Chart.js
- ✅ Interface visual con plantas animadas
- ✅ API REST para lecturas masivas
- ✅ Datos históricos pre-cargados

## 🔧 Configuración

### Variables de entorno (.env)
```env
PORT=3000
MONGODB_URI=mongodb://appuser:apppassword@localhost:27017/connectedroot
NODE_ENV=development
SENSOR_COUNT=4
UPDATE_INTERVAL=5000
```

### appsettings.json
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://appuser:apppassword@localhost:27017/connectedroot",
    "DatabaseName": "connectedroot"
  }
}
```

## 🤝 Contribuir

1. Fork del proyecto
2. Crear rama de feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit de cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## 📝 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## 👨‍💻 Autor

**T1tino** - [GitHub](https://github.com/T1tino)

---

🌱 **ConnectedRoot** - Conectando la tecnología con la naturaleza
