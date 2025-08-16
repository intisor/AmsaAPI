INSERT INTO Levels (LevelType,StateId,UnitId)
	SELECT 'State' AS LevelType,StateId,NULL AS UnitId
	FROM States s

	UNION 

	SELECT 'Unit' AS LevelType,NULL as StateId,UnitId
	FROM Units u