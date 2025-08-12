using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Sensor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("zonaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ZonaId { get; set; } = string.Empty;

        [BsonElement("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [BsonElement("modelo")]
        public string Modelo { get; set; } = string.Empty;

        [BsonElement("estado")]
        public string Estado { get; set; } = "activo";

        [BsonElement("fechaInstalacion")]
        public DateTime FechaInstalacion { get; set; } = DateTime.Now;

        [BsonElement("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        // Navigation properties (no almacenadas en MongoDB)
        [BsonIgnore]
        public Zona? Zona { get; set; }
    }
}