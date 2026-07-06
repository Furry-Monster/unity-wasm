# ABI 版本策略

## 格式

`tool.json` 中的 `abi` 字段：

```
editor-api/{major}
```

当前支持：**`editor-api/1`**

## 规则

| 变更类型 | 是否 breaking | 处理方式 |
|----------|---------------|----------|
| 新增 import（additive） | 否 | 同 major，更新 manifest + 文档 |
| 修改已有 import 签名 | 是 | 新 major `editor-api/2` |
| 删除 import | 是 | 新 major |

## 加载行为（M2+）

- `abi` 缺失或不等于 Host 支持的版本 → **拒绝加载**，Console 输出 [`AbiVersion.GetErrorMessage`](../packages/com.fumo.editor-wasm/Editor/AbiVersion.cs)
- breaking 实验在 `feature/editor-api-2` 分支进行

## 与 package 版本

`com.fumo.editor-wasm` 的 UPM 版本（0.x）独立于 ABI major。
