﻿// ----------------------------------------------------------------------
// <copyright file="EmbeddedJsonLocalizationExtensionService.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoloX.BlazorJsonLocalization.Services.Impl
{
    /// <summary>
    /// Embedded Json Localization extension service.
    /// </summary>
    public class EmbeddedJsonLocalizationExtensionService : IJsonLocalizationExtensionService<EmbeddedJsonLocalizationOptions>
    {
        ///<inheritdoc/>
        public async ValueTask<IReadOnlyDictionary<string, string>?> TryLoadAsync(
            EmbeddedJsonLocalizationOptions options,
            Assembly assembly,
            string baseName,
            CultureInfo cultureInfo)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            var embeddedFileProvider = GetFileProvider(assembly);

            return await LoadStringMapAsync(embeddedFileProvider, options.ResourcesPath, baseName, cultureInfo)
                .ConfigureAwait(false);
        }

        private static async ValueTask<IReadOnlyDictionary<string, string>?> LoadStringMapAsync(
            IFileProvider fileProvider,
            string resourcesPath,
            string baseName,
            CultureInfo cultureInfo)
        {
            var basePath = string.IsNullOrEmpty(resourcesPath)
                ? baseName
                : Path.Combine(resourcesPath, baseName);

            IReadOnlyDictionary<string, string>? map;
            bool done;

            do
            {
                var cultureName = cultureInfo.Name;
                var path = string.IsNullOrEmpty(cultureName)
                    ? $"{basePath}.json"
                    : $"{basePath}-{cultureName}.json";

                map = await TryLoadMapAsync(fileProvider, path)
                    .ConfigureAwait(false);

                done = map != null
                    || ReferenceEquals(cultureInfo.Parent, cultureInfo);

                cultureInfo = cultureInfo.Parent;
            }
            while (!done);

            return map;
        }

        private static async ValueTask<IReadOnlyDictionary<string, string>?> TryLoadMapAsync(
            IFileProvider fileProvider,
            string path)
        {
            var fileInfo = fileProvider.GetFileInfo(path);

            if (!fileInfo.Exists)
            {
                return null;
            }

            using var stream = fileInfo.CreateReadStream();

            var map = await JsonSerializer
                .DeserializeAsync<Dictionary<string, string>>(stream)
                .ConfigureAwait(false);

            return map ?? throw new FileLoadException("Null resources");
        }

        private static IFileProvider GetFileProvider(Assembly assembly)
        {
            return new EmbeddedFileProvider(assembly);
        }
    }
}
