using System.ComponentModel.DataAnnotations;

namespace Library.API.Models
{
    public abstract class BookForManipulationDto
    {

        [Required(ErrorMessage = "You should fill out the title.")]
        [MaxLength(100, ErrorMessage = "The title shouldnt have more than 100 characters.")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "The description shouldnt have more than 500 characters.")]
        public virtual string Description { get; set; }
    }
}
