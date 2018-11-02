using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
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
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, IPropertyMappingService propertyMappingService, ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        // if no route on controller
        //[HttpGet("api/authors")]
        //public IActionResult GetAuthors([FromQuery()] int pageNumber = 1, [FromQuery] int pageSize = 10)
        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }


            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
                   CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;

            var paginationMetadaa = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadaa));

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors.ShapeData(authorsResourceParameters.Fields));
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber - 1,
                          pageSize = authorsResourceParameters.PageSize
                      });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber + 1,
                          pageSize = authorsResourceParameters.PageSize
                      });

                default:
                    return _urlHelper.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        searchQuery = authorsResourceParameters.SearchQuery,
                        genre = authorsResourceParameters.Genre,
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize
                    });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor([FromRoute] Guid id, [FromQuery] string fields)
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

            if(!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null) return NotFound();

            var author = Mapper.Map<AuthorDto>(authorFromRepo);
            //return new JsonResult(author);
            return Ok(author.ShapeData(fields));
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
