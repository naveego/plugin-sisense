namespace Plugin_Naveego_Legacy.DataContracts
{
    public class Field
    {
        public string api_name   { get; set; }
        public string data_type    { get; set; }
        public string field_label  { get; set; }
        public string json_type    { get; set; }
        public int    length       { get; set; }
    }

    public class FieldsResponse
    {
        public Field[] fields { get; set; }
    }
}