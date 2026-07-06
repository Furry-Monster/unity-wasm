# M2 架构决策（ADR）

> 日期：2026-07  
> 状态：已定案

## 1. FFI-first 单一真理

**决策**：[`schemas/host-imports.v1.json`](../schemas/host-imports.v1.json) 为 Host import 的 enforce 源；WIT 与之对齐，作为语义/AI 文档。

**理由**：M1 实际运行的是 `EditorHostBridge` 的 lowered FFI（ptr/len），而非 WIT 中的高层 `list<string>` API。

## 2. Guest 目标：core wasm

**决策**：继续 **wasm32-unknown-unknown** + cdylib + `Linker.DefineFunction`；**不**在 M2 迁移 Wasm Component Model。

**理由**：与 Wasmtime .NET 16.x 当前集成方式一致；Component Model 留 M3 末再评估。

## 3. Rust 绑定：manifest codegen

**决策**：wit-bindgen Spike 结论 — 对 core wasm 使用 **[`scripts/gen-rust-imports.py`](../scripts/gen-rust-imports.py)** 从 manifest 生成 `imports.rs`（等价于 Plan 回退方案）。

**理由**：manifest → Rust extern 与 Host 100% 对齐；不引入 component 工具链复杂度。

## 4. 契约验证：本地脚本

**决策**：[`scripts/verify-contracts.sh`](../scripts/verify-contracts.sh) 静态 diff wasm imports vs manifest；**不做 GitHub Actions**（延续 M1 ADR）。

## 5. ABI fail-fast

**决策**：`tool.json.abi` 必须等于 `editor-api/1`；不匹配拒绝加载。

## 6. Tier 3 扩展

**决策**：除 Plan 草案的 `get_object_path` / `get_serialized_property` 外，增加 `get_component_count` / `get_component_type_at` 以支撑 `prefab-inspector-lite` 示例（只读、浅层）。
