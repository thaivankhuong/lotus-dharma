# LimitTokens Rule (Ultra-Optimized + Task Access Enabled)

## Purpose
Minimize token usage and restrict Cursor operations to backend code inside /src.
Prevent automatic workspace scanning while still allowing user-invoked task files.

---

## Scope Control

### Allowed:
- Only read or modify backend files inside:
  - /src/**

### Block automatic scanning of:
- /docs
- /tasks      # but user-invoked @task files are allowed
- /infra
- /templates
- /.github
- /.devcontainer
- /.azdo
- root-level config files (unless user explicitly names them)

### Exception (important):
If the user references a file directly using:
- `@ld-0001`
- `@ld-0002`
- `@taskname`

→ Cursor MUST open that exact file, even if located inside /tasks.

---

## Call Behavior
- Only modify files explicitly named by the user.
- Do not auto-scan entire workspace.
- Do not auto-refactor or auto-fix unrelated files.
- Do not rewrite large files unless requested.
- Stop after the first valid output.
- Do not perform iterative improvements unless user says "continue".


---

## Output Rules
- Default: short and concise answers.
- Only provide long explanations when user says "CHI TIẾT".
- Do not produce large diffs unless user asks for full file output.

---

## Safety
- Never modify EF Core migrations unless user explicitly requests.
- Never change API contracts or domain models without user confirmation.
- Preserve Clean Architecture boundaries at all times.

---

# End of LimitTokens.md
