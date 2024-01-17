using System.ComponentModel.DataAnnotations;

namespace epmgr.Data
{
    public class MgrDesasterCall
    {
        public int Id { get; set; }

        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(6)]
        public string Pin { get; set; }

        [StringLength(255)]
        public string Announcement { get; set; }

        [StringLength(32)]
        public string Target { get; set; }
    }
}