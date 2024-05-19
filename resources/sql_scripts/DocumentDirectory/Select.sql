SELECT
    [ID],
    [Label],
    [DirectoryPath],
	[Order],
	[LastIndexingTime],
    [CreatedAt],
    [ModifiedAt]
FROM [DocumentDirectories]
ORDER BY
	[Order] ASC;