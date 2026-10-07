# AGENTS.md

**必须：** 代码不写解释性注释。仅允许：程序依赖的注释（如脚手架锚点、生成代码标记、编译器或工具指令）；代码无法表达的约束，限一行，只写约束本身。

# graphify

- **graphify** (`~/.Codex/skills/graphify/SKILL.md`) - any input to knowledge graph. Trigger: `/graphify`

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

## Agent skills

### Issue tracker

任务和规格使用本地 Markdown，保存在 `.scratch/<feature>/`。见 `docs/agents/issue-tracker.md`。

### Triage labels

使用五个默认分诊标签。见 `docs/agents/triage-labels.md`。

### Domain docs

使用单一 `CONTEXT.md` 和根目录 `docs/adr/`。见 `docs/agents/domain.md`。

### Project research

首次理解项目、分析 `misakaruntime` 或判断上游同步前，读取 `docs/project-understanding.md`，并核对当前 Git 状态与记录中的提交基线。
