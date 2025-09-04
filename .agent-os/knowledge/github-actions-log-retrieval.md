# GitHub Actions Log Retrieval Method

## Problem
Direct WebFetch of GitHub Actions job URLs often fails because:
- The page loads dynamically with JavaScript
- Logs are loaded asynchronously 
- The initial HTML doesn't contain the actual test output

## Successful Method

### 1. Use GitHub CLI (gh) - MOST RELIABLE
```bash
# Get run details
gh run view <RUN_ID> --repo <owner>/<repo>

# Get job logs
gh run view <RUN_ID> --log --repo <owner>/<repo>

# Get specific job logs
gh run view <RUN_ID> --job <JOB_ID> --log

# For our repo specifically:
gh run view 17351644226 --log --repo chadmcghie/Crud
```

### 2. Use Raw Log URLs
Instead of the web UI URL like:
```
https://github.com/chadmcghie/Crud/actions/runs/17351644226/job/49258762742
```

Try to construct the raw log URL:
```
https://api.github.com/repos/chadmcghie/Crud/actions/runs/17351644226/logs
```

### 3. If WebFetch Must Be Used
When using WebFetch, ask specifically for:
- Error messages and stack traces
- Test failure summaries
- Step outcomes (passed/failed)
- Specific error text patterns

Don't ask for "the full logs" as they won't be in the initial page load.

## Example Commands

```bash
# View latest run for a branch
gh run list --branch test-server-optimization --repo chadmcghie/Crud --limit 1

# View detailed logs for specific job
gh run view --repo chadmcghie/Crud --job 49258762742 --log

# Download logs as artifact
gh run download <RUN_ID> --repo chadmcghie/Crud
```

## Why This Works
- `gh` CLI authenticates and accesses the API directly
- Gets raw log data, not the web UI
- Bypasses JavaScript rendering issues
- Can filter and search logs programmatically

## Fallback Options
If `gh` is not available:
1. Ask user to check the logs and report specific errors
2. Use curl with GitHub API token to access logs endpoint
3. Check artifact downloads if logs were uploaded

## Memory Note
**Always try `gh run view` FIRST before attempting WebFetch on GitHub Actions URLs**