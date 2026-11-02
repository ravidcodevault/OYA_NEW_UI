-- Run in the database used by the deployed application's
-- ConnectionStrings:DefaultConnection. Publishing the website does not run this script.
-- Historical sales remain valid when an item is subsequently marked inactive.
-- Preserve all other filters, result columns, and existing procedure customizations.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @procedures TABLE (Name sysname PRIMARY KEY);
    INSERT INTO @procedures (Name)
    VALUES (N'USP_GetOverallDashboardSales_V2'), (N'USP_GetSalesDataForReports');

    DECLARE @changes TABLE (ProcedureName sysname, RemovedItemFilters int);
    DECLARE @name sysname, @definition nvarchar(max), @updated nvarchar(max);
    DECLARE @predicate nvarchar(100), @removed int, @index int;
    DECLARE @compact nvarchar(max), @createAt int, @message nvarchar(2048);

    WHILE EXISTS (SELECT 1 FROM @procedures)
    BEGIN
        SELECT TOP (1) @name = Name FROM @procedures ORDER BY Name;
        SET @definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @name, N'P'));
        IF @definition IS NULL
        BEGIN
            SET @message = N'Cannot read dbo.' + @name
                + N'. Check the selected database and VIEW DEFINITION permission.';
            THROW 51000, @message, 1;
        END;

        SET @updated = @definition;
        SET @removed = 0;
        SET @index = 1;
        WHILE @index <= 4
        BEGIN
            SET @predicate = CASE @index
                WHEN 1 THEN N'AND (i.IsActive = 1)'
                WHEN 2 THEN N'AND (i.IsActive=1)'
                WHEN 3 THEN N'AND i.IsActive = 1'
                WHEN 4 THEN N'AND i.IsActive=1' END;
            SET @removed += (LEN(@updated) - LEN(REPLACE(
                @updated COLLATE Latin1_General_100_CI_AS, @predicate, N'')))
                / LEN(@predicate);
            SET @updated = REPLACE(
                @updated COLLATE Latin1_General_100_CI_AS, @predicate, N'');
            SET @index += 1;
        END;

        SET @compact = REPLACE(REPLACE(REPLACE(REPLACE(
            @updated, N' ', N''), CHAR(9), N''), CHAR(10), N''), CHAR(13), N'');
        IF @compact COLLATE Latin1_General_100_CI_AS LIKE N'%i.IsActive%'
           OR @removed NOT IN (0, 3)
        BEGIN
            SET @message = N'Unexpected item-filter format/count in dbo.' + @name
                + N'. Review its definition before changing it; migration rolled back.';
            THROW 51001, @message, 1;
        END;

        IF @removed = 3
        BEGIN
            -- OBJECT_DEFINITION can begin with CREATE, ALTER, or CREATE OR ALTER.
            IF @updated COLLATE Latin1_General_100_CI_AS NOT LIKE N'%CREATE OR ALTER%'
            BEGIN
                SET @createAt = CHARINDEX(N'CREATE',
                    @updated COLLATE Latin1_General_100_CI_AS);
                IF @createAt > 0
                    SET @updated = STUFF(@updated, @createAt, 6, N'ALTER');
            END;
            EXEC sys.sp_executesql @updated;
        END;

        INSERT INTO @changes VALUES (@name, @removed);
        DELETE FROM @procedures WHERE Name = @name;
    END;

    COMMIT TRANSACTION;
    SELECT DB_NAME() AS DatabaseName, ProcedureName, RemovedItemFilters
    FROM @changes ORDER BY ProcedureName;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
