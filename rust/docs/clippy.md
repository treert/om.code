# clippy —— 静态检查（额外 lint 集合）

## 定位

`clippy` 是 **Rust 官方的 lint 工具**，通过 rustup 组件分发，以 cargo 子命令 `cargo clippy` 使用。

一句话区分职责：

| 工具 | 回答的问题 |
| --- | --- |
| `rustc` | 这段代码**能不能编译**？ |
| `cargo clippy` | 能编译，但**写得对不对味**？有没有更惯用/更高效的写法？ |
| `cargo fmt` | 排版**好不好看**？（与逻辑无关） |

技术上 clippy 是**在 rustc 之上挂了一批额外的 lint pass**，所以 `cargo clippy` 大体等于
「`cargo check` + 额外检查」——比 `cargo check` 慢一点点，但能抓的东西多得多。

> 版本号别混：本机 `clippy 0.1.96` 对应的编译器和 `rustc 1.96.0` 是同一版本（ac68faa20, 2026-05-25），
> 只是 clippy 自己走 0.1.x 编号。

## 它都挑什么刺

典型例子（都是 rustc 允许、clippy 会提醒的）：

- 无谓的 `.clone()` / `.to_string()` / `Vec::new()` 后立刻 push；
- 参数该用 `&[T]` 却写了 `&Vec<T>`；该用 `&str` 却写了 `&String`；
- `if x == true`、`if let Some(_) = x` 之类冗余写法；
- 手写循环求和/求最大值（可用 `sum()` / `max()`）；`for i in 0..v.len()` 取元素（可用迭代器）；
- `println!` 里塞字面量、`format!` 参数为空串等格式化小毛病（`print_literal`）；
- 可能隐藏 bug 的写法（`correctness` 组，默认直接 deny）。

## 常用命令

```powershell
cargo clippy --all-targets                  # 检查 main + examples + tests（本仓库最常用）
cargo clippy --all-targets -- -D warnings    # 警告当错误：CI 姿势
cargo clippy --all-targets -- -W clippy::pedantic   # 打开更严的一组规则
cargo clippy --fix --allow-dirty             # 自动修一部分（改完请 review diff）
cargo clippy -- -A clippy::needless_return   # 关掉某条规则
$env:RUSTFLAGS="-D warnings"; cargo clippy    # 另一种全局生效方式
```

lint 分组（可用 `-W clippy::<组名>` 整组开启）：`correctness`（默认 deny）、
`suspicious`、`style`、`complexity`、`perf`（默认 warn）、`pedantic`、`nursery`、`cargo`（默认 allow）。

**持久化**开关写进 `Cargo.toml`（stable 1.74+）：

```toml
[lints.clippy]
print_literal = "allow"      # 例：允许 format 串里混字面量
```

或按作用域临时关掉：

```rust
#[allow(clippy::print_literal)]
```

## 本仓库现状（实测）

```text
cargo clippy --quiet --all-targets      # 只有 1 条警告
warning: literal with an empty format string
  --> examples\func_macro.rs:85:53
```

即 `println!("{:<6} | {:<46} | {}", "kind", "text", "count")` 里的 `"count"` 是字面量，
clippy 建议直接写进格式串。这里**故意保留**：表头和表体复用同一套宽度格式串，改掉反而不好对齐。
要消掉警告，可在该函数上加 `#[allow(clippy::print_literal)]` 并注明原因。

## 与 rustc lint 的关系

- 两者共用同一套机制：`-A`(allow) / `-W`(warn) / `-D`(deny) / `-F`(forbid)，也共用 `#[allow(...)]` 属性。
- **规则集不同**：rustc 管语言层面（未使用变量、死代码、命名规范…），clippy 管写法与质量。
- `cargo fix` 修的是 **rustc** 的 lint；`cargo clippy --fix` 修的是 clippy 的 lint，别搞混。

## 注意

- `--fix` 只处理「有确定替换方案」的规则，不能全指望它；且会直接改源码，先确保工作区干净。
- clippy 的结论**不是法律**：学习代码里刻意演示反例时，`allow` + 注释说明比硬改更合适。
