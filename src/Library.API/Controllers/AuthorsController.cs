using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

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
            IEnumerable<Author> authorsFromRepo = _libraryRepository.GetAuthors();


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

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            //return new JsonResult(authors);
            return Ok(authors);
        }

        [HttpGet("{id}", Name = "GetAuthor")]
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

            var author = Mapper.Map<AuthorDto>(authorFromRepo);
            //return new JsonResult(author);
            return Ok(author);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto authorForCreation)
        {
            if (authorForCreation == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(authorForCreation);

            _libraryRepository.AddAuthor(authorEntity);

            if (!_libraryRepository.Save())
            {
                // if we handle it like this, exception creation has a cost, but the handling is solved genericly by the middleware
                throw new Exception("Creating an authord failed to save.");

                // if we handle like this, this code will be duplicated in some places, instead of handling it by the middleware
                // but no exeption creation is necessary
                //return StatusCode(500, "A problem occurred");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_libraryRepository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null) return NotFound();

            _libraryRepository.DeleteAuthor(authorFromRepo);

            if (!_libraryRepository.Save())
            {
                
                throw new Exception("Deleting an authord failed to save.");
            }

            return NoContent();
        }
    }
}
