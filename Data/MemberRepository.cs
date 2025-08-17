using AmsaAPI.Models;
using Microsoft.Data.SqlClient;

namespace AmsaAPI.Data
{
    public class MemberRepository(SqlDbConnection db)
    {
        private readonly SqlDbConnection _db = db;

        public List<Member> GetMembers()
        {
            var members = new List<Member>();
            using var connection = _db.GetOpenConnection();
            using var command = new SqlCommand("SELECT * FROM Members", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                members.Add(new Member
                {
                    MemberId = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.GetString(3),
                    PhoneNumber = reader.GetString(4),
                    MKANID = reader.GetInt32(5),
                    UnitId = reader.GetInt32(6)
                });
            }
            return members;
        }

    }
}
