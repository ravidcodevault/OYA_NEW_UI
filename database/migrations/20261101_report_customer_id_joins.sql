-- Run in the database used by the daily-report application's DefaultConnection.
-- Match QuickBooks customers by ListID; names can contain legacy quote encodings
-- or change after a transaction was created. Preserve all existing report filters.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @definition nvarchar(max) =
        OBJECT_DEFINITION(OBJECT_ID(N'dbo.USP_GetSalesDataForReports', N'P'));
    IF @definition IS NULL
        THROW 51100, 'Cannot read dbo.USP_GetSalesDataForReports.', 1;

    DECLARE @updated nvarchar(max) = @definition;
    DECLARE @oldJoin nvarchar(100) = N'ON c.FullName = a.CustomerRefFullName';
    DECLARE @newJoin nvarchar(100) = N'ON c.ListID = a.CustomerRefListID';
    DECLARE @oldSelect nvarchar(100) = N'            FullName,';
    DECLARE @newSelect nvarchar(100) = N'            ListID,';
    DECLARE @oldGroup nvarchar(100) = N'GROUP BY FullName';
    DECLARE @newGroup nvarchar(100) = N'GROUP BY ListID';
    DECLARE @oldJoins int = (LEN(@definition)-LEN(REPLACE(@definition,@oldJoin,N'')))/LEN(@oldJoin);
    DECLARE @newJoins int = (LEN(@definition)-LEN(REPLACE(@definition,@newJoin,N'')))/LEN(@newJoin);
    DECLARE @oldSelects int = (LEN(@definition)-LEN(REPLACE(@definition,@oldSelect,N'')))/LEN(@oldSelect);
    DECLARE @oldGroups int = (LEN(@definition)-LEN(REPLACE(@definition,@oldGroup,N'')))/LEN(@oldGroup);
    DECLARE @changed bit = 0;

    IF @oldJoins=0 AND @newJoins=3
       AND CHARINDEX(@newSelect,@definition)>0 AND CHARINDEX(@newGroup,@definition)>0
    BEGIN
        -- Already migrated; no-op.
        SET @changed=0;
    END
    ELSE
    BEGIN
        IF @oldJoins<>3 OR @newJoins<>0 OR @oldSelects<>1 OR @oldGroups<>1
            THROW 51101, 'Unexpected customer-lookup shape; review the procedure before changing it.', 1;
        SET @updated=REPLACE(@updated,@oldSelect,@newSelect);
        SET @updated=REPLACE(@updated,@oldGroup,@newGroup);
        SET @updated=REPLACE(@updated,@oldJoin,@newJoin);

        IF @updated COLLATE Latin1_General_100_CI_AS LIKE N'CREATE OR ALTER%'
            SET @updated=STUFF(@updated,1,LEN(N'CREATE OR ALTER'),N'ALTER');
        ELSE IF @updated COLLATE Latin1_General_100_CI_AS LIKE N'CREATE%'
            SET @updated=STUFF(@updated,1,LEN(N'CREATE'),N'ALTER');
        ELSE IF @updated COLLATE Latin1_General_100_CI_AS NOT LIKE N'ALTER%'
            THROW 51102, 'Unexpected procedure declaration; migration rolled back.', 1;

        EXEC sys.sp_executesql @updated;
        SET @changed=1;
    END;
    COMMIT TRANSACTION;
    SELECT DB_NAME() AS DatabaseName,
        N'dbo.USP_GetSalesDataForReports' AS ProcedureName,
        @changed AS Changed;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

