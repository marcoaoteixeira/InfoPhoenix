UPDATE [Documents] SET
    [DocumentDirectoryID] = @DocumentDirectoryID,
    [FilePath] = @FilePath,
    [Content] = @Content,
    [RawFile] = @RawFile,
    [LastIndexingTime] = @LastIndexingTime,
	[LastWriteTime] = @LastWriteTime,
    [CreatedAt] = @CreatedAt,
    [ModifiedAt] = @ModifiedAt
WHERE
	[ID] = @ID;