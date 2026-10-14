# rustfmt —— 代码格式化

## 定位

`rustfmt` 是 **Rust 官方的格式化器**：它只改**排版**（缩进、换行、空格、逗号位置…），
不改变代码语义，也不做任何逻辑审查。`cargo fmt` 是它的 cargo 封装。

对应关系：`cargo fmt` → 调用 `rustfmt`（本机 `rustfmt 1.9.0-stable`）。

## 常用命令

```powershell
cargo fmt                       # 格式化当前 package 的 bin/lib
cargo fmt --all                 # 含 workspace 所有成员 / 所有 target
cargo fmt -- --check            # 只检查不改（CI 用），有差异返回非 0
cargo fmt --all --check
cargo fmt -- --edition 2024     # 明确指定版次（正常情况下 cargo 会带上）
rustfmt src/main.rs             # 直接对单文件格式化
rustfmt --check src/main.rs     # 单文件检查
```

配合编辑器保存时自动格式化，基本不用手动敲这些命令。

## 本仓库现状（实测）

```text
cargo fmt --all --check
Diff in ...\examples\type_name.rs:67:
-    println!("Option<RefCell<i32>> = {}", type_name::<Option<RefCell<i32>>>());
+    println!(
+        "Option<RefCell<i32>> = {}",
+        type_name::<Option<RefCell<i32>>>()
+    );
Diff in ...\examples\type_name.rs:78:
-    println!("&dyn std::fmt::Debug = {}", type_name::<&dyn std::fmt::Debug>());
+    println!(
+        "&dyn std::fmt::Debug = {}",
+        type_name::<&dyn std::fmt::Debug>()
+    );
```

也就是**当前仓库还没跑过 fmt**，`examples/type_name.rs` 里有两处（第 70、81 行）需要折行。

注意这两处**并不是超过 100 列**（实测那些行都不到 100 字符），而是撞上了 rustfmt 的
**"small heuristics"**：调用宽度默认只允许 `fn_call_width = 60`（= max_width 的 60%），
一旦一个函数调用的参数总宽度超过它，即使整行没到 100 列，也会被拆成多行。
这是「为什么我的代码没超宽还是被 fmt 拆了」的常见原因，想放宽就调 `use_small_heuristics`
或 `fn_call_width`（见下方配置）。

想让仓库保持整洁，跑一次即可：

```powershell
cargo fmt --all
cargo fmt --all --check     # 应该变成无输出
```

## 配置：`rustfmt.toml`

在项目根目录建 `rustfmt.toml`（或 `.rustfmt.toml`）即可，常用稳定选项：

```toml
edition = "2024"       # 关键：不写的话，单独跑 rustfmt 会按 2015 版解析语法
max_width = 100        # 默认 100
tab_spaces = 4         # 默认 4
newline_style = "Auto" # Windows 上很重要：避免 CRLF/LF 互改导致整文件 diff
use_small_heuristics = "Default"
```

**nightly-only 选项**（想用 `cargo +nightly fmt` 并加 `unstable_features = true`）：
`imports_granularity`、`group_imports`、`wrap_comments`、`format_strings`、`format_code_in_doc_comments`。
这些是「把 use 归并成一条」「自动排版注释和 format 串」之类的高级欲望，stable 上不可用。

本仓库**目前没有** `rustfmt.toml`，用的全是默认值。

## 与 clippy 的分工

| 关心点 | 归谁管 |
| --- | --- |
| 缩进、折行、空格、逗号 | `rustfmt` |
| 冗余 clone、`&Vec` vs `&[T]`、惯用写法 | `clippy` |
| 未使用变量、死代码、命名规范 | `rustc` |

`rustfmt` 永远**不会**告诉你「这段代码有更好的写法」，那是 clippy 的活。

## 注意

- 首次对旧文件跑 fmt 会产出**很大的 diff**，建议单独一次提交（"format only"），
  不要和逻辑改动混在一起，否则 review 时全是噪音。
- Windows 上如果文件是 CRLF 而 rustfmt 想写 LF（或相反），会出现「只改了行尾」的全文件 diff。
  本仓库根目录的 `.gitattributes` 已经有 `* text=auto`（提交时按 LF 归一化），风险不大；
  若真遇到，可在 `rustfmt.toml` 里显式写 `newline_style = "Auto"`（这也是默认值的行为）。
- fmt 不保证「只动空白」到字面层面（比如它可能给尾表达式补分号、给 `match` 臂调整逗号），
  但语义等价；跑完用 `cargo build --all-targets` 复核一次即可。
