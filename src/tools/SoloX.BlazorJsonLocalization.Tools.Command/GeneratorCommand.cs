// ----------------------------------------------------------------------
// <copyright file="GeneratorCommand.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using SoloX.BlazorJsonLocalization.Tools.Core;
using SoloX.BlazorJsonLocalization.Tools.Extensions;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace SoloX.BlazorJsonLocalization.Tools
{
    /// <summary>
    /// Generator command.
    /// </summary>
    internal sealed class GeneratorCommand
    {
        private readonly RootCommand rootCommand = new RootCommand("Localization generation command.");

        private readonly Argument<FileInfo> projectArgument = new Argument<FileInfo>("projectFile", "CS Project to run generation.");
        private readonly Option<FileInfo> outputCodeOption = new Option<FileInfo>("--outputCode", "File to store CS output file list.");
        private readonly Option<FileInfo> outputResourceOption = new Option<FileInfo>("--outputResource", "File to store Json output resource file list.");
        private readonly Option<FileInfo> logFileOption = new Option<FileInfo>("--logFile", "File to write logging.");
        private readonly Option<bool> logDebugOption = new Option<bool>("--logDebug", "Enable debug logging.");
        private readonly Option<bool> useRelaxedJsonEscaping = new Option<bool>("--useRelaxedJsonEscaping", "Use Relaxed Json Escaping.");
        private readonly Option<bool> useMultiLine = new Option<bool>("--useMultiLine", "Use Multi-Line Json resources.");

        /// <summary>
        /// Setup GeneratorCommand instance.
        /// </summary>
        public GeneratorCommand()
        {
            this.rootCommand.AddGlobalOption(this.logFileOption);
            this.rootCommand.AddGlobalOption(this.logDebugOption);

            this.rootCommand.AddArgument(this.projectArgument);
            this.rootCommand.AddOption(this.outputCodeOption);
            this.rootCommand.AddOption(this.outputResourceOption);
            this.rootCommand.AddOption(this.useRelaxedJsonEscaping);
            this.rootCommand.AddOption(this.useMultiLine);

            this.rootCommand.SetHandler(RunGeneratorCommandHandlerAsync);
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        /// <param name="args">Command arguments.</param>
        /// <returns></returns>
        public async Task RunGeneratorCommandAsync(string[] args)
        {
            await this.rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        /// <summary>
        /// Run command
        /// </summary>
        private Task RunGeneratorCommandHandlerAsync(InvocationContext invocationContext)
        {
            return SetupServiceCollectionAndRunCommandAsync(invocationContext, RunGeneratorCommandHandlerAsync);
        }

        private async Task SetupServiceCollectionAndRunCommandAsync(InvocationContext invocationContext, Func<InvocationContext, IServiceProvider, Task> commandHandler)
        {
            var serviceCollection = new ServiceCollection();

            var logDebug = invocationContext.BindingContext.ParseResult.GetValueForOption(this.logDebugOption);

            var logFile = invocationContext.BindingContext.ParseResult.GetValueForOption(this.logFileOption);
            if (logFile != null)
            {
                var fileLoggerConf = new LoggerConfiguration()
                    .WriteTo
                    .File(logFile.FullName, formatProvider: CultureInfo.InvariantCulture, rollOnFileSizeLimit: true, fileSizeLimitBytes: 5 * 1_024 * 1_024);

                if (logDebug)
                {
                    fileLoggerConf
                        .MinimumLevel
                        .Debug();
                }

                using var fileLogger = fileLoggerConf
                    .CreateLogger();

                using var loggerFactory = LoggerFactory.Create(
                    b =>
                    {
                        b.ClearProviders();
                        b.AddSerilog(fileLogger);
                    });

                serviceCollection.AddToolsGenerator(loggerFactory);

                var serviceProvider = serviceCollection.BuildServiceProvider();
                await using var _ = serviceProvider.ConfigureAwait(false);

                await commandHandler(invocationContext, serviceProvider).ConfigureAwait(false);
            }
            else
            {
                serviceCollection.AddLogging(b =>
                {
                    b.AddConsole(opt =>
                    {
                        opt.FormatterName = BasicConsoleFormatter.Name;
                    })
                    .AddConsoleFormatter<BasicConsoleFormatter, ConsoleFormatterOptions>();

                    if (logDebug)
                    {
                        b.SetMinimumLevel(LogLevel.Debug);
                    }
                });

                serviceCollection.AddToolsGenerator();

                var serviceProvider = serviceCollection.BuildServiceProvider();
                await using var _ = serviceProvider.ConfigureAwait(false);

                await commandHandler(invocationContext, serviceProvider).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run generator command.
        /// </summary>
        private async Task RunGeneratorCommandHandlerAsync(InvocationContext invocationContext, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<GeneratorCommand>>();

            var projectFile = invocationContext.BindingContext.ParseResult.GetValueForArgument(this.projectArgument);

            if (!projectFile.Exists)
            {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
                logger.LogError("Could not find project file {ProjectFile}", projectFile);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
                invocationContext.ExitCode = -1;
                return;
            }

            var useRelaxedJsonEscapingOptionValue = invocationContext.BindingContext.ParseResult.GetValueForOption(this.useRelaxedJsonEscaping);

            var useMultiLineOptionValue = invocationContext.BindingContext.ParseResult.GetValueForOption(this.useMultiLine);

            var generator = serviceProvider.GetRequiredService<ILocalizationGenerator>();
            var results = generator.Generate(projectFile.FullName, new GeneratorOptions(useRelaxedJsonEscapingOptionValue, useMultiLineOptionValue));

            var full = projectFile.Directory?.FullName ?? Environment.CurrentDirectory;

            var outputResourceFile = invocationContext.BindingContext.ParseResult.GetValueForOption(this.outputResourceOption);
            if (outputResourceFile != null)
            {
                await File.WriteAllLinesAsync(outputResourceFile.FullName, results.GeneratedResourceFiles.Select(f => Path.GetRelativePath(full, f))).ConfigureAwait(false);
            }

            var outputCodeFile = invocationContext.BindingContext.ParseResult.GetValueForOption(this.outputCodeOption);
            if (outputCodeFile != null)
            {
                await File.WriteAllLinesAsync(outputCodeFile.FullName, results.GeneratedCodeFiles.Select(f => Path.GetRelativePath(full, f))).ConfigureAwait(false);
            }

#pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger.LogInformation($"Generation competed.");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        }
    }

#pragma warning disable CA1812
    /// <summary>
    /// Basic console formatter.
    /// </summary>
    internal sealed class BasicConsoleFormatter : ConsoleFormatter
    {
        /// <summary>
        /// Formatter name.
        /// </summary>
        public const string Name = "basic_console";

        /// <summary>
        /// Setup formatter instance.
        /// </summary>
        public BasicConsoleFormatter()
            : base(Name)
        {
        }

        /// <inheritdoc/>
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
#pragma warning disable IDE0072 // Add missing cases
            var prefix = logEntry.LogLevel switch
            {
                LogLevel.Critical => "Critical: ",
                LogLevel.Error => "Error: ",
                LogLevel.Warning => "Warning: ",
                _ => string.Empty,
            };
#pragma warning restore IDE0072 // Add missing cases

            var txt = prefix + logEntry.Formatter(logEntry.State, logEntry.Exception);

#pragma warning disable CA1062 // Validate arguments of public methods
            textWriter.WriteLine(txt);
#pragma warning restore CA1062 // Validate arguments of public methods
        }
    }
#pragma warning restore CA1812

}
