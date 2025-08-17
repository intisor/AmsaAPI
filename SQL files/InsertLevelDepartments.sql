-- Insert all departments for all levels except the restricted departments
INSERT INTO LevelDepartments (LevelId, DepartmentId)
	SELECT l.LevelId, d.DepartmentId
	FROM Levels l 
	CROSS JOIN Departments d
	WHERE d.DepartmentName NOT IN ('VP Admin', 'VP South-West', 'VP North', 'VP South-South/South-East')
	AND NOT EXISTS (
		SELECT 1 
		FROM LevelDepartments ld 
		WHERE ld.LevelId = l.LevelId AND ld.DepartmentId = d.DepartmentId
	);

-- Insert specific departments only for Level 1
INSERT INTO LevelDepartments (LevelId, DepartmentId)
	SELECT 1 AS LevelId, d.DepartmentId
	FROM Departments d
	WHERE d.DepartmentName IN ('VP Admin', 'VP South-West', 'VP North', 'VP South-South/South-East')
	AND NOT EXISTS (
		SELECT 1
		FROM LevelDepartments ld
		WHERE ld.LevelId = 1 AND ld.DepartmentId = d.DepartmentId
	);