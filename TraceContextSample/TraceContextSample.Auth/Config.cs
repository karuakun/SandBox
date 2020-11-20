// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Test;

namespace TraceContextSample.Auth
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(), 
                new IdentityResources.Address()
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
                    ClientSecrets = new[]
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
                    ClientSecrets = new[]
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
                    ClientSecrets = new[]
                    {
                        new Secret(Constants.Backend2.ClientSecret.Sha256())
                    },
                    AllowedScopes =
                    {
                        Constants.Backend3.ResourceName
                    }
                },
                new Client
                {
                    ClientId = Constants.WebApp.ClientId,
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = new[]
                    {
                        new Secret(Constants.WebApp.ClientSecret.Sha256())
                    },
                    RequireClientSecret = false,
                    RequirePkce = true,
                    AllowedScopes = Constants.WebApp.Scopes.ToList(),
                    AllowOfflineAccess = true,
                    AllowedCorsOrigins = new[] 
                    {
                        Constants.WebApp.BaseUri ,
                    },
                    RedirectUris = Constants.WebApp.RedirectUris.ToList(),
                },
                new Client
                {
                    ClientId = Constants.WebApp2.ClientId,
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = new[]
                    {
                        new Secret(Constants.WebApp2.ClientSecret.Sha256())
                    },
                    RequireClientSecret = false,
                    RequirePkce = true,
                    AllowedScopes = Constants.WebApp2.Scopes.ToList(),
                    AllowOfflineAccess = true,
                    AllowedCorsOrigins = new[]
                    {
                        Constants.WebApp2.BaseUri ,
                    },
                    RedirectUris = Constants.WebApp2.RedirectUris.ToList(),
                }
            };

        public static List<TestUser> TestUsers =>
            new List<TestUser>
            {
                new TestUser
                {
                    Username = "test@test.local",
                    SubjectId = "test@test.local",
                    Password = "test",
                    IsActive = true,
                    Claims = new List<Claim>
                    {
                        new Claim(OidcConstants.StandardScopes.Email, "test@test.local"),
                        new Claim(OidcConstants.StandardScopes.Address, "日本のどこか"),
                        new Claim("test", "test")
                    }
                }
            };

    }
}