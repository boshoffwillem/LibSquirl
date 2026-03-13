using System.Text.Json.Serialization;
using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Organizations;

public sealed class OrganizationsApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options),
        IOrganizationsApi
{
    public async Task<List<Organization>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<Organization>>("/v1/organizations", cancellationToken);
    }

    public async Task<Organization> GetAsync(CancellationToken cancellationToken = default)
    {
        OrganizationWrapper wrapper = await GetAsync<OrganizationWrapper>(
            $"{OrgPath}",
            cancellationToken
        );
        return wrapper.Organization;
    }

    public async Task<Organization> UpdateAsync(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        OrganizationWrapper wrapper = await PatchAsync<OrganizationWrapper>(
            $"{OrgPath}",
            request,
            cancellationToken
        );
        return wrapper.Organization;
    }

    public async Task<List<Plan>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        PlansWrapper wrapper = await GetAsync<PlansWrapper>($"{OrgPath}/plans", cancellationToken);
        return wrapper.Plans;
    }

    public async Task<Subscription> GetSubscriptionAsync(
        CancellationToken cancellationToken = default
    )
    {
        SubscriptionWrapper wrapper = await GetAsync<SubscriptionWrapper>(
            $"{OrgPath}/subscription",
            cancellationToken
        );
        return wrapper.Subscription;
    }

    public async Task<List<Invoice>> ListInvoicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        InvoicesWrapper wrapper = await GetAsync<InvoicesWrapper>(
            $"{OrgPath}/invoices",
            cancellationToken
        );
        return wrapper.Invoices;
    }

    public async Task<OrgUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        OrgUsageWrapper wrapper = await GetAsync<OrgUsageWrapper>(
            $"{OrgPath}/usage",
            cancellationToken
        );
        return wrapper.Organization;
    }

    public async Task<List<Member>> ListMembersAsync(CancellationToken cancellationToken = default)
    {
        MembersWrapper wrapper = await GetAsync<MembersWrapper>(
            $"{OrgPath}/members",
            cancellationToken
        );
        return wrapper.Members;
    }

    public async Task<Member> GetMemberAsync(
        string username,
        CancellationToken cancellationToken = default
    )
    {
        MemberWrapper wrapper = await GetAsync<MemberWrapper>(
            $"{OrgPath}/members/{username}",
            cancellationToken
        );
        return wrapper.Member;
    }

    public async Task<Member> AddMemberAsync(
        AddMemberRequest request,
        CancellationToken cancellationToken = default
    )
    {
        MemberWrapper wrapper = await PostAsync<MemberWrapper>(
            $"{OrgPath}/members",
            request,
            cancellationToken
        );
        return wrapper.Member;
    }

    public async Task<Member> UpdateMemberAsync(
        string username,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default
    )
    {
        MemberWrapper wrapper = await PatchAsync<MemberWrapper>(
            $"{OrgPath}/members/{username}",
            request,
            cancellationToken
        );
        return wrapper.Member;
    }

    public async Task RemoveMemberAsync(
        string username,
        CancellationToken cancellationToken = default
    )
    {
        await DeleteNoContentAsync($"{OrgPath}/members/{username}", cancellationToken);
    }

    public async Task<List<Invite>> ListInvitesAsync(CancellationToken cancellationToken = default)
    {
        InvitesWrapper wrapper = await GetAsync<InvitesWrapper>(
            $"{OrgPath}/invites",
            cancellationToken
        );
        return wrapper.Invites;
    }

    public async Task<Invite> CreateInviteAsync(
        CreateInviteRequest request,
        CancellationToken cancellationToken = default
    )
    {
        InviteWrapper wrapper = await PostAsync<InviteWrapper>(
            $"{OrgPath}/invites",
            request,
            cancellationToken
        );
        return wrapper.Invite;
    }

    private sealed class OrganizationWrapper
    {
        [JsonPropertyName("organization")]
        public Organization Organization { get; } = null!;
    }

    private sealed class PlansWrapper
    {
        [JsonPropertyName("plans")]
        public List<Plan> Plans { get; } = [];
    }

    private sealed class SubscriptionWrapper
    {
        [JsonPropertyName("subscription")]
        public Subscription Subscription { get; } = null!;
    }

    private sealed class InvoicesWrapper
    {
        [JsonPropertyName("invoices")]
        public List<Invoice> Invoices { get; } = [];
    }

    private sealed class OrgUsageWrapper
    {
        [JsonPropertyName("organization")]
        public OrgUsage Organization { get; } = null!;
    }

    private sealed class MembersWrapper
    {
        [JsonPropertyName("members")]
        public List<Member> Members { get; } = [];
    }

    private sealed class MemberWrapper
    {
        [JsonPropertyName("member")]
        public Member Member { get; } = null!;
    }

    private sealed class InvitesWrapper
    {
        [JsonPropertyName("invites")]
        public List<Invite> Invites { get; } = [];
    }

    private sealed class InviteWrapper
    {
        [JsonPropertyName("invited")]
        public Invite Invite { get; } = null!;
    }
}
