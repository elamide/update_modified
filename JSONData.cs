namespace update_modified
{
    public class Rootobject
    {
        public JSONData[] books { get; set; }
    }

    public class JSONData
    {
        public string cover { get; set; }
        public string[] formats { get; set; }
        public int id { get; set; }
        public DateTime last_modified { get; set; }
    }
}