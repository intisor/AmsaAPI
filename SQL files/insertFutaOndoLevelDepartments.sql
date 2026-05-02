-- Created by GitHub Copilot in SSMS - review carefully before executing
-- Assigning members 35-40 to departments at the state level (Ondo, LevelId=8)

-- 35: Finance
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (35, 159); -- Asekun Ibrahim, Finance, State Level
-- 36: Welfare
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (36, 157); -- Akinwunmi Abdul Haleem, Welfare, State Level
-- 37: General
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (37, 166); -- Onabanjo Abdusalam, General, State Level
-- 38: President
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (38, 164); -- Salaudeen Umar, President, State Level
-- 39: Publicity
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (39, 163); -- Adeniyi Uthman, Publicity, State Level
-- 40: Taleem
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (40, 155); -- Sanusi Mubarak, Taleem, State Level

-- Assigning members 41-46 to departments at the unit level (FUTA, LevelId=38)

-- 41: Finance
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (41, 819); -- Onabanjo Abdus Salam, Finance, Unit Level
-- 42: Welfare
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (42, 817); -- Oyedele Fathi, Welfare, Unit Level
-- 43: General
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (43, 826); -- Bello Toheeb, General, Unit Level
-- 44: President
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (44, 824); -- Ipadeola Bilal Ahmad, President, Unit Level
-- 45: Publicity
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (45, 823); -- Akintayo Fareed Ahmad, Publicity, Unit Level
-- 46: Taleem
INSERT INTO dbo.MemberLevelDepartments (MemberId, LevelDepartmentId) VALUES (46, 815); -- Mudathir AbdulMujeeb, Taleem, Unit Level