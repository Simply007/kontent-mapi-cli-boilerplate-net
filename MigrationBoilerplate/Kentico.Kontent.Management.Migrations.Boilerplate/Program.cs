using Kentico.Kontent.Management.Migrations.Boilerplate.Extensions;
using Kentico.Kontent.Management.Models.Shared;
using Kentico.Kontent.Management.Models.Types;
using Kentico.Kontent.Management.Models.Types.Elements;
using Kentico.Kontent.Management.Models.Types.Patch;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kentico.Kontent.Management.Migrations.Boilerplate
{
    public class Program
    {
        private static ManagementClient client;

        public static async Task<int> Main(string[] args)
        {
            var cmd = new RootCommand
            {
                new Option<string?>(new[] { "--projectId", "-p" }, "Kentico Kontent Project ID."),
                new Option<string?>(new[] { "--apiKey", "-k" }, "Kentico Kontent Management API key."),
                new Option(new[] { "--verbose", "-v" }, "Show verbose output."),

                new Command("project", "Project options.")
                {
                    new Command("validate", "Validate the project consistency.").WithHandler(nameof(ValidateProject)),
                },
                new Command("contentType", "Content type.")
                {
                    new Command("create", "Creates content type")
                    {
                        new Argument<string>("name", "Content type name."),
                    }.WithHandler(nameof(CreateContentType)),
                    new Command("addElement", "Add element to content type")
                    {
                        new Option<string>("--contentTypeId", "Content type ID"),
                        new Option<string>("--elementName", "Name of the element"),
                    }.WithHandler(nameof(AddTypeElement)),
                },
            };

            new CommandLineBuilder(cmd)
                .UseMiddleware(async (context, next) =>
                {
                    var projectId = context.ParseResult.RootCommandResult.Children
                        .First(symbolResult => symbolResult.Symbol.Name == "projectId")
                        .Tokens.First().Value;
                    var apiKey = context.ParseResult.RootCommandResult.Children
                        .First(symbolResult => symbolResult.Symbol.Name == "apiKey")
                        .Tokens.First().Value;

                    client = new ManagementClient(new ManagementOptions
                    {
                        ProjectId = projectId,
                        ApiKey = apiKey,
                    });

                    await next(context);
                })
                .Build();

            return await cmd.InvokeAsync(args);
        }

        private static async Task<int> ValidateProject(IConsole console)
        {
            var result = await client.ValidateProjectAsync();

            console.Out.Write($"{result.Project.Name} does {(result.VariantIssues.Count == 0 ? "not have any issues" : $"have {result.VariantIssues.Count} issues")}.");
            return 0;
        }

        private static async Task<int> CreateContentType(IConsole console, string name)
        {
            var result = await client.CreateContentTypeAsync(new ContentTypeCreateModel
            {
                Name = name,
                Elements = Array.Empty<ElementMetadataBase>()
            });

            console.Out.Write($"Created content type: {result.Name} {result.Id}");

            return 0;
        }

        private static async Task<int> AddTypeElement(IConsole console, string contentTypeId, string elementName)
        {
            await client.ModifyContentTypeAsync(Reference.ById(Guid.Parse(contentTypeId)), new[]{
                new ContentTypeAddIntoPatchModel()
                {
                    Path = "/elements",
                    Value = new TextElementMetadataModel
                    {
                        Name = elementName
                    }
                }
            });

            return 0;
        }
    }
}
