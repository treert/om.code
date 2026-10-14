# AGENTS.md —— rust 学习/实验目录

> 本文件是**规则/约定类文档**，内容求**稳定、少改动**：不放具体文件清单、版本号、目录树细节等易变事实
> （那些去看目录本身，或 `docs/README.md` 里的实测表）。改动这里等于改长期规矩，请慎重。

## 目的

这个目录是 `om.code` 仓库的子目录，**专门用来学习、验证 Rust 的各种机制**。

它不是一个产品项目，而是一堆「可运行的小实验」：

- 遇到某个语言机制（借用检查、Deref、宏、生命周期、trait 对象、async…）想弄明白时，
  在这里写一个最小样例，**用真实输出/编译器报错来验证理解**，而不是停留在记忆和猜测。
- 每个实验是一个独立可跑的 `examples/*.rs`，可以随时单独运行、互不干扰。
- 结论以**实际运行结果**为准：注释里写的类型、行为，都必须和 `cargo run` 的输出对得上。

## 当前结构

```
rust/
├── Cargo.toml            # package: hello_rust, edition 2024
├── AGENTS.md             # 本文件
├── src/main.rs           # 不是真入口，只做「示例索引」：列出各 example 及运行命令
├── examples/             # 每个主题一个独立可运行文件，命名规则见下方「约定」
└── docs/                 # 工具链各工具的说明文档，一个工具一篇（索引见 docs/README.md）
```

`examples/` 和 `docs/` 下的具体文件会不断增加和调整，**本文件不维护文件清单**，需要时直接看目录。

## 约定

1. **新主题 = `examples/` 下新增一个文件**，文件名用主题的 snake_case（如 `lifetime.rs`、`async_basic.rs`），
   不要把实验代码堆进 `src/main.rs`。
2. 每个 example 顶部写 `//!` 文档注释，说明：**这个实验想验证什么** + **运行命令**（`cargo run --example <name>`）。
3. 新增 example 后，顺手在 `src/main.rs` 的文档列表和 `main()` 打印里补一行，保持索引有效。
4. 注释用中文，风格是「讲清为什么」而不是复述代码；涉及坑点/反直觉行为时，
   把**结论**和**证据**（实际打印出来的类型名、报错信息）一起写上。
5. 想演示「会报错」的代码时，用注释形式保留在原位并注明编译器错误信息，保证文件始终能编译。
6. 尽量不引入外部依赖；确实需要时先说明理由，并写入 `Cargo.toml`。
7. 改完必须验证：`cargo build --all-targets`，以及 `cargo run --example <name>` 检查输出是否和注释描述一致。
8. **工具类说明放 `docs/`，一个工具一篇**（`rustup`、`cargo`、`rustc`、`clippy`、`rustfmt`、`rust-analyzer`…），
   新增时同步登记到 `docs/README.md` 的索引表；文中命令和版本号要与本机实测结果一致。

## 常用命令

```text
cargo run --example <name>        # 运行某个示例（<name> = examples/ 下的文件名，不带 .rs）
cargo build --examples            # 编译全部示例（顺带校验）
cargo build --all-targets         # 编译 main 与所有 example
cargo clippy --all-targets        # 静态检查（见 docs/clippy.md）
cargo fmt --all                   # 格式化（见 docs/rustfmt.md）
```

想知道有哪些示例：直接看 `examples/` 目录；或者运行不带名字的 `cargo run --example`，
它会报「takes one argument」并把可用示例列表打印出来（不是正常用法，仅用于速查）。
各工具怎么用、本机版本是多少：见 `docs/README.md`。

## 注意

- `target/` 是构建产物，不要提交、也不要手工修改。
- 这里允许「学完就扔」的代码，但**不允许编译不过或跑不出结果的代码**留在目录里。
