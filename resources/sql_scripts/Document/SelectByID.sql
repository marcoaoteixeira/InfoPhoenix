SELECT
    [ID],
    [DocumentDirectoryID],
    [FilePath],
    [Content],
    NULL AS [RawFile],
    [LastIndexingTime],
	[LastWriteTime],
    [CreatedAt],
    [ModifiedAt]
FROM [Documents]
WHERE
	[ID] = @ID;