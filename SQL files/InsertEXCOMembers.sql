-- Find LevelDepartmentId for National-level roles (LevelId=1)
DECLARE @LevelDepartmentIds TABLE (DepartmentId INT, LevelDepartmentId INT);
INSERT INTO @LevelDepartmentIds
SELECT ld.DepartmentId, ld.LevelDepartmentId
FROM LevelDepartments ld
WHERE ld.LevelId = 1;

-- Insert executives into MembersLevelDepartments
INSERT INTO MemberLevelDepartments (MemberId,LevelDepartmentId)
SELECT m.MemberId, ld.LevelDepartmentId
FROM Members m
CROSS APPLY (
	SELECT LevelDepartmentId from @LevelDepartmentIds ld
	 WHERE DepartmentId = CASE m.MKANID
        WHEN 1031 THEN 1  -- Abdulqudus Sulaiman: President
        WHEN 1032 THEN 3  -- Abdul-Khabeer Arowosere: VP Admin
        WHEN 1033 THEN 4  -- Sheriffdeen Saula: VP South-West
        WHEN 1034 THEN 5  -- Hafiz Musoddiq Aderibigbe: VP North
        WHEN 1035 THEN 6  -- Waliyullah Jimoh: VP South-South/South-East
        WHEN 1036 THEN 7  -- Masroor-Mahmud Adeeyo: General
        WHEN 1037 THEN 8  -- Hafiz Faruq Ademoye: Assistant General
        WHEN 1038 THEN 8  -- Yusuf Ahmad: Assistant General
        WHEN 1039 THEN 17 -- Ridwan Shittu: Finance
        WHEN 1040 THEN 18 -- Ibrahim Akinwande: Assistant Finance
        WHEN 1041 THEN 9  -- Hafiz Mahmud Adeleke: Taleem/Tarbiyya
        WHEN 1042 THEN 10 -- Abdur-Rahman Abolaji: Assistant Taleem/Tarbiyya
        WHEN 1043 THEN 10 -- Hafiz Adam Mahmud: Assistant Taleem/Tarbiyya
        WHEN 1044 THEN 11 -- Yusuf Oje: Tabligh
        WHEN 1045 THEN 12 -- Abdus-Salam Tijani: Assistant Tabligh
        WHEN 1046 THEN 12 -- Hafiz Fadlul-Munim Ariyo: Assistant Tabligh
        WHEN 1047 THEN 13 -- Abdur-Roqeeb Yaqoub: Welfare
        WHEN 1048 THEN 14 -- Ridwanullah Ajiferuke: Assistant Welfare
        WHEN 1049 THEN 14 -- Hafiz Muhammad Aliyu: Assistant Welfare
        WHEN 1050 THEN 15 -- Hassan Faremi: Sport
        WHEN 1051 THEN 16 -- Hafiz Hayatur-Rahman Abdullah: Assistant Sport
        WHEN 1052 THEN 19 -- Hafiz Abdur-Rasheed Lawal: Tajneed
        WHEN 1053 THEN 20 -- Ridwanullah Robiu: Assistant Tajneed
        WHEN 1054 THEN 20 -- Abdul-Qayyum Ajibike: Assistant Tajneed
        WHEN 1055 THEN 21 -- Hasbiyallah Robiu: Publicity
        WHEN 1056 THEN 22 -- Abdul-Mujeeb Murtadha: Assistant Publicity
        WHEN 1057 THEN 22 -- Abdul-Gaffar Banuso: Assistant Publicity
        WHEN 1058 THEN 22 -- Aleemdeen Towolawi: Assistant Publicity
        WHEN 1059 THEN 22 -- Ataur-Rahman Alaka: Assistant Publicity
        WHEN 1060 THEN 22 -- Mubarak-Ahmad Adeeyo: Assistant Publicity
        WHEN 1061 THEN 23 -- Abdul-Basit Oladele: Health
        WHEN 1062 THEN 24 -- Hafiz Abdul-Warith Omoniyi: Assistant Health
        WHEN 1063 THEN 25 -- Intisor Abdul-Awwal: Secondary School
        WHEN 1064 THEN 26 -- Lawal Kolawole: Assistant Secondary School
    END
) ld
WHERE m.MKANID BETWEEN 1031 AND 1064;