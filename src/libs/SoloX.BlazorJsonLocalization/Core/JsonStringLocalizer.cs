﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;

namespace SoloX.BlazorJsonLocalization.Core
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        public IFileProvider FileProvider { get; }
        public string Name { get; }
        public string Resources { get; }

        public JsonStringLocalizer(IFileProvider fileProvider, string resources, string name)
        {
            FileProvider = fileProvider;
            Resources = resources;
            Name = name;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public LocalizedString this[string name]
        {
            get
            {
                var stringMap = LoadStringMap();

                return new LocalizedString(name, stringMap[name]);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var stringMap = LoadStringMap();

                return new LocalizedString(name, string.Format(stringMap[name], arguments));
            }
        }

        private Dictionary<string, string> LoadStringMap()
        {
            var cultureInfo = CultureInfo.CurrentUICulture;

            var cultureName = cultureInfo.TwoLetterISOLanguageName;

            var fileInfo =
                FileProvider.GetFileInfo(
                    Path.Combine(Resources, $"{Name}-{cultureName}.json"));

            if (!fileInfo.Exists)
            {
                fileInfo =
                    FileProvider.GetFileInfo(
                        Path.Combine(Resources, $"{Name}.json"));
            }

            using var stream = fileInfo.CreateReadStream();

            return JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream).Result;
        }
    }
}
