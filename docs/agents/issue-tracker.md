# Issue tracker: Local Markdown

任务和规格保存在本仓库的 `.scratch/` 下。

## Conventions

- 每个功能一个目录：`.scratch/<feature-slug>/`。
- 规格：`.scratch/<feature-slug>/spec.md`。
- 每张实施工单独立一个文件：
  `.scratch/<feature-slug>/issues/<NN>-<slug>.md`。
- 工单从 `01` 开始编号，不合并成单个工单汇总文件。
- 分诊状态写在工单顶部附近的 `Status:` 行；
  状态名称见 `triage-labels.md`。
- 评论和讨论追加到文件末尾的 `## Comments` 下。

## 发布到任务跟踪器

在 `.scratch/<feature-slug>/` 下创建对应文件，
必要时创建目录。

## 获取相关工单

读取用户提供的路径或编号所对应的工单文件。

## Wayfinding operations

- Map：`.scratch/<effort>/map.md`，
  保存 Notes、Decisions-so-far 和 Fog。
- 子工单：`.scratch/<effort>/issues/NN-<slug>.md`；
  `Type:` 使用 research、prototype、grilling 或 task；
  领取和解决分别记录为 `Status: claimed`、`Status: resolved`。
- 依赖：顶部附近记录 `Blocked by: NN, NN`。
  所有依赖工单均为 resolved 后解除阻塞。
- Frontier：按编号查找开放、无阻塞且未领取的第一张工单。
- Claim：开始工作前先保存 `Status: claimed`。
- Resolve：在 `## Answer` 下追加结果，保存 `Status: resolved`，
  并在 map.md 的 Decisions-so-far 中追加摘要与工单链接。
