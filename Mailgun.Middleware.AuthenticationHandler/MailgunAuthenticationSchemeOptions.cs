// <copyright file="MailgunAuthenticationSchemeOptions.cs" company="Balazs Keresztury">
// Copyright (c) Balazs Keresztury. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Microsoft.AspNetCore.Authentication;

namespace Mailgun.Middleware.AuthenticationHandler
{
    /// <summary>
    /// Options for the MailgunAuthenticationHandler middleware.
    /// </summary>
    public class MailgunAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Mailgun API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The maximum acceptable age of the signature.
        /// </summary>
        public TimeSpan MaxSignatureAge { get; set; }
    }
}
