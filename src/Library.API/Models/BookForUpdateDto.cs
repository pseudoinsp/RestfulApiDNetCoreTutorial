namespace Library.API.Models
{
    public class BookForUpdateDto
    {
        // id is provided in the HttpPut uri
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
