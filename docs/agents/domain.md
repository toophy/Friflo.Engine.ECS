# Domain Docs

## 探索前读取

- 根目录 `CONTEXT.md`。
- `docs/adr/` 中与当前工作有关的决策记录。

文件不存在时直接继续。
由 domain-modeling 在术语或决策明确后按需创建，
无需提前创建占位文档。

## File structure

本仓库采用 single-context：

- `CONTEXT.md`：领域术语与模型。
- `docs/adr/`：架构决策记录。
- `src/`：源代码。

## 使用领域词汇

工单标题、重构提案、假设和测试名称中的领域概念，
使用 `CONTEXT.md` 定义的术语。

术语缺失时，先检查是否符合项目现有语言；
确有缺口时记录为 domain-modeling 的后续事项。

## ADR 冲突

输出与现有 ADR 冲突时，
明确指出对应 ADR 和重新讨论的理由。
