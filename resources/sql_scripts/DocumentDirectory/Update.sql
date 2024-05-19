UPDATE [DocumentDirectories] SET
    [Label] = @Label,
    [DirectoryPath] = @DirectoryPath,
	[Order] = @Order,
	[LastIndexingTime] = @LastIndexingTime,
    [CreatedAt] = @CreatedAt,
    [ModifiedAt] = @ModifiedAt
WHERE
	[ID] = @ID;