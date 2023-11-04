namespace DHConsulting.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Utente")]
    public partial class Utente
    {
        [Key]
        public int IdUtente { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(50)]
        public string Role { get; set; }

        public int? FailedLoginAttempts { get; set; }

        public DateTime? LockoutEndTime { get; set; }

        public bool Confirmed { get; set; }

        public byte[] Token { get; set; }
    }
}
