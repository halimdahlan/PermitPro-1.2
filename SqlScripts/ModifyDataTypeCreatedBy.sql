BEGIN TRANSACTION;
BEGIN TRY
    -- Step 1: Modify the column data type
    -- SQL Server handles implicit conversion from uniqueidentifier to nvarchar automatically
    ALTER TABLE [Permits] 
    ALTER COLUMN [CreatedBy] nvarchar(450) NULL;

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration failed. Rolled back changes.';
    THROW;
END CATCH;