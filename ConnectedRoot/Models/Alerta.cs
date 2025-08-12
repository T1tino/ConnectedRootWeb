using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Alerta
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [BsonElement("zonaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ZonaId { get; set; } = string.Empty;

        [BsonElement("valorRegistrado")]
        public double ValorRegistrado { get; set; }

        [BsonElement("umbralMinimo")]
        public double UmbralMinimo { get; set; }

        [BsonElement("umbralMaximo")]
        public double UmbralMaximo { get; set; }

        [BsonElement("valorUmbralViolado")]
        public double ValorUmbralViolado { get; set; }

        [BsonElement("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [BsonElement("fechaHora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [BsonElement("estado")]
        public string Estado { get; set; } = "pendiente";

        [BsonElement("enviada")]
        public bool Enviada { get; set; } = false;
    }
}