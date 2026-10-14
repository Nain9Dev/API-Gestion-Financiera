using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PolicyOperations.Api.Configuration;
using PolicyOperations.Api.Contracts.Demo;
using PolicyOperations.Api.Demo;
using PolicyOperations.Api.Security;
using PolicyOperations.Application.Policies;

namespace PolicyOperations.Api.Controllers;

[ApiController]
[Route("api/v1/demo")]
public sealed partial class PublicDemoController : ControllerBase
{
    private const string DemoActorSubject = "public-portfolio-demo";
    private const string DemoCurrency = "EUR";
    private const decimal DemoInsuredAmount = 125_000m;

    private readonly ILogger<PublicDemoController> _logger;
    private readonly PolicyService _policyService;
    private readonly PublicDemoDataPruner _dataPruner;
    private readonly PublicDemoOptions _options;
    private readonly TimeProvider _timeProvider;

    public PublicDemoController(
        ILogger<PublicDemoController> logger,
        PolicyService policyService,
        PublicDemoDataPruner dataPruner,
        IOptions<PublicDemoOptions> options,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _policyService = policyService;
        _dataPruner = dataPruner;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    [HttpPost("run")]
    [AllowAnonymous]
    [EnableRateLimiting(PublicDemoRateLimit.PolicyName)]
    [RequestSizeLimit(1024)]
    [ProducesResponseType<PublicDemoRunResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PublicDemoRunResponse>> Run(
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        if (!_options.Enabled)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Public demo is not enabled.",
                Detail = "This deployment does not expose the public demo scenario.",
                Instance = HttpContext.Request.Path
            };
            problemDetails.Extensions["code"] = "public_demo_disabled";
            problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return NotFound(problemDetails);
        }

        if (Request.ContentLength.GetValueOrDefault() > 0 ||
            Request.Headers.TransferEncoding.Count > 0)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Public demo request body is not allowed.",
                Detail = "Run the fixed scenario without sending a request body.",
                Instance = HttpContext.Request.Path
            };
            problemDetails.Extensions["code"] = "public_demo_body_not_allowed";
            problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return BadRequest(problemDetails);
        }

        var now = _timeProvider.GetUtcNow();
        var runStopwatch = Stopwatch.StartNew();
        var cleanupCompleted = await _dataPruner.TryPruneAsync(
            _options.OrganizationId,
            now.AddHours(-_options.RetentionHours),
            cancellationToken);

        var runId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var policyNumber = $"SYNTH-WEB-{now:yyyyMMddHHmmss}-{runId}";
        var insuredPartyReference = $"SYNTH-INSURED-{runId}";
        var coverageStartDate = DateOnly.FromDateTime(now.UtcDateTime);
        var coverageEndDate = coverageStartDate.AddYears(1).AddDays(-1);
        var commandContext = new PolicyCommandContext(
            _options.OrganizationId,
            DemoActorSubject,
            HttpContext.TraceIdentifier);

        var stepStopwatch = Stopwatch.StartNew();
        var draft = await _policyService.CreateAsync(
            _options.OrganizationId,
            policyNumber,
            DemoInsuredAmount,
            DemoCurrency,
            cancellationToken);
        var createDurationMs = stepStopwatch.ElapsedMilliseconds;
        var draftVersion = Convert.FromBase64String(draft.Version);
        var policyPath = $"/api/v1/policies/{draft.Id}";

        stepStopwatch.Restart();
        var active = await _policyService.ActivateAsync(
            commandContext,
            draft.Id,
            draftVersion,
            insuredPartyReference,
            coverageStartDate,
            coverageEndDate,
            cancellationToken);
        var activateDurationMs = stepStopwatch.ElapsedMilliseconds;

        var staleUpdateRejected = false;
        stepStopwatch.Restart();

        try
        {
            _ = await _policyService.CancelAsync(
                commandContext,
                draft.Id,
                draftVersion,
                "Synthetic stale public demo request",
                cancellationToken);
        }
        catch (PolicyConcurrencyException)
        {
            staleUpdateRejected = true;
        }

        var staleDurationMs = stepStopwatch.ElapsedMilliseconds;

        if (!staleUpdateRejected)
        {
            throw new InvalidOperationException(
                "The public demo expected the stale policy version to be rejected.");
        }

        stepStopwatch.Restart();
        var cancelled = await _policyService.CancelAsync(
            commandContext,
            draft.Id,
            Convert.FromBase64String(active.Version),
            "Synthetic public demo request",
            cancellationToken);
        var cancelDurationMs = stepStopwatch.ElapsedMilliseconds;

        stepStopwatch.Restart();
        var transitions = await _policyService.ListTransitionsAsync(
            _options.OrganizationId,
            draft.Id,
            cancellationToken);
        var auditDurationMs = stepStopwatch.ElapsedMilliseconds;

        var steps = new[]
        {
            new PublicDemoStepResponse(
                "create_draft",
                HttpMethods.Post,
                "/api/v1/policies",
                StatusCodes.Status201Created,
                "succeeded",
                draft.Status.ToString(),
                PolicyEtag.Format(draft.Version),
                null,
                createDurationMs),
            new PublicDemoStepResponse(
                "activate_policy",
                HttpMethods.Post,
                $"{policyPath}/activate",
                StatusCodes.Status200OK,
                "succeeded",
                active.Status.ToString(),
                PolicyEtag.Format(active.Version),
                null,
                activateDurationMs),
            new PublicDemoStepResponse(
                "reject_stale_update",
                HttpMethods.Post,
                $"{policyPath}/cancel",
                StatusCodes.Status412PreconditionFailed,
                "rejected_as_expected",
                active.Status.ToString(),
                PolicyEtag.Format(draft.Version),
                "concurrency_conflict",
                staleDurationMs),
            new PublicDemoStepResponse(
                "cancel_policy",
                HttpMethods.Post,
                $"{policyPath}/cancel",
                StatusCodes.Status200OK,
                "succeeded",
                cancelled.Status.ToString(),
                PolicyEtag.Format(cancelled.Version),
                null,
                cancelDurationMs),
            new PublicDemoStepResponse(
                "read_audit_trail",
                HttpMethods.Get,
                $"{policyPath}/transitions",
                StatusCodes.Status200OK,
                transitions.Count == 2 ? "succeeded" : "unexpected_result",
                null,
                null,
                null,
                auditDurationMs)
        };

        LogPublicDemoCompleted(_logger, runId, cancelled.Id, transitions.Count);

        return Ok(new PublicDemoRunResponse(
            runId,
            now,
            _options.RetentionHours,
            runStopwatch.ElapsedMilliseconds,
            cleanupCompleted,
            steps,
            cancelled,
            transitions));
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Public demo run {RunId} completed for policy {PolicyId} with {TransitionCount} transitions.")]
    private static partial void LogPublicDemoCompleted(
        ILogger logger,
        string runId,
        Guid policyId,
        int transitionCount);
}
