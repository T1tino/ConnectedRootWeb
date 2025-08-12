using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Lectura
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("sensorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SensorId { get; set; } = string.Empty;

        [BsonElement("fechaHora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [BsonElement("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [BsonElement("valor")]
        public double Valor { get; set; }

        [BsonElement("unidad")]
        public string Unidad { get; set; } = string.Empty;
    }
}