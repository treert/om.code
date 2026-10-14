# rustup —— 工具链管理器

## 定位

`rustup` 是**安装器和版本管理器**，本身**不是编译器**。它管三件事：

| 概念 | 含义 | 例子 |
| --- | --- | --- |
| toolchain | 一整套编译器+标准库，按「发布通道 + 目标平台」区分 | `stable-x86_64-pc-windows-msvc`、`nightly-2026-05-25`、`1.96.0` |
| component | 挂在某个 toolchain 上的可选部件 | `rustc`、`cargo`、`rust-std`、`clippy`、`rustfmt`、`rust-analyzer`、`rust-src`、`rust-docs`、`miri` |
| target | 该 toolchain 能编译到的目标平台（交叉编译用） | `aarch64-apple-darwin`、`x86_64-unknown-linux-gnu` |

> 小坑：`rustup --version` 打印的版本号是 rustup 自己的（本机 1.29.0），
> 和 `rustc --version`（本机 1.96.0）是两码事，rustup 自己也会提醒你这一点。

## 常用命令

```powershell
rustup show                      # 当前激活工具链 + 已装工具链 + 组件概况
rustup show home                 # rustup 的安装目录（本机：C:\Users\treertzhu\.rustup）
rustup toolchain list            # 列出已安装工具链
rustup toolchain install nightly # 安装 nightly
rustup update                    # 更新所有工具链 + rustup 自身
rustup check                     # 只检查有没有更新，不装

rustup component list --installed   # 已装组件（本仓库环境见 ../docs/README.md）
rustup component add rust-src       # 装标准库源码（IDE 看 std 实现需要）
rustup component add miri --toolchain nightly   # miri 只有 nightly 有
rustup component remove clippy

rustup target add aarch64-apple-darwin   # 加交叉编译目标
rustup target list --installed

rustup default stable            # 改全局默认工具链
rustup override set nightly      # 只给当前目录设覆盖（在当前目录执行）
rustup override list             # 看有哪些目录级覆盖
rustup which rustc               # 实际会执行哪个 rustc 可执行文件
rustup run nightly cargo build   # 用指定工具链跑一条命令
rustup doc --std                 # 打开本地标准库文档（离线可看）
rustup doc --book                # 打开《The Rust Programming Language》
```

临时用别的工具链，不必切换，直接在命令前加 `+通道`：

```powershell
cargo +nightly build
rustc +1.96.0 --version
```

## 目录级覆盖：`rust-toolchain.toml`

在项目根目录放一个 `rust-toolchain.toml`，**进入该目录就自动切工具链**，比 `rustup override` 更推荐
（文件进版本库，团队/多机器行为一致）：

```toml
[toolchain]
channel = "stable"          # 或 "1.96.0" / "nightly-2026-05-25"
components = ["rustfmt", "clippy", "rust-src"]
targets = ["aarch64-apple-darwin"]
```

本目录**当前没有**这个文件，用的就是全局默认的 stable 1.96.0。

## 本仓库相关

- 那些 `cargo run --example <name>` 能跑起来，前提是 `cargo`、`rustc`、`rust-std` 这些组件在位；
  换机器后先 `rustup show` 确认，缺了再 `rustup component add`。
- 想试夜间特性（如 `#![feature(...)]`、miri、`cargo expand`）时再装 nightly：
  `rustup toolchain install nightly`，然后用 `cargo +nightly ...` 调用，不必改默认。

## 注意

- `rustup component add` 装的是**二进制分发**的组件，很快；`cargo install xxx` 是另外一回事，
  那是从源码编译第三方工具，慢得多，两者别混。
- 卸载工具链用 `rustup toolchain uninstall <name>`；只删组件用 `component remove`。
