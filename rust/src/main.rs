//! Rust 学习/实验 crate。
//!
//! 每个语言特性单独放在 `examples/` 下的一个文件里，避免互相干扰：
//!
//! - `examples/func_macro.rs` —— 函数式宏（token 树、macro_rules!）
//! - `examples/macro_ast.rs`  —— 宏递归展开 → 编译期解析出一个 DSL 语法树
//! - `examples/hygiene.rs`    —— 宏卫生性：局部变量 / 标签 / 条目名 / `$crate`
//! - `examples/type_name.rs`  —— `type_name` / `type_name_of_val` 类型名探针
//! - `examples/lifetime.rs`   —— 生命周期：省略规则、`'_`、`'static`
//!
//! 运行某个示例：`cargo run --example type_name`
//! 查看全部示例：直接看 `examples/` 目录
//! 生命周期专题的结论，另见 `docs/lifetime.md`；宏专题（含过程宏）见 `docs/macro.md`。

fn main() {
    println!("本 crate 是学习用的，请运行具体示例：");
    println!("  cargo run --example type_name");
    println!("  cargo run --example func_macro");
    println!("  cargo run --example macro_ast");
    println!("  cargo run --example hygiene");
    println!("  cargo run --example lifetime");
}
