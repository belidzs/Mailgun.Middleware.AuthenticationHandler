// <copyright file="MailgunAuthenticationHandler.cs" company="Balazs Keresztury">
// Copyright (c) Balazs Keresztury. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Mailgun.Models.SignedEvent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mailgun.Middleware.AuthenticationHandler
{
    /// <summary>
    /// An AuthenticationHandler middleware which authenticates POST requests from Mailgun.
    /// </summary>
    public class MailgunAuthenticationHandler : AuthenticationHandler<MailgunAuthenticationSchemeOptions>
    {
        private ILogger<MailgunAuthenticationHandler> _logger;
        private JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MailgunAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">MailgunAuthenticationSchemeOptions.</param>
        /// <param name="logger">An ILoggerFactory.</param>
        /// <param name="encoder">A UrlEncoder.</param>
        /// <param name="clock">An ISystemClock.</param>
        public MailgunAuthenticationHandler(IOptionsMonitor<MailgunAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _logger = logger.CreateLogger<MailgunAuthenticationHandler>();

            // configure JSON serializer to handle Mailgun's format
            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = new DashedJsonNamingPolicy.DashedJsonNamingPolicy(),
            };
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <inheritdoc />
        protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // this enables reading Request.Body multiple times
            Request.EnableBuffering();

            // record original string position
            long initialPosition = Request.Body.Position;

            // deserialize content
            var signedEvent = await JsonSerializer.DeserializeAsync<SignedEvent>(Request.Body, _jsonSerializerOptions);

            // rewind stream as a courtesy for the next middleware in the pipeline
            Request.Body.Position = initialPosition;

            // validate signedEvent
            if (!ValidateSignedEvent(signedEvent))
            {
                // invalid schema
                return AuthenticateResult.NoResult();
            }

            if (signedEvent.Signature.IsValid(Options.ApiKey, Options.MaxSignatureAge))
            {
                // successful authentication yields a dummy principal for authorization purposes
                var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "mailgun") }, Scheme.Name);
                var p = new ClaimsPrincipal(identity);
                var t = new AuthenticationTicket(p, Scheme.Name);
                return AuthenticateResult.Success(t);
            }
            else
            {
                string message = "Invalid signature";
                _logger.LogWarning(message);
                return AuthenticateResult.Fail(message);
            }
        }

        private bool ValidateSignedEvent(SignedEvent signedEvent)
        {
            // validate signedEvent using the DataAnnotations in Mailgun.Models.SignedEvent
            var context = new ValidationContext(signedEvent);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(signedEvent, context, results))
            {
                foreach (var result in results)
                {
                    _logger.LogWarning("Data validation failure: {errorMessage}", result.ErrorMessage);
                }

                return false;
            }

            return true;
        }
    }
}
