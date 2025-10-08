How to use (quick)

Plan→Clean: run flow plan_clean with a task description like
“Tree-shake Angular app; remove dead code; keep risk low.”

Branch Reconciler: run flow branch_reconciler with inputs
repo=<path>, base=main, head=feature/my-branch.

This pack will:

Create all helper files under agent-os/.

Route models by mode (Opus: planning/troubleshooting/reconciling; Sonnet: implementing/documenting/cleaning/free-balling).

Save planning artifacts under docs/tech-choices/…

Save branch comparison reports under docs/reports/…

If you want me to tweak file paths (e.g., your exact solution folder names or Angular workspace), say the word and I’ll update the YAML to match your repo layout.