Daily Developer Workflow

```
GitHub Issue created (templated, labeled)
      ↓
Developer: git checkout dev && git pull
      ↓
Developer creates feature branch: feature/issue-123-user-registration
      ↓
Develop locally, run tests locally (dotnet test / npm test / flutter test)
      ↓
Commit using Conventional Commits (Phase 14)
      ↓
Push feature branch
      ↓
Open PR → dev (template auto-filled, "Closes #123")
      ↓
GitHub Actions (pr-ci.yml) runs path-relevant jobs automatically
      ↓
CODEOWNERS auto-requests relevant reviewers
      ↓
3 team members review and approve (or request changes)
      ↓
   IF checks fail → PR blocked, developer pushes fixes, CI re-runs automatically
   IF changes requested → developer addresses, pushes, stale approvals dismissed, re-review needed
      ↓
Once ci-gate passes + 3 approvals + conversations resolved →
      ↓
Maintainer (you) reviews final state and clicks Merge (squash merge into dev)
      ↓
dev updated → dev-integration.yml runs full-stack check automatically
      ↓
Issue auto-closes (via "Closes #123")
```