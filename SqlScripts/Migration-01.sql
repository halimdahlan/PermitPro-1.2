-- 1. Create the index for performance on the foreign key
CREATE INDEX [IX_Permits_CreatedBy] 
ON [Permits] ([CreatedBy]);
GO

-- 2. Add the Foreign Key constraint pointing to AspNetUsers
ALTER TABLE [Permits] 
ADD CONSTRAINT [FK_Permits_AspNetUsers_CreatedBy] 
FOREIGN KEY ([CreatedBy]) 
REFERENCES [AspNetUsers] ([Id]);
GO


--DROP
/*
-- 1. Drop the foreign key constraint first
ALTER TABLE [Permits] 
DROP CONSTRAINT [FK_Permits_AspNetUsers_CreatedBy];
GO

-- 2. Drop the index second
DROP INDEX [IX_Permits_CreatedBy] 
ON [Permits];
GO

ALTER TABLE [Permits] 
ALTER COLUMN [CreatedBy] uniqueidentifier NULL;

*/
