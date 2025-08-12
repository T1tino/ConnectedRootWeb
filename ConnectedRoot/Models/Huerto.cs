using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Huerto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("nombreHuerto")]
        public string NombreHuerto { get; set; } = string.Empty;
        
        [BsonElement("ubicacion")]
        public string Ubicacion { get; set; } = string.Empty;
        
        [BsonElement("responsableId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ResponsableId { get; set; }
        
        [BsonElement("fechaRegistro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        
        [BsonElement("estado")]
        public string Estado { get; set; } = "activo";
        
        // Propiedad para el lookup (no se guarda en MongoDB)
        [BsonIgnore]
        public Usuario? Responsable { get; set; }
    }
}