//! Rust 学习/实验 crate。
//!
//! 每个语言特性单独放在 `examples/` 下的一个文件里，避免互相干扰：
//!
//! - `examples/func_macro.rs` —— 函数式宏（token 树、macro_rules!）
//! - `examples/type_name.rs`  —— `type_name` / `type_name_of_val` 类型名探针
//!
//! 运行某个示例：`cargo run --example type_name`
//! 查看全部示例：直接看 `examples/` 目录

fn main() {
    println!("本 crate 是学习用的，请运行具体示例：");
    println!("  cargo run --example type_name");
    println!("  cargo run --example func_macro");
}
