using LiteDB;

namespace Plugin_Sisense.DataContracts
{
    public class ShapeNameObject
    {
        [BsonId]
        public string ShapeName { get; set; }
    }
}