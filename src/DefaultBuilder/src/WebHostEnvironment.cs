// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{
    internal class WebHostEnvironment : IWebHostEnvironment
    {
        private static readonly NullFileProvider NullFileProvider = new();

        public WebHostEnvironment(Assembly? callingAssembly)
        {
            ContentRootPath = Directory.GetCurrentDirectory();

            ApplicationName = (callingAssembly ?? Assembly.GetEntryAssembly())?.GetName()?.Name ?? string.Empty;
            EnvironmentName = Environments.Production;

            // Default to /wwwroot if it exists.
            var wwwroot = Path.Combine(ContentRootPath, "wwwroot");
            if (Directory.Exists(wwwroot))
            {
                WebRootPath = wwwroot;
            }

            ContentRootFileProvider = NullFileProvider;
            WebRootFileProvider = NullFileProvider;

            ResolveFileProviders(new Configuration());
        }

        // For testing
        internal WebHostEnvironment()
        {
            ApplicationName = default!;
            EnvironmentName = default!;
            ContentRootPath = default!;
            WebRootPath = default!;
            ContentRootFileProvider = default!;
            WebRootFileProvider = default!;
        }

        public void ApplyConfigurationSettings(IConfiguration configuration)
        {
            ReadConfigurationSettings(configuration);
            ResolveFileProviders(configuration);
        }

        internal void ReadConfigurationSettings(IConfiguration configuration)
        {
            ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? ApplicationName;
            EnvironmentName = configuration[WebHostDefaults.EnvironmentKey] ?? EnvironmentName;
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey] ?? ContentRootPath;
            WebRootPath = configuration[WebHostDefaults.WebRootKey] ?? WebRootPath;
        }

        public void ApplyEnvironmentSettings(IWebHostBuilder genericWebHostBuilder)
        {
            genericWebHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, ApplicationName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, EnvironmentName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.ContentRootKey, ContentRootPath);
            genericWebHostBuilder.UseSetting(WebHostDefaults.WebRootKey, WebRootPath);

            genericWebHostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                CopyPropertiesTo(context.HostingEnvironment);
            });
        }

        internal void CopyPropertiesTo(IWebHostEnvironment destination)
        {
            destination.ApplicationName = ApplicationName;
            destination.EnvironmentName = EnvironmentName;

            ResolveFileProviders(new Configuration());

            destination.ContentRootPath = ContentRootPath;
            destination.ContentRootFileProvider = ContentRootFileProvider;

            destination.WebRootPath = WebRootPath;
            destination.WebRootFileProvider = WebRootFileProvider;
        }

        public void ResolveFileProviders(IConfiguration configuration)
        {
            if (Directory.Exists(ContentRootPath))
            {
                ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
            }

            if (Directory.Exists(WebRootPath))
            {
                WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            }

            if (this.IsDevelopment())
            {
                StaticWebAssetsLoader.UseStaticWebAssets(this, configuration);
            }
        }

        private string ResolveWebRootPath(string webRootPath, string contentRootPath)
        {
            if (string.IsNullOrEmpty(webRootPath))
            {
                return Path.GetFullPath(contentRootPath);
            }

            if (Path.IsPathRooted(webRootPath))
            {
                return webRootPath;
            }

            return Path.Combine(Path.GetFullPath(contentRootPath), webRootPath);
        }

        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        // Mimick the setting used in HostingEnvironment
        private string _webRootPath = default!;
        public string WebRootPath
        {
            get => _webRootPath;
            set => _webRootPath = ResolveWebRootPath(value, ContentRootPath ?? Directory.GetCurrentDirectory());
        }
    }
}
