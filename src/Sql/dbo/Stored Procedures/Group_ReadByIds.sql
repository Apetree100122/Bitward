CREATE PROCEDURE [dbo].[Group_ReadByIds]
    @Ids AS [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    IF (SELECT COUNT(1) FROM @Ids) < 1
        BEGIN
            RETURN(-1)
        END

    SELECT
        *
    FROM
        [dbo].[Group]
    WHERE
        [Id] IN (SELECT [Id] FROM @Ids)
END