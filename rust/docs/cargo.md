# cargo —— 构建与包管理入口

## 定位

`cargo` 是 Rust 的**官方构建工具 + 包管理器**。它负责读取 `Cargo.toml`、解析依赖、
组织编译单元，然后**拼出 `rustc` 命令行**去调用编译器。日常写代码 99% 只用 cargo，
不直接碰 rustc。

## 本仓库的 `Cargo.toml`

```toml
[package]
name = "hello_rust"
version = "0.1.0"
edition = "2024"

[dependencies]
```

- `edition = "2024"` 是**语言版次**，决定语法/语义按哪一代规则解析（不等于编译器版本），
  需要 rustc ≥ 1.85；本机 1.96.0 没问题。
- 目前**没有任何外部依赖**，所以 `Cargo.lock` 只锁自己。

## 常用命令

| 命令 | 作用 |
| --- | --- |
| `cargo new <name>` / `cargo init` | 新建 / 在已有目录初始化 package |
| `cargo add <crate>` / `cargo rm <crate>` | 增删依赖（直接改 `Cargo.toml`） |
| `cargo build` | 编译（debug，产物 `target/debug/`） |
| `cargo build --release` | 优化编译（`target/release/`） |
| `cargo check` | **只做类型检查、不生成机器码**，比 build 快，开发时最常用 |
| `cargo run [--args]` | 编译并运行 `src/main.rs` |
| `cargo test` | 跑单元测试 / 集成测试 / 文档测试 |
| `cargo bench` | 跑基准测试 |
| `cargo doc --open` | 生成并打开 API 文档 |
| `cargo fix` | 按 **rustc** 的 lint 建议自动改写代码 |
| `cargo clippy` / `cargo fmt` | 见 [clippy.md](./clippy.md) / [rustfmt.md](./rustfmt.md) |
| `cargo tree` | 显示依赖树 |
| `cargo clean` | 清掉 `target/` |
| `cargo metadata` | 输出依赖/包的机器可读 JSON（写脚本时用） |
| `cargo locate-project` | 打印 `Cargo.toml` 的绝对路径 |
| `cargo config` | 查看当前生效的配置项合并结果 |

`cargo --list` 会打印全部子命令。本机还带有几个**内置别名**和第三方子命令：

```text
b = build    c = check    r = run    t = test    d = doc
create-tauri-app    # 第三方子命令（cargo install 装的），与本仓库无关
```

## 本仓库专用：example 怎么跑

```powershell
cargo run --example type_name       # 运行某个示例（文件：examples/type_name.rs）
cargo run --example func_macro
cargo build --examples              # 编译全部示例（顺带校验）
cargo build --all-targets           # 连 main、tests、benches 一起编译
cargo check --all-targets           # 最快的全量校验
```

> 实测：`cargo run --example` **不带名字会报错**（`--example takes one argument`），
> 只是错误信息里顺带列出可用示例。查有哪些示例请直接看 `examples/` 目录。

## 编译模型（理解 `target/` 里的东西）

- 一个 **package** 可以包含多个 **target**：`src/main.rs`（bin）、`src/lib.rs`（lib）、
  `examples/*.rs`、`tests/*.rs`、`benches/*.rs`。每个 target 是独立编译单元，
  所以示例之间互不影响。
- 增量编译：产物和缓存都在 `target/`（本仓库 `.gitignore` 里已忽略 `/target`），
  首次全量编译慢，之后只重编改动过的 crate。
- 想看 cargo 到底怎么调 rustc：`cargo build -v`；想把额外参数直接传给 rustc：
  `cargo rustc -- --emit=mir`。

## 配置分层

| 文件 | 作用范围 | 是否进版本库 |
| --- | --- | --- |
| `Cargo.toml` | 包本身：名字、版次、依赖、profile、features | 是 |
| `Cargo.lock` | 锁定精确版本（可执行程序应提交） | 是 |
| `.cargo/config.toml` | 本机/本目录行为：镜像源、默认 target、构建参数、别名 | 通常否 |

## 注意

- `cargo build` 后再 `cargo run` 不会重复编译；反过来 `cargo check` 通过**不代表能生成可执行文件**
  （它不做到代码生成那一步）。
- 报错时先看是 **cargo 自己的错**（依赖解析、manifest 格式、找不到 target）还是 **rustc 的错**
  （`error[E0382]: borrow of moved value` 这种），两者的排查方向完全不同。
