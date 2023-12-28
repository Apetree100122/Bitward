CREATE PROCEDURE [dbo].[CollectionUser_ReadByOrganizationUserIds_V2]
    @OrganizationUserIds [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        CU.*
    FROM
        [dbo].[OrganizationUser] OU
    INNER JOIN
        [dbo].[CollectionUser] CU ON CU.[OrganizationUserId] = OU.[Id]
    INNER JOIN
        @OrganizationUserIds OUI ON OUI.[Id] = OU.[Id]
END