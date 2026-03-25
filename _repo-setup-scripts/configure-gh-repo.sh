#!/usr/bin/env bash
set -euo pipefail

########################################
# Config – toggle features here
########################################

ENABLE_AUTO_MERGE=true
DELETE_BRANCH_ON_MERGE=true

REQUIRE_PR_REVIEWS=true
REQUIRED_APPROVING_REVIEWS=1
REQUIRE_CODEOWNER_REVIEWS=false
DISMISS_STALE_REVIEWS=true
REQUIRE_LAST_PUSH_APPROVAL=false

REQUIRE_STATUS_CHECKS=true
STATUS_CHECKS_STRICT=true
STATUS_CHECK_CONTEXTS=()  # e.g. ("ci/test" "ci/build")

ENFORCE_ADMINS=true
REQUIRE_LINEAR_HISTORY=true
REQUIRE_CONVERSATION_RESOLUTION=true
REQUIRE_SIGNED_COMMITS=true

########################################
# Helpers
########################################

log()   { printf '[INFO ] %s\n' "$*" >&2; }
warn()  { printf '[WARN ] %s\n' "$*" >&2; }
error() { printf '[ERROR] %s\n' "$*" >&2; }

DRY_RUN=false
ERRORS=()

record_error() {
  ERRORS+=("$1")
  error "$1"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || {
    error "Required command '$1' not found"
    exit 1
  }
}

resolve_repo() {
  if [[ $# -ge 1 ]]; then
    echo "$1"
    return
  fi

  if gh_repo=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null); then
    echo "$gh_repo"
    return
  fi

  error "Could not determine repository. Pass owner/repo or run inside a repo."
  exit 1
}

########################################
# Core request executor (real or dry-run)
########################################

send_request() {
  local method=$1
  local endpoint=$2
  local body=$3

  if $DRY_RUN; then
    log "DRY RUN: $method $endpoint"
    printf "%s\n\n" "$body"
    return 0
  fi

  if ! gh api -X "$method" "$endpoint" \
      -H "Accept: application/vnd.github+json" \
      --input - <<<"$body"; then
    return 1
  fi
}

########################################
# Payload builders
########################################

build_repo_settings_payload() {
  cat <<EOF
{
  "allow_auto_merge": $ENABLE_AUTO_MERGE,
  "delete_branch_on_merge": $DELETE_BRANCH_ON_MERGE
}
EOF
}

build_branch_protection_payload() {
  local required_status_checks_json

  if $REQUIRE_STATUS_CHECKS && ((${#STATUS_CHECK_CONTEXTS[@]} > 0)); then
    local contexts_json
    contexts_json="[\"$(printf '%s","' "${STATUS_CHECK_CONTEXTS[@]}" | sed 's/,"$//')\"]"

    required_status_checks_json=$(cat <<JSON
{
  "strict": $STATUS_CHECKS_STRICT,
  "contexts": $contexts_json
}
JSON
)
  else
    if $REQUIRE_STATUS_CHECKS && ((${#STATUS_CHECK_CONTEXTS[@]} == 0)); then
      warn "REQUIRE_STATUS_CHECKS=true but STATUS_CHECK_CONTEXTS is empty – disabling status checks in payload to avoid invalid combination."
    fi
    required_status_checks_json="null"
  fi

  cat <<EOF
{
  "required_status_checks": $required_status_checks_json,
  "enforce_admins": $ENFORCE_ADMINS,
  "required_pull_request_reviews": {
    "required_approving_review_count": $REQUIRED_APPROVING_REVIEWS,
    "require_code_owner_reviews": $REQUIRE_CODEOWNER_REVIEWS,
    "dismiss_stale_reviews": $DISMISS_STALE_REVIEWS,
    "require_last_push_approval": $REQUIRE_LAST_PUSH_APPROVAL
  },
  "restrictions": null,
  "required_linear_history": $REQUIRE_LINEAR_HISTORY,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": $REQUIRE_CONVERSATION_RESOLUTION
}
EOF
}

########################################
# Operations
########################################

apply_repo_settings() {
  local repo=$1
  local payload
  payload=$(build_repo_settings_payload)

  log "Applying repo settings to $repo"

  if ! send_request PATCH "repos/$repo" "$payload"; then
    record_error "Failed to update repo settings for $repo"
  fi
}

apply_branch_protection() {
  local repo=$1
  local branch="main"
  local payload
  payload=$(build_branch_protection_payload)

  log "Applying branch protection to $repo ($branch)"

  if ! send_request PUT "repos/$repo/branches/$branch/protection" "$payload"; then
    record_error "Failed to update branch protection for $repo ($branch)"
  fi
}

apply_signed_commits() {
  local repo=$1
  local branch="main"

  if ! $REQUIRE_SIGNED_COMMITS; then
    log "Signed commits disabled — skipping"
    return
  fi

  log "Enabling required signed commits for $repo ($branch)"

  if ! send_request POST "repos/$repo/branches/$branch/protection/required_signatures" "{}"; then
    record_error "Failed to enable required signed commits for $repo ($branch)"
  fi
}

########################################
# Main
########################################

main() {
  require_cmd gh

  if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=true
    shift
    log "Dry-run mode enabled — no changes will be applied"
  fi

  local repo
  repo=$(resolve_repo "$@")

  log "Using repository: $repo"

  apply_repo_settings "$repo"
  apply_branch_protection "$repo"
  apply_signed_commits "$repo"

  if ((${#ERRORS[@]} > 0)); then
    warn ""
    warn "Completed with errors:"
    for e in "${ERRORS[@]}"; do warn " - $e"; done
    exit 1
  fi

  log "All operations completed successfully"
}

main "$@"
