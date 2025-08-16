INSERT INTO LevelDepartments (LevelId,DepartmentId)
	SELECT l.LevelId, d.DepartmentId
	FROM Levels l 
	CROSS JOIN Departments d