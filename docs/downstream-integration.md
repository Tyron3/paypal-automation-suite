# Downstream Integration Guide

## How This Project Gates `paypal-transfer-handler` Merges

This automation project (`paypal-automation`) triggers the downstream project's pipeline
after all API tests pass. The downstream project must be configured to **block merges**
until the upstream tests succeed.

---

## Step 1: Upstream trigger (already done ✅)

The `trigger_downstream` job in this repo's `.gitlab-ci.yml` fires a pipeline in
`domains/remittances/payments/paypal-transfer-handler` when all tests pass, passing
these variables:

| Variable                  | Value                        |
|---------------------------|------------------------------|
| `UPSTREAM_PROJECT`        | This project's path          |
| `UPSTREAM_PIPELINE_ID`    | This pipeline's ID           |
| `UPSTREAM_COMMIT_SHA`     | This commit SHA              |
| `UPSTREAM_COMMIT_BRANCH`  | This branch name             |

---

## Step 2: Downstream project configuration

### 2a. Add a gate job in `paypal-transfer-handler/.gitlab-ci.yml`

Add this job to the downstream project's pipeline so that it explicitly appears
as a stage that was triggered by the upstream:

```yaml
# ── Gate job — marks that upstream API tests passed ──
upstream_api_tests:
  stage: test  # or a custom stage like "gate"
  script:
    - echo "✅ Upstream API tests passed"
    - echo "  Project:  $UPSTREAM_PROJECT"
    - echo "  Pipeline: $UPSTREAM_PIPELINE_ID"
    - echo "  Commit:   $UPSTREAM_COMMIT_SHA"
    - echo "  Branch:   $UPSTREAM_COMMIT_BRANCH"
  rules:
    # Only run when triggered by an upstream pipeline
    - if: $UPSTREAM_PIPELINE_ID
```

### 2b. Protect the default branch (required for merge blocking)

In the downstream GitLab project:

1. Go to **Settings → Merge requests**
2. Under **Merge checks**, enable:
   - ✅ **Pipelines must succeed**
3. Go to **Settings → Repository → Protected branches**
4. Ensure the default branch (e.g. `main`) is protected

This means:
- When a developer opens an MR in `paypal-transfer-handler`, they can merge normally
  (their own pipeline must pass).
- When `paypal-automation` triggers the downstream pipeline via `trigger_downstream`,
  if the API tests failed, the trigger never fires → the downstream pipeline is never
  created → no green pipeline → **merge is blocked**.

### 2c. (Optional) Require the upstream pipeline via CI/CD subscriptions

GitLab Premium/Ultimate supports **pipeline subscriptions**:

1. In `paypal-transfer-handler`, go to **Settings → CI/CD → Pipeline subscriptions**
2. Add: `domains/remittances/payments/paypal-automation` (this project)
3. Now, every time this project's pipeline completes, it automatically triggers
   a pipeline in the downstream project.

This is an alternative to the `trigger_downstream` job and provides tighter coupling.

---

## Flow Diagram

```
paypal-automation                    paypal-transfer-handler
┌──────────────┐                     ┌──────────────────────┐
│  build       │                     │                      │
│  test  ──────┼──── trigger ───────►│  upstream_api_tests  │
│  report      │   (only on pass)    │  build               │
│  downstream  │                     │  test                │
└──────────────┘                     │  deploy              │
                                     └──────────────────────┘
                                              │
                                     MR merge blocked if
                                     pipeline not green ✅
```

---

## Summary

| Responsibility          | Where                        |
|-------------------------|------------------------------|
| Run API tests           | `paypal-automation` (this repo) |
| Trigger downstream      | `trigger_downstream` job     |
| Block merge on failure  | Downstream GitLab settings: "Pipelines must succeed" |

