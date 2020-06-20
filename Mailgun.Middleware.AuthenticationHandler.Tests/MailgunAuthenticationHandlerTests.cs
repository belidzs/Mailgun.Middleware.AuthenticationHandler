// <copyright file="MailgunAuthenticationHandlerTests.cs" company="Balazs Keresztury">
// Copyright (c) Balazs Keresztury. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Mailgun.Models.SignedEvent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Mailgun.Middleware.AuthenticationHandler.Tests
{
    public class MailgunAuthenticationHandlerTests
    {
        private IHostBuilder _hostBuilder;
        private DateTime _originalTimestamp;
        private TimeSpan _timeSinceOriginalTimestamp;
        private MailgunSignature _validSignature;
        private string _apiKey;

        [SetUp]
        public void Setup()
        {
            _originalTimestamp = new DateTime(2020, 6, 18, 11, 55, 0).ToUniversalTime();
            var originalTimestampAsUnixEpoch = (_originalTimestamp - DateTime.UnixEpoch).TotalSeconds.ToString();
            _timeSinceOriginalTimestamp = DateTime.UtcNow - _originalTimestamp;
            _apiKey = "ffffffffffffffffffffffffffffffff-ffffffff-ffffffff";

            _validSignature = new MailgunSignature()
            {
                Signature = "de4b938580bb4d84f710cbb8bfa7d224bb2262c8f644f558c2901c1ae645bb03",
                Token = "ffffffffffffffffffffffffffffffffffffffffffffffffff",
                Timestamp = originalTimestampAsUnixEpoch,
            };

            _hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddAuthentication("MailgunSignature").AddScheme<MailgunAuthenticationSchemeOptions, MailgunAuthenticationHandler>("MailgunSignature", x =>
                        {
                            x.ApiKey = _apiKey;
                            x.MaxSignatureAge = _timeSinceOriginalTimestamp + new TimeSpan(1, 0, 0);
                        });
                    });
                });
        }

        [Test]
        public async Task Test1()
        {
            var host = await _hostBuilder.StartAsync();
            var testServer = host.GetTestServer();
            var client = testServer.CreateClient();

            var content = new StringContent(JsonSerializer.Serialize(_validSignature));

            var response = await client.PostAsync("/", content);
        }
    }
}