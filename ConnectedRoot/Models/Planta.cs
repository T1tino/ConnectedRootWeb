using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Planta
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("zonaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ZonaId { get; set; } = string.Empty;

        [BsonElement("nombreCientifico")]
        public string NombreCientifico { get; set; } = string.Empty;

        [BsonElement("nombreComun")]
        public string NombreComun { get; set; } = string.Empty;

        [BsonElement("fechaSiembra")]
        public DateTime FechaSiembra { get; set; } = DateTime.Now;

        [BsonElement("tipoCultivo")]
        public string TipoCultivo { get; set; } = string.Empty;

        [BsonElement("estado")]
        public string Estado { get; set; } = string.Empty;

        [BsonElement("notas")]
        public string Notas { get; set; } = string.Empty;
    }
}