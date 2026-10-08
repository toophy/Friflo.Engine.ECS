# 整合分支的组件布局与兼容接口

本约定随 `codex/misakaruntime-on-main-clean` 整合提供，决策依据见 [ADR](adr/0001-explicit-aligned-aos.md)，验证结果与覆盖边界见[项目基线](project-understanding.md)。原始结果与临时夹具保留在本地 `.scratch/`，不纳入 Git。

## 布局选择

| 选择 | 存储与访问 |
| --- | --- |
| 不加布局属性 | main 的普通 AoS；`Chunk<T>.ArchetypeComponents` 是可写的真实 `T[]`，`Entity.GetComponent<T>()` 保留 ref 语义 |
| `[AoSAligned]` | 显式对齐 AoS；通过 `GetComponentSpan()` 取得包含实际偏移的组件 span，实体 ref 访问仍可用 |
| `[SoA]` | 按 lane 存储；chunk 用 `GetSoA()` / `SetSoA()`，实体用 `GetSoA<T>()` 读值、`Set()` 写值 |
| `[AoSoA]` | 按 tile 存储；chunk 用 `GetAoSoA()` / `SetAoSoA()`，实体用 `GetSoA<T>()` 读值、`Set()` 写值 |

显式对齐 AoS 的专用形状支持 float、Vector2、Vector4、Quaternion。SoA/AoSoA 保留来源的 Vector2/3/4 形状规则；不要仅凭组件是 struct 就推断它适用于这些布局。

不受支持的显式布局在识别时拒绝，错误包含组件名和请求布局。由泛型静态初始化触发时，外层为 TypeInitializationException，GetBaseException() 返回具体 ArgumentException；没有标注布局的组件仍使用普通 AoS。五个非法声明的正式回归见[布局校验测试](../src/Tests/ECS/SoA/Test_ExplicitLayoutValidation.cs)。

所有显式布局的 `ArchetypeComponents` 均为 null。SoA/AoSoA 不存在可写连续 `T[]` 或稳定 `ref T`，普通组件 span/ref 接口会拒绝。普通 AoS 的 `AsSpan128/256/512`、`StepSpan128/256/512` 和 `ChunkExtensions` 向量转换保留 main 语义；不要把旧数组和 SIMD span 接口用于显式布局。

筛选后的 SoA chunk 会正确应用起始偏移。AoSoA 的逐组件值接口适用于任意筛选偏移；raw lane 接口要求切片起点落在 tile 边界，否则抛出异常。原始 lane/组件 span 写入与 main 的 ref 写入相同，不会自动维护索引；需要维护组件索引时使用 Entity/Batch 的更新入口。

## 宽类型编号

事件的旧 `byte TypeIndex` 字段继续表示低 8 位；需要完整编号的调用方使用 `FullTypeIndex`。EventRecorder 的筛选和格式化已使用完整编号。`FullTypeIndex` 的可表示范围是 0–65535，实际 schema 还包含引擎保留类型；这个表示范围不等于已经运行了 65536 个用户组件。

公共 BitSet 已恢复 main 的 256 位、32 字节 unmanaged 布局；外部 stackalloc、unmanaged 约束和 MemoryMarshal.Cast 实际编译/运行通过。扩容实现迁移到独立 WideBitSet，Tags 的旧 `Tags(in BitSet)` 构造继续可用。

用户确认 Tags/ComponentTypes 保留高类型容量，因此这两个结构和 WideBitSet 在 x64 上仍为 40 字节，包含托管 overflow 引用；Chunk<T> 仍从 main 的 16 字节变为 32 字节。不能假定这些结构满足旧的 unmanaged、stackalloc、原生互操作或固定二进制布局约束。核心公开符号无缺失；普通托管调用样例及 main 编译程序替换 ECS DLL 均通过，适用范围详见[项目基线](project-understanding.md)。

## NativeAOT

通过 `NativeAOT.RegisterComponent`、相应 indexed、relation 或 link relation 注册入口注册类型后再创建 schema。注册入口保留公开字段元数据，使显式布局识别在裁剪后仍然成立。字段计数的 IL2090 在该契约下局部处理，最小 native 夹具无此诊断；八种布局、SoA indexed 生命周期与五个非法声明共 21/21 通过。构建仍报告通用反射分析和 Fliox 依赖的 trim/AOT 警告，后者在固定官方 main 上也能复现。不能由这些用例推断任意字段形状、未注册动态反射路径或其他目标平台均已验证。
