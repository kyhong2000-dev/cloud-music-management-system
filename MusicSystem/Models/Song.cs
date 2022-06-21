using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicSystem.Models
{
    public class Song
    {
        public int songId { get; set; }

        [StringLength(25, ErrorMessage = "The song name should be between 1 - 25 characters!", MinimumLength = 1)]
        [Display(Name = "Song")]
        [Required]
        public string songName { get; set; }

        [Display(Name = "Artist")]
        public string artistName { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Required]
        [Display(Name = "Duration")]
        public decimal duration { get; set; }

        [StringLength(30, ErrorMessage = "The album name should be between 5 - 30 characters!", MinimumLength = 5)]
        [Display(Name = "Album")]
        [Required]
        public String albumName { get; set; }

        [Display(Name = "Release Date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime releasedDate { get; set; }

        [Range(0, 1000)]
        [Column(TypeName = "decimal(30,2)")]
        [Required]
        [Display(Name = "Cost (RM)")]
        public decimal songCost { get; set; }

        [Display(Name = "Downloads")]
        public int songDownload { get; set; }

        [Column(TypeName = "decimal(30,2)")]
        [Display(Name = "Total Earning (RM)")]
        public decimal totalEarning { get; set; }

        [Display(Name = "Song File Name")]
        public string songFileName { get; set; }

    }
}
