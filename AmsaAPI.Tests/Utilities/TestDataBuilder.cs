using AmsaAPI.Data;

namespace AmsaAPI.Tests.Utilities;

/// <summary>
/// Helper for building test entities with fluent API
/// </summary>
public class TestDataBuilder
{
    public static MemberBuilder CreateMember()
    {
        return new MemberBuilder();
    }

    public static UnitBuilder CreateUnit()
    {
        return new UnitBuilder();
    }

    public static DepartmentBuilder CreateDepartment()
    {
        return new DepartmentBuilder();
    }

    public class MemberBuilder
    {
        private Member _member = new()
        {
            FirstName = "Test",
            LastName = "Member",
            MkanId = new Random().Next(10000, 99999),
            Gender = "M",
            Email = "test@example.com",
            PhoneNumber = "08000000000"
        };

        public MemberBuilder WithFirstName(string firstName)
        {
            _member.FirstName = firstName;
            return this;
        }

        public MemberBuilder WithLastName(string lastName)
        {
            _member.LastName = lastName;
            return this;
        }

        public MemberBuilder WithMkanId(int mkanId)
        {
            _member.MkanId = mkanId;
            return this;
        }

        public MemberBuilder WithUnitId(int unitId)
        {
            _member.UnitId = unitId;
            return this;
        }

        public MemberBuilder WithEmail(string email)
        {
            _member.Email = email;
            return this;
        }

        public Member Build()
        {
            return _member;
        }
    }

    public class UnitBuilder
    {
        private Unit _unit = new()
        {
            UnitName = "Test Unit",
            StateId = 1
        };

        public UnitBuilder WithUnitName(string name)
        {
            _unit.UnitName = name;
            return this;
        }

        public UnitBuilder WithStateId(int stateId)
        {
            _unit.StateId = stateId;
            return this;
        }

        public Unit Build()
        {
            return _unit;
        }
    }

    public class DepartmentBuilder
    {
        private Department _dept = new()
        {
            DepartmentName = "Test Department",
            DepartmentCode = "TD001"
        };

        public DepartmentBuilder WithName(string name)
        {
            _dept.DepartmentName = name;
            return this;
        }

        public DepartmentBuilder WithCode(string code)
        {
            _dept.DepartmentCode = code;
            return this;
        }

        public Department Build()
        {
            return _dept;
        }
    }
}
