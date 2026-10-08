# Friflo 整合分支理解与基线

本工作树用于用户已确认的 misakaruntime 整合与严格验证。继续分析前应检查 `git status`、当前 HEAD 与本文的固定比较点。

- 官方 main：`717caad3ae2b9714d8e35cda08f3f5796204cd7e`，于 2026-10-08 重新 fetch 核对。
- misakaruntime：`0b4ffa14fca7bd7c6bb262fc67a4173d7c3fece9`。
- 合并提交：`20401eacd7dc993cc07034909cb8c792123d6e5f`，两个父提交分别为上述 main 与来源分支。
- 当前交付分支：`codex/misakaruntime-on-main-clean`。以原合并提交为起点，保留原整合分支 `codex/misakaruntime-on-main` 的最终源码与正式回归；原分支 `0b9527a9` 仅作为本地备份。
- 最终源码与原验证树一致：src tree `8424549238be618b8623993221a74c977ac7ca0e`，ECS tree `d5ee9335bd4818bd263496a2cae0587301777cd8`。清理只改变交付文件范围、忽略规则和文档链接，没有重新执行测试。
- 2026-10-08 完整两轴审查范围 `717caad3...162f0db6`：Standards 0，Spec 1 项 P2（不支持的显式布局未明确拒绝）。`aa3c84e9` 已修复该问题，五个负例补充正式回归；增量复审两轴均为 0。
- 最终同内容工作树：Release/Debug 各 968 通过、0 失败、1 项原有忽略；Unity 2022.3.57f1c2 定向 EditMode 38/38、自然退出 0；NativeAOT win-x64 21/21。核心五个声明框架与解决方案 Release 均构建通过，仍有既有反射、trim/AOT 等警告。

任务工单、原始日志、测试结果、证据压缩包和临时夹具保留在本地 `.scratch/`，不纳入 Git。可复现的正式回归保存在 `src/Tests`、`src/Tests-internal` 和 `src/Tests-NativeAOT`；仓库文档只记录必要行为约定、结果摘要与验证边界。

核心在 `src/ECS`，Boost 在 `src/ECS.Boost`，公开行为测试在 `src/Tests`，内部表示和生命周期测试在 `src/Tests-internal`。核心声明 net10.0、net8.0、net7.0、net6.0 和 netstandard2.1。

来源包含完整 SoA 开发历史，以及 `9a3426ed` 的类型扩容和 `0b4ffa14` 的稀疏映射、引用清理、跨 buffer ID 修复。容量集合使用 WideBitSet 的 inline 256 位和 overflow blocks，公共 BitSet utility 恢复 main 的 32 字节 unmanaged 布局；用户接受 Tags/ComponentTypes 的托管扩容数据。实体索引/关系所有权仍有 inline 32 位与稀疏 overflow；heap 与 batch slot 为低编号直接查找、高编号有序查找。

用户已确认未标注组件保留 main 的普通 AoS 语义，对齐 AoS 通过 `[AoSAligned]` 显式启用；SoA/AoSoA 专用访问保留。见[布局决策](adr/0001-explicit-aligned-aos.md)。宽编号事件通过 `FullTypeIndex` 读取，旧 `byte TypeIndex` 字段保留其低 8 位语义。

原工作树 `C:/MySpace/Projects/CSharp/Friflo.Engine.ECS.2` 的研究文档和未提交工件完整保留。其历史性能数据与当前修复后的实测不可混用；本工作树的报告明确记录重新执行和未执行项。

高容量、外部兼容调用和性能验证在 `ec49d37c` 的诊断修复阶段完成：1024 用户组件与 1024 用户标签、256 组 indexed/link/relation、48 个同步 buffer 的 24576 个唯一实体，以及四类托管引用释放均通过；main 的 2692 项公开 API 无缺失，外部样例和 main 编译程序替换 ECS DLL 均通过。末次布局校验修复没有重跑这些独立探针。

最后同组微基准的 Get2 比 main 慢 29.0%，BatchSet 慢 18.7%，chunk 查询样本范围重叠；该性能数据属于末次布局校验修复前的源码阶段。未完成 colony-sim 实际游戏负载、完整 Unity suite、Player/IL2CPP、其他架构平台或全部固定 native ABI 验证。
