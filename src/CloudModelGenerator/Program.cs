﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;
using System.CommandLine;
using System;
using System.Collections.Generic;

namespace CloudModelGenerator
{
    class Program
    {
        static IConfigurationRoot Configuration { get; set; }

        const string HelpOption = "help";

        static int Main(string[] args)
        {
            var syntax = Parse(args);
            
            var unexpectedArgs = new List<string>(syntax.RemainingArguments);
            if (unexpectedArgs.Count>0)
            {
                Console.WriteLine("Invalid arguments!");
                foreach (var unexpectedArgument in unexpectedArgs)
                {
                    Console.WriteLine($"Unrecognized option '{unexpectedArgument}'");
                }
                Console.WriteLine(syntax.GetHelpText());

                return 1;
            }

            return Execute(syntax);
        }

        static ArgumentSyntax Parse(string[] args)
        {
            var result = ArgumentSyntax.Parse(args, syntax =>{

                syntax.ErrorOnUnexpectedArguments = false;

                string nullStr = null;
                syntax.DefineOption("p|projectid", ref nullStr, "Kentico Cloud Project ID.");
                syntax.DefineOption("n|namespace", ref nullStr, "-n|--namespace");
                syntax.DefineOption("o|outputdir", ref CodeGeneratorOptions.DefaultOutputDir, "Output directory for the generated files.");
                syntax.DefineOption("f|filenamesuffix", ref nullStr, "Optionally add a suffix to generated filenames (e.g., News.cs becomes News.Generated.cs).");
                syntax.DefineOption("g|generatepartials", ref CodeGeneratorOptions.DefaultGeneratePartials, "Generate partial classes for customization (if this option is set filename suffix will default to Generated).");
                syntax.DefineOption("t|withtypeprovider", ref CodeGeneratorOptions.DefaultWithTypeProvider, "Indicates whether the CustomTypeProvider class should be generated.");
                syntax.DefineOption("s|structuredmodel", ref CodeGeneratorOptions.DefaultStructuredModel, "Indicates whether the classes should be generated with types that represent structured data model.");

                syntax.ApplicationName = "content-types-generator";
            });
            return result;
        }

        static int Execute(ArgumentSyntax argSyntax)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appSettings.json", true)
                .Add(new CommandLineOptionsProvider(argSyntax.GetOptions()));

            Configuration = builder.Build();

            CodeGeneratorOptions options = new CodeGeneratorOptions();

            // Load the options from the configuration sources
            new ConfigureFromConfigurationOptions<CodeGeneratorOptions>(Configuration).Configure(options);

            // No projectId was passed as an arg or set in the appSettings.config
            if (string.IsNullOrEmpty(options.ProjectId))
            {
                Console.Error.WriteLine("Provide a Project ID!");
                Console.WriteLine(argSyntax.GetHelpText());
                return 1;
            }

            var codeGenerator = new CodeGenerator(Options.Create(options));

            codeGenerator.GenerateContentTypeModels(options.StructuredModel);

            if (options.WithTypeProvider)
            {
                codeGenerator.GenerateTypeProvider();
            }

            return 0;
        }
    }
}
