﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Business;
using Bit.Core.Models.Data.Organizations.OrganizationUsers;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Scim.Models;
using Bit.Scim.Users;
using Bit.Scim.Utilities;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Scim.Test.Users;

[SutProviderCustomize]
public class PostUserCommandTests
{
    [Theory]
    [BitAutoData]
    public async Task PostUser_Success(SutProvider<PostUserCommand> sutProvider, string externalId,
        Organization organization, List<BaseScimUserModel.EmailModel> emails,
        ICollection<OrganizationUserUserDetails> organizationUsers, Core.Entities.OrganizationUser newUser)
    {
        var scimUserRequestModel = new ScimUserRequestModel
        {
            ExternalId = externalId,
            Emails = emails,
            Active = true,
            Schemas = new List<string> { ScimConstants.Scim2SchemaUser }
        };

        sutProvider.GetDependency<IOrganizationRepository>()
            .GetByIdAsync(organization.Id)
            .Returns(organization);
        sutProvider.GetDependency<IOrganizationUserRepository>()
            .GetManyDetailsByOrganizationAsync(organization.Id)
            .Returns(organizationUsers);

        var expectedInvite = () => Arg.Is<OrganizationUserInvite>(i =>
            i.Emails.SingleOrDefault(e => e == scimUserRequestModel.PrimaryEmail.ToLowerInvariant()) != null &&
            i.Type == OrganizationUserType.User &&
            i.AccessAll == false &&
            !i.Collections.Any() &&
            !i.Groups.Any());

        sutProvider.GetDependency<IOrganizationService>()
            .InviteUserAsync(organization, EventSystemUser.SCIM, expectedInvite(), externalId)
            .Returns(newUser);

        var user = await sutProvider.Sut.PostUserAsync(organization.Id, scimUserRequestModel);

        await sutProvider.GetDependency<IOrganizationService>().Received(1)
            .InviteUserAsync(organization, EventSystemUser.SCIM, expectedInvite(), externalId);
        await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1).GetDetailsByIdAsync(newUser.Id);
    }

    [Theory]
    [BitAutoData]
    public async Task PostUser_NullEmail_Throws(SutProvider<PostUserCommand> sutProvider, Guid organizationId)
    {
        var scimUserRequestModel = new ScimUserRequestModel
        {
            Emails = new List<BaseScimUserModel.EmailModel>(),
            Active = true,
            Schemas = new List<string> { ScimConstants.Scim2SchemaUser }
        };

        await Assert.ThrowsAsync<BadRequestException>(async () => await sutProvider.Sut.PostUserAsync(organizationId, scimUserRequestModel));
    }

    [Theory]
    [BitAutoData]
    public async Task PostUser_Inactive_Throws(SutProvider<PostUserCommand> sutProvider, Guid organizationId, List<BaseScimUserModel.EmailModel> emails)
    {
        var scimUserRequestModel = new ScimUserRequestModel
        {
            Emails = emails,
            Active = false,
            Schemas = new List<string> { ScimConstants.Scim2SchemaUser }
        };

        await Assert.ThrowsAsync<BadRequestException>(async () => await sutProvider.Sut.PostUserAsync(organizationId, scimUserRequestModel));
    }

    [Theory]
    [BitAutoData]
    public async Task PostUser_DuplicateExternalId_Throws(SutProvider<PostUserCommand> sutProvider, Guid organizationId, List<BaseScimUserModel.EmailModel> emails, ICollection<OrganizationUserUserDetails> organizationUsers)
    {
        var scimUserRequestModel = new ScimUserRequestModel
        {
            ExternalId = organizationUsers.First().ExternalId,
            Emails = emails,
            Active = true,
            Schemas = new List<string> { ScimConstants.Scim2SchemaUser }
        };

        sutProvider.GetDependency<IOrganizationUserRepository>()
            .GetManyDetailsByOrganizationAsync(organizationId)
            .Returns(organizationUsers);

        await Assert.ThrowsAsync<ConflictException>(async () => await sutProvider.Sut.PostUserAsync(organizationId, scimUserRequestModel));
    }

    [Theory]
    [BitAutoData]
    public async Task PostUser_DuplicateUserName_Throws(SutProvider<PostUserCommand> sutProvider, Guid organizationId, List<BaseScimUserModel.EmailModel> emails, ICollection<OrganizationUserUserDetails> organizationUsers)
    {
        var scimUserRequestModel = new ScimUserRequestModel
        {
            UserName = organizationUsers.First().ExternalId,
            Emails = emails,
            Active = true,
            Schemas = new List<string> { ScimConstants.Scim2SchemaUser }
        };

        sutProvider.GetDependency<IOrganizationUserRepository>()
            .GetManyDetailsByOrganizationAsync(organizationId)
            .Returns(organizationUsers);

        await Assert.ThrowsAsync<ConflictException>(async () => await sutProvider.Sut.PostUserAsync(organizationId, scimUserRequestModel));
    }
}
