using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConnectedRoot.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [BsonElement("primerApellido")]
        public string PrimerApellido { get; set; } = string.Empty;

        [BsonElement("segundoApellido")]
        public string SegundoApellido { get; set; } = string.Empty;

        [BsonElement("correo")]
        public string Correo { get; set; } = string.Empty;

        [BsonElement("contraseña")]
        public string Contraseña { get; set; } = string.Empty;

        [BsonElement("rol")]
        public string Rol { get; set; } = string.Empty;

        [BsonElement("huertosAsignadosIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> HuertosAsignadosIds { get; set; } = new();

        [BsonElement("activo")]
        public bool Activo { get; set; } = true;
    }
}