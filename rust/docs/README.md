# docs —— Rust 工具链各工具说明

这里按「**一个主题一篇**」的方式记录 Rust 生态：工具类按「一个工具一篇」，讲它是干什么的、常用命令、
以及在本机**实测过**的版本与行为；语言机制类单独成篇（见下方「语言机制文档」）。
目的是把查过一遍的东西沉淀下来，下次不用重新查。

## 工具链地图

```text
rustup ── 工具链管理器（管 toolchain / component / target，本身不是编译器）
  └── cargo ── 构建与包管理入口（绝大多数操作都从这里进）
        ├── rustc ───── 真正的编译器（cargo 只是替你拼 rustc 命令行）
        │     ├── rustdoc ── 从文档注释生成 HTML（cargo doc）
        │     └── lint 控制 ─ rustc 自带一批 lint，用 -W / -D / -A / -F 调级别
        ├── clippy ──── 额外的 lint 集合（cargo clippy）
        ├── rustfmt ─── 代码排版（cargo fmt）
        └── miri ────── 解释执行、检测未定义行为（仅 nightly 提供）
rust-analyzer ── 编辑器里的语言服务器（LSP）：补全 / 跳转 / 类型提示 / 内联诊断
```

一句话记法：**rustup 装东西，cargo 干活，rustc 编译，clippy 挑刺，rustfmt 排版，rust-analyzer 伺候编辑器。**

## 本机环境（实测）

| 工具 | 版本 | 备注 |
| --- | --- | --- |
| rustup | 1.29.0 | home：`C:\Users\treertzhu\.rustup` |
| 默认工具链 | stable-x86_64-pc-windows-msvc | profile 默认 |
| rustc | 1.96.0 (ac68faa20 2026-05-25) | |
| cargo | 1.96.0 (30a34c682 2026-05-25) | |
| rustfmt | 1.9.0-stable | 版本号与 rustc **不同步**，正常 |
| clippy | 0.1.96 | 版本号与 rustc **不同步**，正常 |
| rust-analyzer | 已安装组件 | 编辑器侧另有同名扩展 |

已安装组件：`cargo`、`clippy`、`rust-analyzer`、`rust-docs`、`rust-src`、`rust-std`（含 `aarch64-apple-darwin`）、`rustc`、`rustfmt`
未安装：`miri`（stable 不提供该组件，需要 nightly）

## 工具文档索引

| 文档 | 主题 |
| --- | --- |
| [rustup.md](./rustup.md) | 安装 / 切换 / 管理工具链与组件 |
| [cargo.md](./cargo.md) | 构建、运行、测试、包管理 |
| [rustc.md](./rustc.md) | 编译器本体与 lint 级别控制 |
| [clippy.md](./clippy.md) | 静态检查（额外 lint 集合） |
| [rustfmt.md](./rustfmt.md) | 代码格式化 |
| [rust-analyzer.md](./rust-analyzer.md) | IDE / LSP 支持 |

计划补充：`rustdoc`（文档生成）、`miri`（UB 检测）、第三方 cargo 子命令（`cargo-expand` 等）。

## 语言机制文档

不是工具，而是「某个语言机制到底怎么工作」的沉淀；每篇都配一个可运行的 `examples/*.rs`
（`macro.md` 的过程宏部分例外 —— 它必须在独立 crate 里定义，见该文 §4.1，配方已实测）。

| 文档 | 配套示例 | 主题 |
| --- | --- | --- |
| [lifetime.md](./lifetime.md) | [`examples/lifetime.rs`](../examples/lifetime.rs) | 省略规则、`'_`、`'static`、常见借用报错速查 |
| [macro.md](./macro.md) | [`examples/func_macro.rs`](../examples/func_macro.rs)、[`examples/hygiene.rs`](../examples/hygiene.rs) | 声明宏的 token 树匹配 / 重复 / 递归上限、卫生性与 `$crate`、过程宏最小配方 |

## 相关

- 目录用途与代码约定：见上一级 [`AGENTS.md`](../AGENTS.md)。
