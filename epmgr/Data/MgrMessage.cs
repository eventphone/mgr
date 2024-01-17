using System.ComponentModel.DataAnnotations.Schema;

namespace epmgr.Data
{
    public class MgrMessage
    {
        public int Id { get; set; }

        public long Timestamp { get; set; }

        public string Type { get; set; }

        [Column(TypeName = "jsonb")]
        public string Json { get; set; }

        public bool Failed { get; set; }

        public string Error { get; set; }
    }
}