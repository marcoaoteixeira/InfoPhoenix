SELECT
    [ID],
    [Label],
    [DirectoryPath],
	[Order],
	[LastIndexingTime],
    [CreatedAt],
    [ModifiedAt]
FROM [DocumentDirectories]
WHERE
    [ID] = @ID;