SELECT
    [ID],
    [DocumentDirectoryID],
    [FilePath],
    [Content],
    [RawFile],
    [LastIndexingTime],
	[LastWriteTime],
    [CreatedAt],
    [ModifiedAt]
FROM [Documents]
WHERE
    [ID] = @ID;