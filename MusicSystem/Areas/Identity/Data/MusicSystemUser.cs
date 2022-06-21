using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MusicSystem.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the MusicSystemUser class
    public class MusicSystemUser : IdentityUser
    {

        [PersonalData]
        [Display(Name = "Artist Status")]
        public string ArtistStatus { get; set; }

        [PersonalData]
        [Display(Name = "Balance (RM)")]
        public decimal AccountBalance { get; set; }

        [PersonalData]
        [Display(Name = "Spendings (RM)")]
        public decimal AccountSpending { get; set; }

        [PersonalData]
        [Display(Name = "Role")]
        public string UserRole { get; set; }

    }
}
