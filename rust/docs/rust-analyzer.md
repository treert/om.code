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
// .vscode/settings.json（建议的常规设置；该文件被 .gitignore 忽略，只影响本机）
{
  "rust-analyzer.cargo.allTargets": true,      // 让 examples / tests / benches 都进检查范围
  "rust-analyzer.check.command": "clippy",     // 用 clippy 的诊断替代 cargo check，红线更严格
  "rust-analyzer.cargo.features": "all",
  "rust-analyzer.inlayHints.typeHints.enable": true
}
```

> 2026-10-15 更新：本目录现在**有** `.vscode/settings.json` 了（被仓库根 `.gitignore` 的 `.vscode` 规则忽略，
> 所以只影响本机、不会进版本库）；不过里面目前只有一行绕 RA bug 的设置，见下节「已知坑」。
> 上面那几条 `allTargets` / `check.command` 仍然值得加上 —— 本目录几乎全是 example，
> 不开 `allTargets` 的话 `examples/*.rs` 根本不进检查范围。

## 已知坑：过程宏生成的 token 用「哑 span」会把 RA 搞崩（实测）

### 症状

打开 `proc_macro_lab/use_pm/src/main.rs` 时，RA 会抛出一条 panic（编辑器弹窗 / 输出面板）：

```text
thread 'Worker3' panicked at line-index-0.1.2\src\lib.rs:111:35:
invalid offset
   4: <line_index::LineIndex>::line_col
   ...
version: 0.3.3081-standalone (2ad81e72b7 2026-10-11)
request: codeLens/resolve CodeLens { range: 30:11 ~ 30:17, ... }
[Error] Request codeLens/resolve failed. Message: request handler panicked: invalid offset
```

同一处还可能换个请求炸 —— 实测同一天先后撞到三种：

```text
request: codeLens/resolve        → range 落在 pub struct Config 的名字上（kind = impls）
request: textDocument/hover      → position: line 30, character 17
request: textDocument/implementation
```

共同点：`line 30`（0-based）= 文件第 31 行 `pub struct Config`，而且都要**给某个条目算一个位置**。
换句话说，这不是「某个功能坏了」，而是 RA 在 `LineIndex::line_col`（offset → 行列）这一步拿到越界 offset。

### 先排除「陈旧进程」这种假象

本仓库还遇到过另一种长得像的情况：**改完过程宏实现后，RA 仍拿着旧的 proc-macro dylib**，
于是报的是**旧代码的** panic 文案，而磁盘上的 dll 早就是新的了。两者的区分方法：

| 对比项 | 陈旧 dylib | 本节这个 bug |
| --- | --- | --- |
| 进程启动时间 | RA / `rust-analyzer-proc-macro-srv` 早于 dll 的构建时间 | 无规律，可能是刚重启的会话 |
| 磁盘产物 | 源码里已改掉、但报错文案还是旧的 | 与源码无关，改源码不会消失 |
| 处理 | `rust-analyzer: Restart Server`（或结束 `rust-analyzer-proc-macro-srv` 进程） | 见下面「修法」 |

### 判据：A/B 对照（同一进程、同一时刻）

| 探针目标 | 结果 |
| --- | --- |
| `use_pm` 的 `trait Describe`（实现由 `#[derive(Describe)]` 生成） | ❌ `None` + panic `invalid offset` |
| `use_pm` 的 `struct Config` / `struct User` | ❌ `None` + panic |
| `examples/func_macro.rs` 里**手写**的 `impl Display for Sql` | ✅ 正常返回 2 个实现 |
| `examples/lifetime.rs` 里**手写**的 `impl Drop for Noisy` | ✅ 正常返回 1 个实现 |

手写 impl 在同一状态里活得好好的，**只有宏生成出来的条目炸**。

### 根因

过程宏里 `format!(...).parse::<TokenStream>()` 造出来的 token 全都带 `Span::call_site()` 这种「哑」span。
RA 用这些 span 去换算行列时算出了文件长度之外的 offset → `invalid offset`。

`Span::call_site()` 本身是合法的（rustc 能编译、程序能跑，我们前面所有 `cargo run -p use_pm` 都是绿的），
所以这是 **RA 的健壮性 bug**；但「喂给它的是一堆哑 span」这件事**在我们手里**，改掉就没事。

### 修法：`respan`（本仓库已改）

`proc_macro_lab/pm_lab/src/lib.rs` 里的 `respan(ts, span)`：把生成的 token（含括号组内部，**递归**）
统统 `set_span` 成输入里某个**真实 token** 的 span —— 派生宏生成的 impl 挂到结构体名上、
`#[timed]` 的模板挂到函数名上、`calc!` 的表达式挂到第一个 token 上。

实测前后（同一探针、同一状态）：

| `textDocument/implementation` | 改之前 | 改之后 |
| --- | --- | --- |
| `@ trait Describe` | `None` + panic | **2**（`User` / `Config` 两个实现） |
| `@ struct Config` | `None` + panic | **1** |
| `@ struct User` | `None` + panic | **1** |

注意不只是「不崩了」：修之前 RA **根本没能解析出宏生成的 impl**（有的路径静默返回空、有的路径直接崩），
修之后才给出正确结果。顺带生成代码里的报错/跳转位置也终于落回真实源码，而不是整条宏调用。

### 复现与验证

本地诊断脚本 `tmp/lsp_probe.py`（`tmp/` 已在 `.gitignore` 里，不入库；原理是当个极简 LSP 客户端）：

```powershell
# 关键在时序：要**在 workspace 载入/重分析期间**先连发 60 次 hover（复刻 IDE 的行为），
# 之后位置类请求才会稳定炸；静静等它载完再发，反倒可能不炸 —— 这也是这个问题难复现的原因
python tmp/lsp_probe.py "<rust-analyzer.exe 路径>"
python tmp/lsp_probe.py "<rust-analyzer.exe 路径>" ctl   # 急打对象换成无过程宏的 tmp/ctl.rs

# 顺带确认「不是我们的宏本身挂了」：
rust-analyzer analysis-stats proc_macro_lab/use_pm --run-all-ide-things   # panics: 0
```

脚本还会把 RA 自己加载/展开过程宏时 `pm_lab` 打印的 `[pm_lab] ...` 行带出来 ——
一眼分清是「宏挂了」还是「RA 的某个请求处理器挂了」。

### 不改代码时的临时绕法

```jsonc
{ "rust-analyzer.lens.implementations.enable": false }
```

只关「N implementations」这一种 code lens，`rust-analyzer.lens.enable` 总开关不动，
Run / Debug / references 那几种都还在（键名取自 `rust-analyzer --print-config-schema`）。

但它**只顶一半**：`hover`、`textDocument/implementation` 走的是另外的处理器，撞的是同一个 offset 问题 ——
所以真正的解法是上面那个 `respan`。

## 常用操作

| 场景 | 操作 |
| --- | --- |
| 看某变量被推断成什么类型 | 悬停，或开 inlay hints（内联类型提示） |
| 跳进标准库/依赖源码 | Ctrl+Click（需要 `rust-src` 组件） |
| 看某函数/类型所有引用 | Shift+F12 / Find All References |
| 重命名（含跨文件） | F2（RA 的 rename，比纯文本替换可靠） |
| 跑/调试当前 example | 代码上方的 Run / Debug lens（调试需装 CodeLLDB） |
| 手写 `cargo` 命令 | 命令面板 → "rust-analyzer: Run" 或直接用终端 |

## 附：code lens 到底是什么

调上面那个 bug 时顺带把机制摸了一遍，记下来：

- 它是 LSP 的 `textDocument/codeLens`：**贴在某个条目「上方」的一小行灰字 + 可点动作**
  （RA 默认渲染在名字上方，`rust-analyzer.lens.location` = `above_name`），
  不是文件内容 —— 不进文本、不能被光标选中、不影响编译与格式化；
- 协议是**两步**的：`textDocument/codeLens` 先只返回 `{range, command?, data?}`（`command` 可以是 `null`），
  客户端对这个条目再发 **`codeLens/resolve`**，服务器这时才算「点一下干什么」。
  之所以懒加载，是因为「这个类型有哪些实现 / 有多少引用 / 能不能跑」都很贵 ——
  本次 panic 就正好发生在 resolve 这一步；
- RA 常见的几种：`Run` / `Debug`（函数上方）、`N implementations`、`N references`、`N method references`；
- 两个层次的开头：`editor.codeLens`（VS Code 全局，关掉所有语言）与 `rust-analyzer.lens.*`（RA 决定发哪几种）。

和其它两个容易混的区分：

| | 出现在哪 | 性质 | 交互 |
| --- | --- | --- | --- |
| **code lens** | 某行**上方**独立一行 | 动作入口 | 点击执行命令（所以要 resolve） |
| **inlay hints** | 代码**行内** token 之间 | 纯信息（类型、参数名） | 不可点 |
| **hover** | 鼠标悬停浮层 | 信息（里面也可能带命令链接） | 悬停出现 |

## 与其它工具的边界

- **诊断冲突时以 rustc/clippy 为准**：RA 的检查是增量的、按需的，边界场景（复杂的 trait 求解、
  宏展开）结论可能与真实编译不同。
- RA 不负责格式化，`cargo fmt` 才是；也不负责 lint 规则本身，规则来自 rustc/clippy。
- 大项目首次打开会慢（要跑一遍 `cargo check` 建索引），之后才是增量的爽感。

> 另见：[`vscode-highlight.md`](./vscode-highlight.md) —— 编辑器里的着色其实是
> 「编辑器内置 TextMate 语法（纯正则）+ RA 语义 token」两层叠加；RA 的 `contributes.grammars`
> 里**没有** `source.rust`，Rust 的基础语法不在 RA 里。
