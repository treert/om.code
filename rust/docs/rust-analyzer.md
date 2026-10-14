# rust-analyzer —— 编辑器里的语言服务器

## 定位

`rust-analyzer`（简称 RA）是 Rust 的**语言服务器（LSP 实现）**，负责编辑器里的体验：
补全、跳转定义、查找引用、重命名、内联类型提示、悬停文档、内联报错。

它**不是**编译器：不生成机器码，也不替代 `cargo build`。它的价值是「**在你敲代码的瞬间给反馈**」。

- 通过 rustup 组件分发（本机已安装：`rust-analyzer-x86_64-pc-windows-msvc`）；
- 编辑器侧还需要装对应扩展（VS Code 是 `rust-lang.rust-analyzer`），两者配合工作；
- 本机的 `rust-src` 组件已装 —— 这是「跳进标准库源码看实现」的前提。

## 它和 cargo / rustc 的关系

| 功能 | 谁提供 |
| --- | --- |
| 项目级错误/警告（红线） | **默认来自 `cargo check`**（RA 后台调用），不是 RA 自己算的 |
| 补全、跳转、类型提示、重构 | RA 自己的按需分析（快，但不追求程序级完备） |
| 生成二进制、跑测试 | `cargo build` / `cargo test` |
| 最终权威结论 | 永远以命令行 `cargo check` / `cargo clippy` 的输出为准 |

> 所以：**编辑器没报错 ≠ 编译能过**，反过来编辑器偶尔也有滞后的假警告（改完 `Cargo.toml` 后
> 常见），此时点一下 "Restart server" 或跑一遍 `cargo check` 对齐认知。

## 对本仓库特别有用的一步配置

默认情况下 RA 只检查「默认 target」（bin/lib），`examples/*.rs` 可能**不参与检查**。
本目录全是 example，建议打开它：

```jsonc
// .vscode/settings.json（本目录目前没有这个文件，可按需自建）
{
  "rust-analyzer.cargo.allTargets": true,      // 让 examples / tests / benches 都进检查范围
  "rust-analyzer.check.command": "clippy",     // 用 clippy 的诊断替代 cargo check，红线更严格
  "rust-analyzer.cargo.features": "all",
  "rust-analyzer.inlayHints.typeHints.enable": true
}
```

本仓库**目前没有** `.vscode/` 目录，所以上述设置尚未生效（除非你在用户级设置里配过）。

## 常用操作

| 场景 | 操作 |
| --- | --- |
| 看某变量被推断成什么类型 | 悬停，或开 inlay hints（内联类型提示） |
| 跳进标准库/依赖源码 | Ctrl+Click（需要 `rust-src` 组件） |
| 看某函数/类型所有引用 | Shift+F12 / Find All References |
| 重命名（含跨文件） | F2（RA 的 rename，比纯文本替换可靠） |
| 跑/调试当前 example | 代码上方的 Run / Debug lens（调试需装 CodeLLDB） |
| 手写 `cargo` 命令 | 命令面板 → "rust-analyzer: Run" 或直接用终端 |

## 与其它工具的边界

- **诊断冲突时以 rustc/clippy 为准**：RA 的检查是增量的、按需的，边界场景（复杂的 trait 求解、
  宏展开）结论可能与真实编译不同。
- RA 不负责格式化，`cargo fmt` 才是；也不负责 lint 规则本身，规则来自 rustc/clippy。
- 大项目首次打开会慢（要跑一遍 `cargo check` 建索引），之后才是增量的爽感。
