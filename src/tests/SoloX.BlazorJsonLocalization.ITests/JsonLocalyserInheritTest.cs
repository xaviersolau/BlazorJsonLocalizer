// ----------------------------------------------------------------------
// <copyright file="JsonLocalyserInheritTest.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using SoloX.BlazorJsonLocalization.ITests.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SoloX.BlazorJsonLocalization.ITests
{
    public class JsonLocalyserInheritTest
    {
        private readonly ITestOutputHelper testOutputHelper;
        public JsonLocalyserInheritTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("en-US", "Global", "This is global message...")]
        [InlineData("en-US", "Specific", "This is specific message...")]
        public Task ItShouldSetupEmbeddedLocalizerUsingInheritedJsonResourcesOnInterfaces(string cultureName, string key, string expected)
        {
            return SetupHelper.ProcessLocalizerTestAsync<ISpecific>(
                cultureName,
                localizer =>
                {
                    Assert.NotNull(localizer);

                    var localized = localizer[key];
                    Assert.Equal(expected, localized);

                    return Task.CompletedTask;
                },
                this.testOutputHelper);
        }

        [Theory]
        [InlineData("en-US", "Global", "This is global message...")]
        [InlineData("en-US", "Specific", "This is specific message...")]
        public Task ItShouldSetupEmbeddedLocalizerUsingInheritedJsonResourcesOnClasses(string cultureName, string key, string expected)
        {
            return SetupHelper.ProcessLocalizerTestAsync<Specific>(
                cultureName,
                localizer =>
                {
                    Assert.NotNull(localizer);

                    var localized = localizer[key];
                    Assert.Equal(expected, localized);

                    return Task.CompletedTask;
                },
                this.testOutputHelper);
        }

        [Fact]
        public async Task ItShouldLoadAsyncThroughHierarchy()
        {
            await SetupHelper.ProcessHttpLocalizerTestAsync<ISpecific>(
                "en-US",
                new Dictionary<string, string>
                {
                    ["ISpecific.json"] = "{\"Specific\": \"This is specific message...\"}",
                    ["IGlobal.json"] = "{\"Global\": \"This is global message...\"}",
                },
                async (localizer, unlocker) =>
                {
                    Assert.NotNull(localizer);

                    var specificLocalized = localizer["Specific"];
                    Assert.Equal("...", specificLocalized);

                    var globalLocalized = localizer["Global"];
                    Assert.Equal("...", globalLocalized);

                    // Start loading async
                    var loadingTask = localizer.LoadAsync();

                    // We are sure that it is not loaded at this point (since we didn't signal any one).

                    unlocker("ISpecific.json"); // Signal Specific

                    // Make sure Specific string is available.
                    var nbRetry = 5;
                    for (var i = 0; i <= nbRetry; i++)
                    {
                        await Task.Delay(100).ConfigureAwait(false);

                        specificLocalized = localizer["Specific"];
                        if ("This is specific message..." == specificLocalized)
                        {
                            break;
                        }
                        else
                        {
                            Assert.True(i < nbRetry);
                        }
                    }

                    // Global must not be loaded.
                    globalLocalized = localizer["Global"];
                    Assert.Equal("...", globalLocalized);

                    // Loading task must not be completed.
                    Assert.False(loadingTask.IsCompleted);

                    unlocker("IGlobal.json"); // Signal Global

                    // make sure all is loaded.
                    await loadingTask.ConfigureAwait(false);

                    specificLocalized = localizer["Specific"];
                    Assert.Equal("This is specific message...", specificLocalized);

                    globalLocalized = localizer["Global"];
                    Assert.Equal("This is global message...", globalLocalized);
                },
                this.testOutputHelper).ConfigureAwait(false);
        }

        [Fact]
        public async Task ItShouldLoadAsyncThroughHierarchyEvenIfNoSpecificResources()
        {
            await SetupHelper.ProcessHttpLocalizerTestAsync<ISpecific>(
                "en-US",
                new Dictionary<string, string>
                {
                    ["IGlobal.json"] = "{\"Global\": \"This is global message...\"}",
                },
                async (localizer, unlocker) =>
                {
                    Assert.NotNull(localizer);

                    var globalLocalized = localizer["Global"];
                    Assert.Equal("...", globalLocalized);

                    // Start loading async
                    var loadingTask = localizer.LoadAsync();

                    // We are sure that it is not loaded at this point (since we didn't signal any one).

                    unlocker("IGlobal.json"); // Signal Global

                    // Make sure Global string is available.
                    var nbRetry = 5;
                    for (var i = 0; i <= nbRetry; i++)
                    {
                        await Task.Delay(100).ConfigureAwait(false);

                        globalLocalized = localizer["Global"];
                        if ("This is global message..." == globalLocalized)
                        {
                            break;
                        }
                        else
                        {
                            Assert.True(i < nbRetry);
                        }
                    }

                    // Loading task must be completed.
                    Assert.True(loadingTask.IsCompleted);
                },
                this.testOutputHelper).ConfigureAwait(false);
        }
    }
}
