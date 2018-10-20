using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Helpers;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        // if no route on controller
        //[HttpGet("api/authors")]
        [HttpGet()]
        public IActionResult GetAuthors()
        {
            IEnumerable<Entities.Author> authorsFromRepo = _libraryRepository.GetAuthors();


            // this should be avoided
            //var authors = new List<AuthorDto>();

            //foreach (Entities.Author author in authorsFromRepo)
            //{
            //    authors.Add(new AuthorDto()
            //    {
            //        Id = author.Id,
            //        Genre = author.Genre,
            //        Name = string.Join(' ', new[] { author.FirstName, author.LastName }),
            //        Age = author.DateOfBirth.GetCurrentAge()
            //    });
            //};

            var authors = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            //return new JsonResult(authors);
            return Ok(authors);
        }

        [HttpGet("{id}")]
        public IActionResult GetAuthor(Guid id)
        {
            // cumbersome -> need global exceptions handling -- in product, generic info, in development, stacktrace
            //try
            //{
            //    throw new Exception("testing exc.");

            //    var authorFromRepo = _libraryRepository.GetAuthor(id);

            //    if (authorFromRepo == null) return NotFound();

            //    var author = AutoMapper.Mapper.Map<AuthorDto>(authorFromRepo);
            //    //return new JsonResult(author);
            //    return Ok(author);
            //}
            //catch (Exception)
            //{
            //    return StatusCode(500, "Unexpected fault. Try again later.");
            //    throw;
            //}

            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null) return NotFound();

            var author = AutoMapper.Mapper.Map<AuthorDto>(authorFromRepo);
            //return new JsonResult(author);
            return Ok(author);
        }
    }
}
