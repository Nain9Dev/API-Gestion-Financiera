using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyOperations.Api.Contracts.Policies;
using PolicyOperations.Api.Security;
using PolicyOperations.Application.Policies;

namespace PolicyOperations.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyAuthorization.ReadPolicy)]
[Route("api/v1/policies")]
public sealed class PoliciesController : ControllerBase
{
    private readonly PolicyService _policyService;
    private readonly CurrentActorAccessor _currentActorAccessor;

    public PoliciesController(
        PolicyService policyService,
        CurrentActorAccessor currentActorAccessor)
    {
        _policyService = policyService;
        _currentActorAccessor = currentActorAccessor;
    }

    [HttpGet("{policyId:guid}")]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResponse>> GetById(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        var organizationId = _currentActorAccessor.GetOrganizationId(User);
        var policy = await _policyService.GetByIdAsync(
            organizationId,
            policyId,
            cancellationToken);
        SetEtag(policy);

        return Ok(policy);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<PolicyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PolicyResponse>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PolicyService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _currentActorAccessor.GetOrganizationId(User);
        var policies = await _policyService.ListAsync(
            organizationId,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(policies);
    }

    [HttpGet("{policyId:guid}/transitions")]
    [ProducesResponseType<IReadOnlyList<PolicyTransitionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PolicyTransitionResponse>>> ListTransitions(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        var organizationId = _currentActorAccessor.GetOrganizationId(User);
        var transitions = await _policyService.ListTransitionsAsync(
            organizationId,
            policyId,
            cancellationToken);

        return Ok(transitions);
    }

    [HttpPost]
    [Authorize(Policy = PolicyAuthorization.WritePolicy)]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PolicyResponse>> Create(
        CreatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = _currentActorAccessor.GetOrganizationId(User);
        var policy = await _policyService.CreateAsync(
            organizationId,
            request.PolicyNumber,
            request.InsuredAmount,
            request.Currency,
            cancellationToken);
        SetEtag(policy);

        return CreatedAtAction(
            nameof(GetById),
            new { policyId = policy.Id },
            policy);
    }

    [HttpPost("{policyId:guid}/activate")]
    [Authorize(Policy = PolicyAuthorization.WritePolicy)]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<PolicyResponse>> Activate(
        Guid policyId,
        ActivatePolicyRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var expectedVersion = PolicyEtag.ParseRequired(ifMatch);
        var commandContext = _currentActorAccessor.GetCommandContext(
            User,
            HttpContext.TraceIdentifier);
        var policy = await _policyService.ActivateAsync(
            commandContext,
            policyId,
            expectedVersion,
            request.InsuredPartyReference,
            request.CoverageStartDate,
            request.CoverageEndDate,
            cancellationToken);
        SetEtag(policy);

        return Ok(policy);
    }

    [HttpPost("{policyId:guid}/cancel")]
    [Authorize(Policy = PolicyAuthorization.WritePolicy)]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<PolicyResponse>> Cancel(
        Guid policyId,
        CancelPolicyRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var expectedVersion = PolicyEtag.ParseRequired(ifMatch);
        var commandContext = _currentActorAccessor.GetCommandContext(
            User,
            HttpContext.TraceIdentifier);
        var policy = await _policyService.CancelAsync(
            commandContext,
            policyId,
            expectedVersion,
            request.Reason,
            cancellationToken);
        SetEtag(policy);

        return Ok(policy);
    }

    private void SetEtag(PolicyResponse policy)
    {
        Response.Headers.ETag = PolicyEtag.Format(policy.Version);
    }
}
