namespace Library.API.Models
{
    public class BookForCreationDto
    {
        // no authorId, get it from body

        public string Title { get; set; }

        public string Description { get; set; }
    }
}
