﻿using Bit.Core.Auth.Enums;
using Bit.Core.Auth.Models;
using Bit.Core.Auth.Utilities.Duo;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Duo = DuoUniversal;

namespace Bit.Core.Auth.Identity;

public class DuoWebTokenProvider : IUserTwoFactorTokenProvider<User>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeatureService _featureService;
    private readonly ICurrentContext _currentContext;
    private readonly GlobalSettings _globalSettings;

    public DuoWebTokenProvider(
        IServiceProvider serviceProvider,
        IFeatureService featureService,
        ICurrentContext currentContext,
        GlobalSettings globalSettings)
    {
        _serviceProvider = serviceProvider;
        _featureService = featureService;
        _currentContext = currentContext;
        _globalSettings = globalSettings;
    }

    public async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user)
    {
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        if (!await userService.CanAccessPremium(user))
        {
            return false;
        }

        var provider = user.GetTwoFactorProvider(TwoFactorProviderType.Duo);
        if (!HasProperMetaData(provider))
        {
            return false;
        }

        return await userService.TwoFactorProviderIsEnabledAsync(TwoFactorProviderType.Duo, user);
    }

    public async Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user)
    {
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        if (!await userService.CanAccessPremium(user))
        {
            return null;
        }

        var provider = user.GetTwoFactorProvider(TwoFactorProviderType.Duo);
        if (!HasProperMetaData(provider))
        {
            return null;
        }

        // DUO SDK v4 Update: Duo-Redirect
        if (_featureService.IsEnabled(FeatureFlagKeys.DuoRedirect, _currentContext))
        {
            var duoClient = await BuildDuoClient(provider);
            if (duoClient == null)
            {
                return null;
            }
            var state = Duo.Client.GenerateState(); //? Not sure on this yet. But required for GenerateAuthUrl
            var authUrl = duoClient.GenerateAuthUri(user.Email, state);

            return authUrl;
        }
        else
        {
            var signatureRequest = DuoWeb.SignRequest((string)provider.MetaData["IKey"],
                (string)provider.MetaData["SKey"], _globalSettings.Duo.AKey, user.Email);

            return signatureRequest;
        }
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user)
    {
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        if (!await userService.CanAccessPremium(user))
        {
            return false;
        }

        var provider = user.GetTwoFactorProvider(TwoFactorProviderType.Duo);
        if (!HasProperMetaData(provider))
        {
            return false;
        }

        // DUO SDK v4 Update: Duo-Redirect
        if (_featureService.IsEnabled(FeatureFlagKeys.DuoRedirect, _currentContext))
        {
            var duoClient = await BuildDuoClient(provider);
            if (duoClient == null)
            {
                return false;
            }

            // If the result of the exchange doesn't throw an exception and it's not null, then it's valid
            return duoClient.ExchangeAuthorizationCodeFor2faResult(token, user.Email) != null;
        }
        else
        {
            var response = DuoWeb.VerifyResponse((string)provider.MetaData["IKey"], (string)provider.MetaData["SKey"],
                _globalSettings.Duo.AKey, token);

            return response == user.Email;
        }
    }

    private bool HasProperMetaData(TwoFactorProvider provider)
    {
        return provider?.MetaData != null && provider.MetaData.ContainsKey("IKey") &&
            provider.MetaData.ContainsKey("SKey") && provider.MetaData.ContainsKey("Host");
    }

    /// <summary>
    /// Generates a Duo.Client object for use with Duo SDK v4. This combines the health check and the client generation
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    [RequireFeature(FeatureFlagKeys.DuoRedirect)]
    private async Task<Duo.Client> BuildDuoClient(TwoFactorProvider provider)
    {
        var client = new Duo.ClientBuilder(
            (string)provider.MetaData["IKey"], // SDK v4 this is ClientId
            (string)provider.MetaData["SKey"], // SDK v4 this is ClientSecret
            (string)provider.MetaData["Host"],
            string.Format("{0}/duo-redirect-connector", _globalSettings.BaseServiceUri.Vault))
            .Build();

        if (!await client.DoHealthCheck())
        {
            return null;
        }

        return client;
    }
}
