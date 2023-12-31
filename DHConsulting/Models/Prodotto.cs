namespace DHConsulting.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Text.Json.Serialization;

    [Table("Prodotto")]
    public partial class Prodotto
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Prodotto()
        {
            Dettaglio = new HashSet<Dettaglio>();
        }

        [Key]
        public int IdProdotto { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Titolo")]
        public string DescrizioneBreve { get; set; }

        [Required]
        [Display(Name = "Descrizione")]
        public string DescrizioneLunga { get; set; }

        public decimal Costo { get; set; }

        [Display(Name = "Costo scontato")]
        public decimal? CostoScontato { get; set; }

        [StringLength(50)]
        [Display(Name = "Immagine")]
        public string Image { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Dettaglio> Dettaglio { get; set; }
    }
}
