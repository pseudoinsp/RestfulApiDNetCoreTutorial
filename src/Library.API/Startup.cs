using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Library.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Serialization;
using Marvin.Cache.Headers;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;

                // default outputformatter: the first one in this list
                // it will be used if no accept-header was specified
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                //setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter(new MvcOptions()));

                var xmlDataContractSerializer = new XmlDataContractSerializerInputFormatter(new MvcOptions());
                xmlDataContractSerializer.SupportedMediaTypes.Add("application/vnd.marvin.authorwithdateofdeath.full+xml");
                setupAction.InputFormatters.Add(xmlDataContractSerializer);

                var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();

                if (jsonInputFormatter != null)
                {
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.author.full+json");
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.authorwithtimeofdeath.full+json");
                }


                var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();

                if (jsonOutputFormatter != null)
                {
                    jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
                }
            }) 
            // the default contract resolver serializes the data "as-is", and now we use ExpandoObject for data repr.
            // before the serialization, whose dictionary'keys are not camel case.
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddScoped<IUrlHelper, UrlHelper>(implementationFactory =>
            {
                var actionContext = implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                return new UrlHelper(actionContext);
            });

            // lightweight, stateless ->transient
            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<ITypeHelperService, TypeHelperService>();

            services.AddHttpCacheHeaders(
                (ExpirationModelOptions expirationModelOptions)
                =>
                {
                    expirationModelOptions.MaxAge = 600;
                },
                (ValidationModelOptions validationModelOptions)
                =>
                {
                    validationModelOptions.MustRevalidate = true;
                });

            services.AddResponseCaching();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole();

            loggerFactory.AddDebug(LogLevel.Information);

            // in .netCore 2, logging can be added earlier, in the bootstrap phase!
            //loggerFactory.AddNLog();
            //or loggerFactory.AddProvider(new NLog.Extensions.Logging.NLogLoggerProvider());
            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature != null)
                        {
                            var logger = loggerFactory.CreateLogger("Global exception logger");
                            logger.LogError(500, exceptionHandlerFeature.Error,
                                exceptionHandlerFeature.Error.Message);
                        }
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unexpected fault. Try again later.");
                    });
                });
            }

            // AutoMapper: map two objects together  -- now, map Author to AuthorDto
            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Author, AuthorDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge(src.DateOfDeath)));

                cfg.CreateMap<Book, BookDto>();

                cfg.CreateMap<AuthorForCreationDto, Author>();

                cfg.CreateMap<AuthorForCreationWithDateOfDeathDto, Author>();

                cfg.CreateMap<BookForCreationDto, Book>();

                cfg.CreateMap<BookForUpdateDto, Book>();

                cfg.CreateMap<Book, BookForUpdateDto>();
            });

            libraryContext.EnsureSeedDataForContext();

            app.UseResponseCaching();

            // before MVC middleware!
            app.UseHttpCacheHeaders();

            app.UseMvc();
        }
    }
}
