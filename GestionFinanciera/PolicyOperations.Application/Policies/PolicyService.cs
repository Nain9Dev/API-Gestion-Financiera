using System.Security.Cryptography;
using PolicyOperations.Application.Abstractions;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Policies;

public sealed class PolicyService
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int SqlServerRowVersionLength = 8;

    private readonly TimeProvider _timeProvider;
    private readonly ICurrencyCatalog _currencyCatalog;
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PolicyService(
        TimeProvider timeProvider,
        ICurrencyCatalog currencyCatalog,
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork)
    {
        _timeProvider = timeProvider;
        _currencyCatalog = currencyCatalog;
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PolicyResponse> CreateAsync(
        Guid organizationId,
        string? policyNumber,
        decimal insuredAmount,
        string? currency,
        CancellationToken cancellationToken)
    {
        var policy = Policy.Create(
            organizationId,
            policyNumber,
            insuredAmount,
            currency,
            _timeProvider.GetUtcNow());

        if (!_currencyCatalog.IsSupported(policy.Currency))
        {
            throw new RequestValidationException(
                "Currency is not enabled for this deployment.",
                "currency_not_supported");
        }

        if (await _policyRepository.ExistsByNormalizedNumberAsync(
                organizationId,
                policy.NormalizedPolicyNumber,
                cancellationToken))
        {
            throw new PolicyNumberConflictException();
        }

        await _policyRepository.AddAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(policy);
    }

    public async Task<PolicyResponse> GetByIdAsync(
        Guid organizationId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(
            organizationId,
            policyId,
            trackChanges: false,
            cancellationToken);

        return Map(policy);
    }

    public async Task<PagedResult<PolicyResponse>> ListAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(pageNumber, pageSize);

        var skipValue = ((long)pageNumber - 1) * pageSize;

        if (skipValue > int.MaxValue)
        {
            throw new RequestValidationException(
                "Requested page is outside the supported range.");
        }

        var skip = (int)skipValue;
        var policies = await _policyRepository.ListAsync(
            organizationId,
            skip,
            pageSize,
            cancellationToken);
        var totalCount = await _policyRepository.CountAsync(organizationId, cancellationToken);

        return new PagedResult<PolicyResponse>(
            policies.Select(Map).ToArray(),
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<PolicyResponse> ActivateAsync(
        PolicyCommandContext commandContext,
        Guid policyId,
        byte[] expectedVersion,
        string? insuredPartyReference,
        DateOnly coverageStartDate,
        DateOnly coverageEndDate,
        CancellationToken cancellationToken)
    {
        var policy = await GetPolicyForUpdateAsync(
            commandContext.OrganizationId,
            policyId,
            expectedVersion,
            cancellationToken);
        var transition = policy.Activate(
            insuredPartyReference,
            coverageStartDate,
            coverageEndDate,
            commandContext.ActorSubject,
            commandContext.CorrelationId,
            _timeProvider.GetUtcNow());

        await _policyRepository.AddTransitionAsync(transition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(policy);
    }

    public async Task<PolicyResponse> CancelAsync(
        PolicyCommandContext commandContext,
        Guid policyId,
        byte[] expectedVersion,
        string? reason,
        CancellationToken cancellationToken)
    {
        var policy = await GetPolicyForUpdateAsync(
            commandContext.OrganizationId,
            policyId,
            expectedVersion,
            cancellationToken);
        var transition = policy.Cancel(
            reason,
            commandContext.ActorSubject,
            commandContext.CorrelationId,
            _timeProvider.GetUtcNow());

        await _policyRepository.AddTransitionAsync(transition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(policy);
    }

    public async Task<IReadOnlyList<PolicyTransitionResponse>> ListTransitionsAsync(
        Guid organizationId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        _ = await GetPolicyAsync(
            organizationId,
            policyId,
            trackChanges: false,
            cancellationToken);

        var transitions = await _policyRepository.ListTransitionsAsync(
            organizationId,
            policyId,
            cancellationToken);

        return transitions.Select(Map).ToArray();
    }

    private async Task<Policy> GetPolicyForUpdateAsync(
        Guid organizationId,
        Guid policyId,
        byte[] expectedVersion,
        CancellationToken cancellationToken)
    {
        ValidateExpectedVersion(expectedVersion);
        var policy = await GetPolicyAsync(
            organizationId,
            policyId,
            trackChanges: true,
            cancellationToken);

        if (policy.Version.Length != expectedVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(policy.Version, expectedVersion))
        {
            throw new PolicyConcurrencyException();
        }

        _policyRepository.SetExpectedVersion(policy, expectedVersion);
        return policy;
    }

    private async Task<Policy> GetPolicyAsync(
        Guid organizationId,
        Guid policyId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return await _policyRepository.GetByIdAsync(
                organizationId,
                policyId,
                trackChanges,
                cancellationToken)
            ?? throw new PolicyNotFoundException();
    }

    private static void ValidateExpectedVersion(byte[] expectedVersion)
    {
        if (expectedVersion.Length != SqlServerRowVersionLength)
        {
            throw new RequestValidationException(
                "If-Match must contain a valid SQL Server rowversion ETag.",
                "etag_invalid");
        }
    }

    private static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new RequestValidationException("Page number must be greater than zero.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {MaxPageSize}.");
        }
    }

    private static PolicyResponse Map(Policy policy)
    {
        return new PolicyResponse(
            policy.Id,
            policy.OrganizationId,
            policy.PolicyNumber,
            policy.InsuredAmount,
            policy.Currency,
            policy.InsuredPartyReference,
            policy.CoverageStartDate,
            policy.CoverageEndDate,
            policy.Status,
            policy.CreatedAtUtc,
            Convert.ToBase64String(policy.Version));
    }

    private static PolicyTransitionResponse Map(PolicyTransition transition)
    {
        return new PolicyTransitionResponse(
            transition.Id,
            transition.PolicyId,
            transition.FromStatus,
            transition.ToStatus,
            transition.OccurredAtUtc,
            transition.ActorSubject,
            transition.Reason,
            transition.CorrelationId);
    }
}
