﻿using Microsoft.Extensions.Localization;
using SoloX.BlazorJsonLocalization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoloX.BlazorJsonLocalization.Generator.Sample.Components
{
    [Localizer("Resources", new[] { "fr", "en" })]
    public interface IMyComponentStringLocalizer : IStringLocalizer<MyComponent>
    {
        string MyProperty { get; }

        string MyString();
        string MyStringWithArgs(int value1, string value2);
    }
}
