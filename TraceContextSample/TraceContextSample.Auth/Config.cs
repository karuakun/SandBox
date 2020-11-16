// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;

namespace TraceContextSample.Auth
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            { 
                new IdentityResources.OpenId()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new []
            {
                new ApiScope(Constants.Bff.ResourceName),
                new ApiScope(Constants.Backend1.ResourceName),
                new ApiScope(Constants.Backend2.ResourceName),
                new ApiScope(Constants.Backend3.ResourceName),
            };


        public static IEnumerable<Client> Clients =>
            new []
            {
                new Client
                {
                    ClientId = Constants.ConsoleApp.ClientId,
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = new List<Secret>
                    {
                        new Secret(Constants.ConsoleApp.ClientSecret.Sha256())
                    },
                    AllowedScopes =
                    {
                        Constants.Bff.ResourceName
                    }
                },
                new Client
                {
                    ClientId = Constants.Bff.ClientId,
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = new List<Secret>
                    {
                        new Secret(Constants.Bff.ClientSecret.Sha256())
                    },
                    AllowedScopes =
                    {
                        Constants.Backend1.ResourceName,
                        Constants.Backend2.ResourceName
                    }
                },
                new Client
                {
                    ClientId = Constants.Backend2.ClientId,
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = new List<Secret>
                    {
                        new Secret(Constants.Backend2.ClientSecret.Sha256())
                    },
                    AllowedScopes =
                    {
                        Constants.Backend3.ResourceName
                    }
                },
            };
    }
}