namespace DHConsulting.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Linq;
    using System.Text.Json.Serialization;

    [Table("Cliente")]
    public partial class Cliente
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Cliente()
        {
            Ordine = new HashSet<Ordine>();
        }

        [Key]
        public int IdCliente { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome { get; set; }

        [Required]
        [StringLength(50)]
        public string Cognome { get; set; }

        [Column(TypeName = "date")]
        [MinAge(18, ErrorMessage = "Devi avere almeno 18 anni")]
        [Display(Name = "Data di nascita")]
        public DateTime DataNascita { get; set; }

        [Required]
        [StringLength(100)]
        public string Indirizzo { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Città")]
        public string Citta { get; set; }

        [StringLength(16)]
        [RegularExpression("^[A-Za-z]{6}\\d{2}[A-Za-z]\\d{2}[A-Za-z]\\d{3}[A-Za-z]$", ErrorMessage = "CF non valido")]
        public string CF { get; set; }

        [StringLength(11)]
        [Display(Name = "P.IVA")]
        public string Piva { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        [PasswordRequirement(ErrorMessage = "La password deve contenere almeno 8 caratteri, una lettera minuscola, una lettera maiuscola, un numero e un carattere speciale tra .!?@&$%")]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$", ErrorMessage = "Email non valida")]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Telefono")]
        [MinLength(10, ErrorMessage = "Inserisci un numero di cellulare valido (compreso il prefisso)")]
        public string Phone { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ordine> Ordine { get; set; }
    }

    public class MinAgeAttribute : ValidationAttribute
    {
        private int _minAge;

        public MinAgeAttribute(int minAge)
        {
            _minAge = minAge;
        }

        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                var today = DateTime.Today;
                var age = today.Year - date.Year;

                if (date.Date > today.AddYears(-age)) age--;

                return age >= _minAge;
            }

            return false;
        }
    }

    public class PasswordRequirementAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var password = value as string;

            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < 8)
            {
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                return false;
            }

            if (!password.Any(c => ".!?@&$%".Contains(c)))
            {
                return false;
            }

            return true;
        }
    }
}
