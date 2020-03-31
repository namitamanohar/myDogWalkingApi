using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyDogWalkingAPI.Models
{
    public class Dog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 2)]

        public string Name { get; set; }

        public string Breed { get; set; }
        public string Notes { get; set; }
       
        [Required]
        public int OwnerId { get; set; }

        public Owner Owner { get; set; }
    }
}
