CREATE TABLE IF NOT EXISTS [DocumentDirectories] (
    [ID]				TEXT	NOT NULL,
    [Label]				TEXT	NOT NULL,
	[DirectoryPath]		TEXT	NOT NULL,
	[Order]				INTEGER	NOT NULL	DEFAULT 0,
	[LastIndexingTime]	TEXT	NULL,
	[CreatedAt]			TEXT	NOT NULL,
	[ModifiedAt]		TEXT	NULL,
	
	PRIMARY KEY ([ID]),
	
	CONSTRAINT [UQ_DocumentDirectories_DirectoryPath] UNIQUE ([DirectoryPath])
);

CREATE TABLE IF NOT EXISTS [Documents] (
    [ID]					TEXT	NOT NULL,
	[DocumentDirectoryID]	TEXT	NOT NULL,
	[FilePath]				TEXT	NOT NULL,
	[Content]				TEXT	NOT NULL,
	[RawFile]				BLOB	NULL,
	[LastIndexingTime]		TEXT	NULL,
	[LastWriteTime]			TEXT	NULL,
	[CreatedAt]				TEXT	NOT NULL,
	[ModifiedAt]			TEXT	NULL,
	
	PRIMARY KEY ([ID]),
	
	FOREIGN KEY([DocumentDirectoryID]) REFERENCES [DocumentDirectories]([ID]) ON DELETE CASCADE,
	
	CONSTRAINT [UQ_Documents_FilePath] UNIQUE ([FilePath])
);