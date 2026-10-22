# Traceability

- Document version: 0.2
- Status: Active
- Date: 2026-10-22
- Scope: Approved rule identifier to the test that demonstrates it

## How to read this

A rule is *demonstrated* when a named, runnable test fails if the rule is removed. Rules covered
only by a SQL constraint or by manual observation are marked as such, because a constraint that no
test exercises is a design intention, not evidence.

Suites:

- `D` — `PolicyOperations.Domain.Tests` (no infrastructure)
- `A` — `PolicyOperations.Application.Tests` (no database)
- `I` — `PolicyOperations.IntegrationTests` (real SQL Server)
- `M` — `PolicyOperations.IntegrationTests` migration suite (real SQL Server)

## Policy identity and value

| Rule | Suite | Test |
|---|---|---|
| `POL-001` | D | `CreateWithValidValuesCreatesOrganizationScopedDraftPolicy` |
| `POL-002` | D | `CreateWithoutPolicyNumberThrowsRuleViolation`, `CreateWithPolicyNumberOverMaximumLengthThrowsRuleViolation` |
| `POL-003` | A | `CreatePersistsNormalizedPolicyAndSavesOnce` |
| `POL-004` | A, I | `CreateRejectsDuplicateNormalizedNumberInsideTheSameOrganization`, `DuplicateNormalizedNumberWithinOrganizationReturnsConflict` |
| `POL-005` | A, I | `CreateAllowsTheSameNumberInAnotherOrganization`, `PolicyNumberAndReadsAreIsolatedByOrganization` |
| `POL-006` | D, I | `CreateWithInvalidInsuredAmountThrowsRuleViolation`, `CreateAboveMaximumInsuredAmountThrowsRuleViolation`, `CreateWithMaximumAmountPersistsExactValue` |
| `POL-007` | D, I | `CreateWithMaximumInsuredAmountPreservesExactValue`, `CreateWithInvalidAmountReturnsSafeValidationProblem` |
| `POL-008` | D | `CreateWithInvalidCurrencyThrowsRuleViolation` |
| `POL-009` | A, I | `CreateRejectsCurrencyThatIsNotEnabledForTheDeployment`, `UnsupportedCurrencyReturnsSafeValidationProblem` |
| `POL-010` | — | Versioned configuration in `appsettings.json`; no test asserts the specific list, by design |
| `POL-011` | — | Structural: no mutation command exists for amount or currency |
| `POL-012` | D | `CreateWithNonUtcTimestampThrowsRuleViolation` |

## Organization and authorization

| Rule | Suite | Test |
|---|---|---|
| `SEC-001` | I | `UnauthenticatedPolicyRequestReturnsProblemDetails` |
| `SEC-002` | I | `ReaderCannotCreatePolicy` |
| `SEC-003` | I | `CreateThenGetReturnsScopedDraftWithEtag`, `ActivateThenCancelUsesConcurrencyAndAppendsAudit` |
| `SEC-004` | A, I | `ActivateWritesOneAuditRecordAndSavesOnce`, `ActivateThenCancelUsesConcurrencyAndAppendsAudit` |
| `SEC-005` | I | `PolicyNumberAndReadsAreIsolatedByOrganization` |
| `SEC-006` | — | Structural: request contracts expose neither field |
| `SEC-007` | A, I | `ListReturnsOnlyTheRequestingOrganization`, `PolicyNumberAndReadsAreIsolatedByOrganization` |
| `SEC-008` | A, I | `GetByIdTreatsAnotherOrganizationsPolicyAsNotFound`, `GetMissingPolicyReturnsNotFoundProblem` |
| `SEC-009` | I | `UnsupportedCurrencyReturnsSafeValidationProblem`, `CreateWithInvalidAmountReturnsSafeValidationProblem` |
| `SEC-010` | — | Boundary rule; enforced by review, not by a test |

## Lifecycle and completeness

| Rule | Suite | Test |
|---|---|---|
| Draft to Active | D, A | `ActivateDraftStoresCompletenessAndReturnsAuditTransition`, `ActivateWritesOneAuditRecordAndSavesOnce` |
| Draft or Active to Cancelled | D, A | `CancelDraftRequiresReasonAndReturnsAuditTransition`, `CancelRecordsTheReasonInTheAuditTrail` |
| Cancelled is terminal | D, A | `CancelledPolicyCannotReactivate`, `CancelIsTerminal` |
| Activation completeness | D, I | `ActivateWithInvalidCoverageDoesNotChangeDraft`, `ActivationRequiresEtagAndCompleteData` |
| Cancellation reason required | D | `CancelWithoutReasonDoesNotChangePolicy` |
| Transition actor required | D | `InvalidTransitionActorDoesNotMutateDraft` |

## Concurrency

| Rule | Suite | Test |
|---|---|---|
| `CONC-001` | I | `ActivateThenCancelUsesConcurrencyAndAppendsAudit` |
| `CONC-002` | A, I | `ResponseExposesTheRowVersionAsBase64`, `CreateThenGetReturnsScopedDraftWithEtag` |
| `CONC-003` | A | `ActivateRejectsAnEtagThatIsNotASqlServerRowVersion` |
| `CONC-004` | I | `ActivationRequiresEtagAndCompleteData` |
| `CONC-005` | A, I | `ActivateRejectsAnEtagThatIsNotASqlServerRowVersion`, `ActivationRequiresEtagAndCompleteData` |
| `CONC-006` | A, I | `ActivateRejectsAStaleVersionWithoutWritingAnAuditRecord`, `ActivateThenCancelUsesConcurrencyAndAppendsAudit` |
| `CONC-007` | A | `CancelIsTerminal` |

## Audit trail

| Rule | Suite | Test |
|---|---|---|
| One record per accepted transition | A, I | `ActivateWritesOneAuditRecordAndSavesOnce`, `ActivateThenCancelUsesConcurrencyAndAppendsAudit` |
| Reason recorded on cancellation | A | `CancelRecordsTheReasonInTheAuditTrail` |
| Ordered by occurrence then identifier | A | `ListTransitionsReturnsOccurrenceOrder` |
| Scoped to the organization | A | `ListTransitionsRejectsAPolicyOutsideTheOrganization` |
| Update and audit share one save | A | `ActivateWritesOneAuditRecordAndSavesOnce` |

## Pagination

| Rule | Suite | Test |
|---|---|---|
| `PAGE-001` | A | `ListRejectsPagingOutsideTheSupportedRange` |
| `PAGE-002` | A, I | `ListRejectsPagingOutsideTheSupportedRange`, `ListReturnsBoundedPageAndTotalCount` |
| `PAGE-003` | A, I | `ListRejectsAPageOffsetThatWouldOverflowTheSqlOffset`, `ListWithOverflowingPageReturnsValidationProblem` |
| `PAGE-004` | I | `ListReturnsBoundedPageAndTotalCount` |

## Public portfolio demo

| Rule | Suite | Test |
|---|---|---|
| `DEMO-001` | — | Configuration gate; the disabled path is asserted by review of `PublicDemoOptions` validation |
| `DEMO-002` | I | `PublicDemoRunsCompleteSyntheticLifecycleWithoutAuthentication` |
| `DEMO-003` | I | `PublicDemoRunsCompleteSyntheticLifecycleWithoutAuthentication` |
| `DEMO-004` | I | `PublicDemoRunsCompleteSyntheticLifecycleWithoutAuthentication` |
| `DEMO-005` | I | `UnauthenticatedPolicyRequestReturnsProblemDetails`, `ReaderCannotCreatePolicy` |
| `DEMO-006` | — | Configured limit; observed manually against the live deployment |
| `DEMO-007` | I | `PublicDemoPrunerDeletesOnlyExpiredDemoPolicies` |
| `DEMO-008` | I | `PublicDemoRunsCompleteSyntheticLifecycleWithoutAuthentication` |

## Free-tier resilience (ADR-006, proposed)

| Rule | Suite | Test |
|---|---|---|
| `OPS-001` transient conditions are recognised, other failures are not | I | `TransientDatabaseConditionsAreRecognisedAndOtherFailuresAreNot` |
| `OPS-002` cleanup degrades without failing the run | I | `PublicDemoPrunerDeletesOnlyExpiredDemoPolicies` covers the success path; the degraded path is covered by construction and logged |
| `OPS-003` per-step route, status and server timing are reported | I | `PublicDemoRunReportsPerStepRouteStatusAndServerTiming` |
| `OPS-004` baseline security headers on every response | I | `EveryResponseCarriesTheBaselineSecurityHeaders` |
| `OPS-005` demo page keeps a restrictive content security policy | I | `DemoPageIsServedWithARestrictiveContentSecurityPolicy` |
| `OPS-006` transient failure returns `503` with `Retry-After` | — | **Gap.** Producing a genuine transient SQL failure in a test needs a fault-injection seam; only the recognition function is covered today |

## Migration boundary

| Rule | Suite | Test |
|---|---|---|
| Legacy rows preserved | M | `LifecycleMigrationPreservesLegacyRows` |
| Duplicate legacy numbers stop the upgrade | M | `LifecycleMigrationStopsBeforeLosingDuplicatePolicyNumbers` |
| Ownership and currency are never invented (`51003`) | M | `OrganizationMigrationRefusesToInventOwnershipOrCurrency` |
| Down migration refuses data loss (`51004`) | M | `OrganizationDownMigrationRefusesDataLoss` |

## API description

| Rule | Suite | Test |
|---|---|---|
| Quoted `If-Match` example and JSON media types in OpenAPI | I | `SwaggerExplainsQuotedEtagAndDocumentsJsonResponses` |
| Liveness and readiness are separate | I | `LivenessAndSqlReadinessAreSeparateAndHealthy` |

## Client presentation (spec 002, ADR-007 proposed)

Suite `C` — `PolicyOperations.Client` Vitest (`npm test`, no browser).

| Requirement | Suite | Test |
|---|---|---|
| `REQ-102` successful command lights every layer and moves the domain state | C | `lights every layer down to SQL Server for a successful lifecycle command`, `moves the domain state to the resulting status of each successful command` |
| `REQ-103` `412` stops at the concurrency guard without state or ledger change | C | `draws a 412 as a rejected path stopped at the concurrency guard, without touching state or ledger` |
| `REQ-104` one ledger row per transition, two at the end | C | `appends one ledger row per state transition so a full run ends with two rows` |
| `REQ-110` run labelled `live` or `simulated` by source | C | `labels a 200 from the configured API as live…`, `labels the in-browser fallback as simulated when the network fails`, `…when the API answers with an error status` |
| `REQ-111` base URL from `VITE_API_BASE_URL`, same-origin when absent | C | `prefixes the request with VITE_API_BASE_URL…`, `keeps same-origin paths when VITE_API_BASE_URL is absent`, `derives the OpenAPI explorer link from the same base URL` |
| `REQ-112` CORS accepts `https://policy.naindev.com` | I | `PreflightFromPolicySubdomainIsAllowed` |
| `REQ-113` client contract matches the API shape | C | `labels a 200 from the configured API as live and passes the response through unchanged` (typed against `PublicDemoRunResponse`; `vue-tsc` enforces the shape) |
| `REQ-101` `REQ-105` `REQ-120` `REQ-121` `REQ-123` | — | Browser walkthrough recorded in `docs/public-demo.md` (2026-10-22) |
| `REQ-122` reduced motion | — | **Gap.** Code path exists (`reducedMotion` prop skips packets); not emulated in a browser yet |

## Known gaps

1. `OPS-006` has no end-to-end test. Closing it requires a fault-injection seam around the
   database connection; that is a design change and is not in scope for spec 001.
2. `DEMO-006` (rate limit) is verified only by manual observation against the live deployment.
3. `POL-010` deliberately has no test: the supported currency list is deployment configuration,
   not a domain rule.
