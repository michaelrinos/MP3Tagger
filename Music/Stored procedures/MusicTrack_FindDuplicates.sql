-- Stored procedure to find duplicate music tracks based on specified fields
CREATE PROCEDURE [dbo].[MusicTrack_FindDuplicates]
    @GroupByFields NVARCHAR(MAX),  -- Comma-separated list of fields to group by
    @ReturnDuplicatesOnly BIT = 1  -- 1 to return only duplicates, 0 to return all grouped records
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate input parameters
    IF @GroupByFields IS NULL OR LTRIM(RTRIM(@GroupByFields)) = ''
    BEGIN
        RAISERROR('GroupByFields parameter cannot be null or empty', 16, 1)
        RETURN
    END
    
    DECLARE @SQL NVARCHAR(MAX)
    DECLARE @GroupByClause NVARCHAR(MAX)
    DECLARE @WhereClause NVARCHAR(MAX) = ''
    DECLARE @JoinConditions NVARCHAR(MAX) = ''
    
    -- Clean up the input
    SET @GroupByClause = LTRIM(RTRIM(@GroupByFields))
    
    -- Build WHERE clause to check each field for NOT NULL
    DECLARE @Field NVARCHAR(255)
    DECLARE @Pos INT = 1
    DECLARE @NextPos INT
    
    -- First pass: Build WHERE clause for NULL checks
    SET @Pos = 1
    WHILE @Pos <= LEN(@GroupByClause)
    BEGIN
        SET @NextPos = CHARINDEX(',', @GroupByClause, @Pos)
        IF @NextPos = 0 SET @NextPos = LEN(@GroupByClause) + 1
        
        SET @Field = LTRIM(RTRIM(SUBSTRING(@GroupByClause, @Pos, @NextPos - @Pos)))
        
        -- Validate field name (basic SQL injection protection)
        IF @Field LIKE '%[^a-zA-Z0-9_]%' OR @Field = ''
        BEGIN
            RAISERROR('Invalid field name detected: %s', 16, 1, @Field)
            RETURN
        END
        
        IF @WhereClause <> ''
            SET @WhereClause = @WhereClause + ' AND '
        
        SET @WhereClause = @WhereClause + QUOTENAME(@Field) + ' IS NOT NULL'
        
        SET @Pos = @NextPos + 1
    END
    
    -- Second pass: Build JOIN conditions
    SET @Pos = 1
    WHILE @Pos <= LEN(@GroupByClause)
    BEGIN
        SET @NextPos = CHARINDEX(',', @GroupByClause, @Pos)
        IF @NextPos = 0 SET @NextPos = LEN(@GroupByClause) + 1
        
        SET @Field = LTRIM(RTRIM(SUBSTRING(@GroupByClause, @Pos, @NextPos - @Pos)))
        
        IF @JoinConditions <> ''
            SET @JoinConditions = @JoinConditions + ' AND '
        
        SET @JoinConditions = @JoinConditions + 'mt.' + QUOTENAME(@Field) + ' = dg.' + QUOTENAME(@Field)
        
        SET @Pos = @NextPos + 1
    END
    
    -- Build the GROUP BY clause with proper quoting and SELECT clause
    DECLARE @QuotedGroupBy NVARCHAR(MAX) = ''
    DECLARE @QuotedGroupByWithAlias NVARCHAR(MAX) = ''
    DECLARE @SelectClause NVARCHAR(MAX) = 'MusicFileId, '
    SET @Pos = 1
    WHILE @Pos <= LEN(@GroupByClause)
    BEGIN
        SET @NextPos = CHARINDEX(',', @GroupByClause, @Pos)
        IF @NextPos = 0 SET @NextPos = LEN(@GroupByClause) + 1
        
        SET @Field = LTRIM(RTRIM(SUBSTRING(@GroupByClause, @Pos, @NextPos - @Pos)))
        
        IF @QuotedGroupBy <> ''
        BEGIN
            SET @QuotedGroupBy = @QuotedGroupBy + ', '
            SET @QuotedGroupByWithAlias = @QuotedGroupByWithAlias + ', '
        END
        
        SET @QuotedGroupBy = @QuotedGroupBy + QUOTENAME(@Field)
        SET @QuotedGroupByWithAlias = @QuotedGroupByWithAlias + 'mt.' + QUOTENAME(@Field)
        SET @SelectClause = @SelectClause + QUOTENAME(@Field) + ', '
        
        SET @Pos = @NextPos + 1
    END
    
    -- Complete the SELECT clause
    SET @SelectClause = @SelectClause + 'RowNum, CASE WHEN RowNum = 1 THEN 0 ELSE 1 END as IsDuplicate'
    
    -- Build dynamic SQL to find duplicates
    SET @SQL = '
    WITH DuplicateGroups AS (
        SELECT ' + @QuotedGroupBy + ', COUNT(*) as DuplicateCount
        FROM [MusicTrack]
        WHERE ' + @WhereClause + '
        GROUP BY ' + @QuotedGroupBy + '
        HAVING COUNT(*) > 1
    ),
    RankedDuplicates AS (
        SELECT mt.*, 
               ROW_NUMBER() OVER (PARTITION BY ' + @QuotedGroupByWithAlias + ' ORDER BY mt.MusicFileId) as RowNum
        FROM [MusicTrack] mt
        INNER JOIN DuplicateGroups dg ON ' + @JoinConditions + '
    )
    SELECT ' + @SelectClause + '
    FROM RankedDuplicates'
    
    IF @ReturnDuplicatesOnly = 1
    BEGIN
        SET @SQL = @SQL + ' WHERE RowNum > 1'
    END
    
    SET @SQL = @SQL + ' ORDER BY ' + @QuotedGroupBy + ', RowNum'
    
    -- Execute with error handling
    BEGIN TRY
        EXEC sp_executesql @SQL
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
        DECLARE @ErrorState INT = ERROR_STATE()
        
        RAISERROR('Error executing dynamic SQL: %s', @ErrorSeverity, @ErrorState, @ErrorMessage)
    END CATCH
END
GO

-- Stored procedure to delete duplicate music tracks, keeping only the first occurrence
CREATE PROCEDURE [dbo].[MusicTrack_RemoveDuplicates]
    @GroupByFields NVARCHAR(MAX),  -- Comma-separated list of fields to group by
    @DeletedCount INT OUTPUT       -- Number of records deleted
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX)
    DECLARE @GroupByClause NVARCHAR(MAX)
    
    SET @GroupByClause = @GroupByFields
    
    -- Build dynamic SQL to delete duplicates, keeping the first record in each group
    SET @SQL = '
    WITH DuplicateGroups AS (
        SELECT ' + @GroupByClause + ', COUNT(*) as DuplicateCount
        FROM [MusicTrack]
        WHERE ' + @GroupByClause + ' IS NOT NULL
        GROUP BY ' + @GroupByClause + '
        HAVING COUNT(*) > 1
    ),
    RankedDuplicates AS (
        SELECT mt.MusicFileId,
               ROW_NUMBER() OVER (PARTITION BY ' + @GroupByClause + ' ORDER BY mt.MusicFileId) as RowNum
        FROM [MusicTrack] mt
        INNER JOIN DuplicateGroups dg ON '
    
    -- Build the JOIN conditions dynamically
    DECLARE @JoinConditions NVARCHAR(MAX) = ''
    DECLARE @Field NVARCHAR(255)
    DECLARE @Pos INT = 1
    DECLARE @NextPos INT
    
    WHILE @Pos <= LEN(@GroupByFields)
    BEGIN
        SET @NextPos = CHARINDEX(',', @GroupByFields, @Pos)
        IF @NextPos = 0 SET @NextPos = LEN(@GroupByFields) + 1
        
        SET @Field = LTRIM(RTRIM(SUBSTRING(@GroupByFields, @Pos, @NextPos - @Pos)))
        
        IF @JoinConditions <> ''
            SET @JoinConditions = @JoinConditions + ' AND '
        
        SET @JoinConditions = @JoinConditions + 'mt.' + @Field + ' = dg.' + @Field
        
        SET @Pos = @NextPos + 1
    END
    
    SET @SQL = @SQL + @JoinConditions + '
    )
    DELETE FROM [MusicTrack] 
    WHERE MusicFileId IN (
        SELECT MusicFileId FROM RankedDuplicates WHERE RowNum > 1
    )'
    
    EXEC sp_executesql @SQL
    
    SET @DeletedCount = @@ROWCOUNT
END