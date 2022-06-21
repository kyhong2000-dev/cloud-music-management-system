using System.ComponentModel.DataAnnotations;

namespace MusicSystem.Models
{
    public class Artist
    {
        [Key]
        [Display(Name = "Artist ID")]
        public int ArtistID { get; set; }
        [Display(Name = "User ID")]
        public string UserID { get; set; }
        [Display(Name = "Name")]
        public string ArtistName { get; set; }
        [Display(Name = "Identification No")]
        public string ArtistIC { get; set; }
        [Display(Name = "Contact No")]
        public string ArtistContact { get; set; }
        [Display(Name = "Status")]
        public string ArtistStatus { get; set; }
        [Display(Name = "Total Earnings (RM)")]
        public decimal TotalEarning { get; set; }
    }
}
