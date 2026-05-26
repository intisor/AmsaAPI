namespace AmsaAPI.DTOs;

public class MemberSummaryDto : MemberSummaryResponse
{
}

public class MemberDetailDto : MemberDetailResponse
{
    public int MkanId
    {
        get => Mkanid;
        set => Mkanid = value;
    }
}

public class DepartmentDto : DepartmentSummaryDto
{
}
