using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Get All Members Endpoint
public sealed class GetAllMembersEndpoint(MemberService memberService) : Endpoint<EmptyRequest, List<MemberDetailResponse>>
{
    public override void Configure()
    {
        Get("/api/members");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members with complete details");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var result = await memberService.GetAllMembersAsync(ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    case ErrorType.Conflict:
                        await Send.ResultAsync(Results.Conflict(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}

// Get Member By ID Endpoint
public sealed class GetMemberByIdEndpoint(MemberService memberService) : Endpoint<GetMemberByIdRequest, MemberDetailResponse>
{
    public override void Configure()
    {
        Get("/api/members/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get member by ID with full details");
    }

    public override async Task HandleAsync(GetMemberByIdRequest req, CancellationToken ct)
    {
        var result = await memberService.GetMemberByIdAsync(req.Id, ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}

// Get Member By MKAN ID Endpoint
public sealed class GetMemberByMkanIdEndpoint(MemberService memberService) : Endpoint<GetMemberByMkanIdRequest, MemberDetailResponse>
{
    public override void Configure()
    {
        Get("/api/members/mkan/{mkanid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get member by MKAN ID with full details");
    }

    public override async Task HandleAsync(GetMemberByMkanIdRequest req, CancellationToken ct)
    {
        var result = await memberService.GetMemberByMkanIdAsync(req.MkanId, ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}

// Get Members By Unit Endpoint
public sealed class GetMembersByUnitEndpoint(MemberService memberService) : Endpoint<GetMembersByUnitRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/unit/{unitid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members in a specific unit");
    }

    public override async Task HandleAsync(GetMembersByUnitRequest req, CancellationToken ct)
    {
        var result = await memberService.GetMembersByUnitAsync(req.UnitId, ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}

// Get Members By Department Endpoint
public sealed class GetMembersByDepartmentEndpoint(MemberService memberService) : Endpoint<GetMembersByDepartmentRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/department/{departmentid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members in a specific department");
    }

    public override async Task HandleAsync(GetMembersByDepartmentRequest req, CancellationToken ct)
    {
        var result = await memberService.GetMembersByDepartmentAsync(req.DepartmentId, ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}

// Search Members By Name Endpoint
public sealed class SearchMembersByNameEndpoint(MemberService memberService) : Endpoint<SearchMembersByNameRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/search/{name}");
        AllowAnonymous();
        Summary(s => s.Summary = "Search members by first or last name");
    }

    public override async Task HandleAsync(SearchMembersByNameRequest req, CancellationToken ct)
    {
        var result = await memberService.SearchMembersByNameAsync(req.Name, ct);
        
        await result.Match(
            onSuccess: async value => await Send.OkAsync(value, ct),
            onFailure: async (errorType, message) =>
            {
                switch (errorType)
                {
                    case ErrorType.NotFound:
                        await Send.NotFoundAsync(ct);
                        break;
                    case ErrorType.Validation:
                    case ErrorType.BadRequest:
                        await Send.ResultAsync(Results.BadRequest(message));
                        break;
                    default:
                        await Send.ResultAsync(Results.Problem(message));
                        break;
                }
            });
    }
}