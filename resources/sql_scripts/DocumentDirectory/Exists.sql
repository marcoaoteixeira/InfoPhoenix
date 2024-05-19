SELECT EXISTS(
	SELECT
		[ID]
	FROM [DocumentDirectories]
	WHERE
		[ID] = @ID
);