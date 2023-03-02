using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonauMorgen.NEXTFI.Web.Models
{
    [Table("ScheduleForeman")]
    public class ScheduleForemanModel
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public int Counter { get; set; }
        public int? IsActive { get; set; }
        public int? IsActiveManager { get; set; }
        public int? IsActiveFinance { get; set; }
        public int? IsActiveAdmin { get; set; }
        public int? IsActiveMaster { get; set; }

    }
}