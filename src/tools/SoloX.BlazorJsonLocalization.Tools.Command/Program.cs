// ----------------------------------------------------------------------
// <copyright file="Program.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

namespace SoloX.BlazorJsonLocalization.Tools
{
    /// <summary>
    /// CLI Tools Generator.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Run the tools command.
        /// </summary>
        /// <returns>Error code.</returns>
        public static async Task Main(string[] args)
        {
            var generatorCommand = new GeneratorCommand();

            await generatorCommand.RunGeneratorCommandAsync(args).ConfigureAwait(false);
        }
    }
}
