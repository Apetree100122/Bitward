﻿using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Bit.Core.Services;

public class MultiServicePushNotificationService : IPushNotificationService
{
    private readonly List<IPushNotificationService> _services = new List<IPushNotificationService>();
    private readonly ILogger<MultiServicePushNotificationService> _logger;

    public MultiServicePushNotificationService(
        IHttpClientFactory httpFactory,
        IDeviceRepository deviceRepository,
        IInstallationDeviceRepository installationDeviceRepository,
        GlobalSettings globalSettings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MultiServicePushNotificationService> logger,
        ILogger<RelayPushNotificationService> relayLogger,
        ILogger<NotificationsApiPushNotificationService> hubLogger)
    {
        _logger = logger;
        if (globalSettings.SelfHosted)
        {
            if (CoreHelpers.SettingHasValue(globalSettings.PushRelayBaseUri) &&
                globalSettings.Installation?.Id != null &&
                CoreHelpers.SettingHasValue(globalSettings.Installation?.Key))
            {
                _services.Add(new RelayPushNotificationService(httpFactory, deviceRepository, globalSettings,
                    httpContextAccessor, relayLogger));
                _logger.LogInformation("Self-hosted startup configuring push notification relay against {relay}", globalSettings.PushRelayBaseUri);
            }
            else
            {
                _logger.LogInformation("Self-hosted startup not configuring push relay. Push Relay Base Uri? {0} | Installation Id? {1} | Installation Key? {2}", 
                    CoreHelpers.SettingHasValue(globalSettings.PushRelayBaseUri), globalSettings.Installation?.Id != null, CoreHelpers.SettingHasValue(globalSettings.Installation?.Key));
            }
            if (CoreHelpers.SettingHasValue(globalSettings.InternalIdentityKey) &&
                CoreHelpers.SettingHasValue(globalSettings.BaseServiceUri.InternalNotifications))
            {
                _services.Add(new NotificationsApiPushNotificationService(
                    httpFactory, globalSettings, httpContextAccessor, hubLogger));
                _logger.LogInformation("Self-hosted startup configuring Notifications API against {uri}", globalSettings.BaseServiceUri.InternalNotifications);
            }
            else
            {
                _logger.LogInformation("Self-hosted startup not configuring Notifications API push notifications. Internal Identity Key? {0} | Internal Notifications Uri? {1}", 
                    CoreHelpers.SettingHasValue(globalSettings.InternalIdentityKey), CoreHelpers.SettingHasValue(globalSettings.BaseServiceUri.InternalNotifications));
            }
        }
        else
        {
            if (CoreHelpers.SettingHasValue(globalSettings.NotificationHub.ConnectionString))
            {
                _services.Add(new NotificationHubPushNotificationService(installationDeviceRepository,
                    globalSettings, httpContextAccessor));
            }
            if (CoreHelpers.SettingHasValue(globalSettings.Notifications?.ConnectionString))
            {
                _services.Add(new AzureQueuePushNotificationService(globalSettings, httpContextAccessor));
            }
        }
    }

    public Task PushSyncCipherCreateAsync(Cipher cipher, IEnumerable<Guid> collectionIds)
    {
        PushToServices((s) => s.PushSyncCipherCreateAsync(cipher, collectionIds));
        return Task.FromResult(0);
    }

    public Task PushSyncCipherUpdateAsync(Cipher cipher, IEnumerable<Guid> collectionIds)
    {
        PushToServices((s) => s.PushSyncCipherUpdateAsync(cipher, collectionIds));
        return Task.FromResult(0);
    }

    public Task PushSyncCipherDeleteAsync(Cipher cipher)
    {
        PushToServices((s) => s.PushSyncCipherDeleteAsync(cipher));
        return Task.FromResult(0);
    }

    public Task PushSyncFolderCreateAsync(Folder folder)
    {
        PushToServices((s) => s.PushSyncFolderCreateAsync(folder));
        return Task.FromResult(0);
    }

    public Task PushSyncFolderUpdateAsync(Folder folder)
    {
        PushToServices((s) => s.PushSyncFolderUpdateAsync(folder));
        return Task.FromResult(0);
    }

    public Task PushSyncFolderDeleteAsync(Folder folder)
    {
        PushToServices((s) => s.PushSyncFolderDeleteAsync(folder));
        return Task.FromResult(0);
    }

    public Task PushSyncCiphersAsync(Guid userId)
    {
        PushToServices((s) => s.PushSyncCiphersAsync(userId));
        return Task.FromResult(0);
    }

    public Task PushSyncVaultAsync(Guid userId)
    {
        PushToServices((s) => s.PushSyncVaultAsync(userId));
        return Task.FromResult(0);
    }

    public Task PushSyncOrgKeysAsync(Guid userId)
    {
        PushToServices((s) => s.PushSyncOrgKeysAsync(userId));
        return Task.FromResult(0);
    }

    public Task PushSyncSettingsAsync(Guid userId)
    {
        PushToServices((s) => s.PushSyncSettingsAsync(userId));
        return Task.FromResult(0);
    }

    public Task PushLogOutAsync(Guid userId)
    {
        PushToServices((s) => s.PushLogOutAsync(userId));
        return Task.FromResult(0);
    }

    public Task PushSyncSendCreateAsync(Send send)
    {
        PushToServices((s) => s.PushSyncSendCreateAsync(send));
        return Task.FromResult(0);
    }

    public Task PushSyncSendUpdateAsync(Send send)
    {
        PushToServices((s) => s.PushSyncSendUpdateAsync(send));
        return Task.FromResult(0);
    }

    public Task PushSyncSendDeleteAsync(Send send)
    {
        PushToServices((s) => s.PushSyncSendDeleteAsync(send));
        return Task.FromResult(0);
    }

    public Task SendPayloadToUserAsync(string userId, PushType type, object payload, string identifier,
        string deviceId = null)
    {
        PushToServices((s) => s.SendPayloadToUserAsync(userId, type, payload, identifier, deviceId));
        return Task.FromResult(0);
    }

    public Task SendPayloadToOrganizationAsync(string orgId, PushType type, object payload, string identifier,
        string deviceId = null)
    {
        PushToServices((s) => s.SendPayloadToOrganizationAsync(orgId, type, payload, identifier, deviceId));
        return Task.FromResult(0);
    }

    private void PushToServices(Func<IPushNotificationService, Task> pushFunc)
    {
        if (_services != null)
        {
            foreach (var service in _services)
            {
                pushFunc(service);
            }
        }
    }
}
