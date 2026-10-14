# rustc —— 编译器本体

## 定位

`rustc` 才是**真正把 Rust 源码变成二进制/库的编译器**。cargo 只是负责「读 manifest、算依赖、排顺序」，
最后逐 crate 调用 `rustc`。所以：

- 有 Cargo 项目时，命令写 `cargo build`，**不要**手写 rustc 命令；
- 只想快速试一小段代码时，可以直接 `rustc single.rs`，省掉建项目。

```powershell
rustc hello.rs -o hello.exe          # 单文件编译成可执行
rustc --edition 2024 hello.rs        # 指定语言版次（默认是按最老的 2015！）
rustc --crate-type lib lib.rs        # 生成 rlib 而不是可执行文件
rustc --version                      # 本机：rustc 1.96.0 (ac68faa20 2026-05-25)
```

> 单文件编译时 `--edition` **不会**自动从 `Cargo.toml` 继承，不写就是 2015 版语义，
> 这是新手直接用 rustc 最容易踩的坑。

## 常用参数（值得记住的一小撮）

| 参数 | 作用 |
| --- | --- |
| `-O` | 等价 `-C opt-level=2`（优化） |
| `--crate-type bin/lib/rlib/dylib` | 输出形态 |
| `--emit=mir,llvm-ir,asm,obj,link` | 让编译器**吐中间产物**，看优化结果用 `--emit=asm` 或 `mir` |
| `--print cfg` | 打印当前目标的 cfg 列表（写条件编译时必须看） |
| `--print target-list` | 列出所有可编译目标 |
| `--explain E0382` | 查错误码的详细解释（离线） |
| `--error-format=json` | 机器可读诊断（工具链用） |
| `-A / -W / -D / -F <lint>` | 调 lint 级别：allow / warn / deny / forbid |
| `--test` | 把文件当测试 harness 编译 |

## lint 级别：rustc 自带的「体检项」

rustc 内部就有一批 lint（`unused_variables`、`dead_code`、`unused_mut`、`non_snake_case`…），
无需装任何工具，用 `-W`/`-D` 调级别：

```powershell
cargo rustc -- -W unused_variables                    # 给某一个 target 追加 rustc 参数
$env:RUSTFLAGS="-D warnings"; cargo build             # 影响本次构建的所有 crate
$env:RUSTFLAGS="-A dead_code";  cargo check

cargo clippy -- -D warnings                           # clippy 的命令行参数同理（clippy 侧）
```

> 实测踩坑：`cargo build -- -W unused_variables` **不合法**，cargo 会直接报
> `error: unexpected argument '-W' found`。`--` 后的参数只对 `cargo run`（传给程序）、
> `cargo test`（传给测试 harness）、`cargo rustc`/`cargo clippy`（传给编译器）有效。

想**持久化** lint 配置（而不是每次敲命令行），写进 `Cargo.toml`（stable 1.74+ 支持）：

```toml
[lints.rust]
dead_code = "allow"          # 探索阶段常见：先把没写完的代码跑起来

[lints.clippy]
print_literal = "allow"
```

代码里也能按作用域控制：

```rust
#![allow(dead_code)]            // 整个 crate
#[allow(unused_variables)]      // 单个函数/项
fn f(_unused: i32) {}
```

> 注意区分：rustc 的 lint 是**语言规则层面**的，clippy 是**额外的风格/质量规则**，
> 两者用同一套 `-A/-W/-D` 机制，但规则集合不同（见 [clippy.md](./clippy.md)）。

## 看 cargo 背后发生了什么

```powershell
cargo build -v                  # 打印完整的 rustc 调用命令
cargo rustc -- --emit=mir       # 追加参数，产物落在 target/<profile>/deps/ 下的 .mir 文件
cargo build --timings           # 生成编译耗时报告（HTML）
```

写学习笔记时，`--emit=mir` 特别有用：能直接看到「这段代码在 Rust 眼里被翻译成了什么」。

## 错误信息怎么读

```text
error[E0382]: borrow of moved value: `s`
  --> src/main.rs:5:20
   |
 3 |     let s = String::from("hi");
   |         - move occurs because `s` has type `String`...
```

- `error[E0xxx]`：**错误码**。想深挖就 `rustc --explain E0382`，不用联网。
- 箭头 + 多行标注 = 编译器给出的「数据流故事」，比报错本身更值得读。
- `error` 是必须修；`warning` 默认只是提醒，但**建议在实验代码里也清零**，
  因为警告经常正是理解偏差的信号（比如你以为在用某个变量，其实它没被用）。
