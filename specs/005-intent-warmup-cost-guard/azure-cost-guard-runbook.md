# Runbook — Azure cost guard (owner steps, `REQ-147`)

- Date: 2026-10-26
- Executed by the owner only. The agent read the current state and changed nothing in Azure.

## Verified state (read-only `az`, 2026-10-26)

| Item | Value |
|---|---|
| Subscription offer | `PayAsYouGo`, spending limit off (cannot be enabled on this offer) |
| Database `PolicyOperationsDemo` | `GP_S_Gen5`, free offer on, `freeLimitExhaustionBehavior=AutoPause`, `autoPauseDelay=60`, `minCapacity=0.5` |
| Web app plan | `asp-nain-policy-demo-frc-f1`, `F1` |
| Budgets | None |
| Alerts | `alert-sql-free-99-percent` enabled; no delivery ever observed (ADR-005) |

Why it matters: the free offer gives 100,000 vCore-seconds per month. With a 60 min delay each wake
is estimated at about 1,800 vCore-seconds (about 55 wakes a month); with 15 min, about 450 (about
220 wakes). These figures are estimates, not measurements.

## Outcome, 2026-10-26

- Step 1 was attempted with the owner's authorization and **rejected by Azure**:
  `ProvisioningDisabled: Only default value for auto pause delay is allowed for Free Limit database
  with auto pause exhaustion behavior`. The delay remains 60 min; nothing changed.
- Step 2 was done with the owner's authorization through the Consumption budgets API
  (`budget-nain-policy-1eur`), with the Azure account e-mail as recipient, and read back with
  current spend 0.0.

## Step 1 — Lower the auto-pause delay `[H]` (not possible on this offer)

```bash
az sql db update -g rg-nain-policy-demo -s sql-nain-policy-demo -n PolicyOperationsDemo --auto-pause-delay 15
```

Verify:

```bash
az sql db show -g rg-nain-policy-demo -s sql-nain-policy-demo -n PolicyOperationsDemo --query "{delay:autoPauseDelay,free:useFreeLimit,onExhaust:freeLimitExhaustionBehavior}" -o json
```

Expected: `delay` 15, `free` true, `onExhaust` `AutoPause`.

If Azure rejects 15 (the free offer may restrict it), keep 60 and do not try other values; tell the
agent so the estimate in spec 005 is corrected.

Rollback: the same command with `--auto-pause-delay 60`.

## Step 2 — Create a 1 EUR monthly budget `[H]`

Portal, because the CLI preview command cannot set e-mail notifications reliably:

1. Azure portal, Cost Management, Budgets, Add.
2. Scope: subscription `Azure subscription 1`.
3. Name `budget-nain-policy-1eur`, reset period monthly, amount 1 (in the billing currency).
4. Alerts: actual cost at 50 % and 100 %, plus forecasted at 100 %; recipient your e-mail.
5. Create.

A budget only notifies; it does not stop spending. The real stop for this demo is the free-offer
`AutoPause` and the F1 plan, which cannot bill.

## Step 3 — Check the quota now and then `[H]`

Portal, the database, Overview, "Free amount remaining". If it falls below 20 % early in the month,
the demo is receiving more traffic than the free offer sustains; it then shows `Simulated`, which is
safe and free.

## What stays unverified

- Whether the quota alert e-mail is delivered.
- The real vCore-seconds consumed per wake.
