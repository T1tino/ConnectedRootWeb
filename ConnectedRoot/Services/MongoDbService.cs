using MongoDB.Driver;
using ConnectedRoot.Models;

namespace ConnectedRoot.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("MongoDB:ConnectionString").Value;
            var databaseName = configuration.GetSection("MongoDB:DatabaseName").Value;
            
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Colecciones
        public IMongoCollection<Usuario> Usuarios => 
            _database.GetCollection<Usuario>("Usuarios");

        public IMongoCollection<Huerto> Huertos => 
            _database.GetCollection<Huerto>("Huertos");

        public IMongoCollection<Zona> Zonas => 
            _database.GetCollection<Zona>("Zonas");

        public IMongoCollection<Sensor> Sensores => 
            _database.GetCollection<Sensor>("Sensores");

        public IMongoCollection<Lectura> Lecturas => 
            _database.GetCollection<Lectura>("Lecturas");

        public IMongoCollection<Alerta> Alertas => 
            _database.GetCollection<Alerta>("Alertas");

        public IMongoCollection<Planta> Plantas => 
            _database.GetCollection<Planta>("Plantas");
    }
}