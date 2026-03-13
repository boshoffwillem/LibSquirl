using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Organizations;

public interface IOrganizationsApi
{
    Task<List<Organization>> ListAsync(CancellationToken cancellationToken = default);
    Task<Organization> GetAsync(CancellationToken cancellationToken = default);
    Task<Organization> UpdateAsync(UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<List<Plan>> ListPlansAsync(CancellationToken cancellationToken = default);
    Task<Subscription> GetSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<List<Invoice>> ListInvoicesAsync(CancellationToken cancellationToken = default);
    Task<OrgUsage> GetUsageAsync(CancellationToken cancellationToken = default);
    Task<List<Member>> ListMembersAsync(CancellationToken cancellationToken = default);
    Task<Member> GetMemberAsync(string username, CancellationToken cancellationToken = default);
    Task<Member> AddMemberAsync(AddMemberRequest request, CancellationToken cancellationToken = default);
    Task<Member> UpdateMemberAsync(string username, UpdateMemberRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(string username, CancellationToken cancellationToken = default);
    Task<List<Invite>> ListInvitesAsync(CancellationToken cancellationToken = default);
    Task<Invite> CreateInviteAsync(CreateInviteRequest request, CancellationToken cancellationToken = default);
}
