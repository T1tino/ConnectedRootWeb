using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Zona
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("huertoId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string HuertoId { get; set; } = string.Empty;
        
        [BsonElement("nombreZona")]
        public string NombreZona { get; set; } = string.Empty;
        
        [BsonElement("descripcion")]
        public string Descripcion { get; set; } = string.Empty;
        
        [BsonElement("coordenadas")]
        public Coordenadas? Coordenadas { get; set; }
        
        [BsonElement("tipoZona")]
        public string TipoZona { get; set; } = string.Empty;
        
        [BsonElement("estado")]
        public string Estado { get; set; } = "activa";
        
        // Propiedad para el lookup (no se guarda en MongoDB)
        [BsonIgnore]
        public Huerto? Huerto { get; set; }
    }

    public class Coordenadas
    {
        [BsonElement("latitud")]
        public double Latitud { get; set; }
        
        [BsonElement("longitud")]
        public double Longitud { get; set; }
    }
}