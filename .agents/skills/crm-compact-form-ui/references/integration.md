# Repository integration (optional but recommended)

Place this skill at:

`.agents/skills/crm-compact-form-ui/SKILL.md`

Preserve existing `.agents/skills` folders; this skill is additional.

Codex may select it from its `description` automatically. To make its use an explicit project rule, **append** this small clause to the current root `AGENTS.md` after inspecting its current content (never replace the file):

```md
### Compact forms
For every new or modified form, modal form, filter form, input, select, textarea, button, or form layout, follow `.agents/skills/crm-compact-form-ui/SKILL.md` and its relevant references. Apply compact form density by default, even when the request does not explicitly mention spacing or sizing. Also respect existing responsive and typography skills. Do not change unrelated screens, application behavior, or shared global CSS.
```

When existing AGENTS instructions already state the same rule, update the existing clause instead of duplicating it.

Existing forms should **not** all be rewritten on installation. Apply the rule when a form is created, redesigned, or edited; perform a wider audit only if explicitly requested.
