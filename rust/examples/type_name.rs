//! `std::any::type_name` / `type_name_of_val` 行为探针。
//!
//! 三个最容易踩的点：
//!
//! 1. `type_name_of_val(&x)` 的参数签名是 `fn(&T)`，所以调用处那层 `&` 被参数自己吃掉了，
//!    打印出来的是 `x` 的类型；想看 `&T` 本身必须写成 `type_name_of_val(&&x)`。
//! 2. `type_name::<T>()` 只靠类型参数推断，连值都不需要 —— 所以它也能问 `str`、`dyn Fn()`
//!    这些「没有大小、拿不到值」的类型。
//! 3. 输出是编译器内部的类型名（`alloc::string::String` 这种），**格式没有稳定性承诺**，
//!    只能用于调试/教学，绝不能参与逻辑判断（比如 `if name == "&i32"`）。
//!
//! 运行：`cargo run --example type_name`

use std::any::{type_name, type_name_of_val};
use std::cell::{Ref, RefCell};
use std::collections::HashMap;

/// 手写一个 `&T` 参数的小包装，效果与 `type_name_of_val` 逐字相同：
/// 调用处的 `&` 只用于推断 `T`，不会出现在结果里。
fn name_of<T: ?Sized>(_: &T) -> &'static str {
    type_name::<T>()
}

fn main() {
    section_borrow_layers();
    section_common_types();
    section_type_level_only();
    section_deref_probe();
}

/// ① 一层 `&` 到底被谁吃掉 —— 这是 `type_name_of_val` 最反直觉的地方。
fn section_borrow_layers() {
    println!("== ① 引用层数：谁被吃掉了 ==");

    let x = 42i32;
    println!("type_name_of_val(&x)    = {}", type_name_of_val(&x)); // x 的类型：i32
    println!("type_name_of_val(&&x)   = {}", type_name_of_val(&&x)); // 补一层才看到 &i32
    println!("type_name_of_val(&&&x)  = {}", type_name_of_val(&&&x));
    println!("name_of(&x)             = {}", name_of(&x)); // 自写包装，等价
    println!("type_name::<&i32>()     = {}", type_name::<&i32>()); // 纯类型写法
    println!("type_name::<&&i32>()    = {}", type_name::<&&i32>());
}

/// ② 常见类型的「真名」：容器、数组、切片、元组、函数指针、闭包、trait 对象。
fn section_common_types() {
    println!("\n== ② 常见类型的内部表示 ==");

    let v: Vec<String> = vec!["a".to_string()];
    let m: HashMap<&str, u32> = HashMap::new();
    let arr = [1u8, 2, 3];
    let slice: &[u8] = &arr;
    let tup = (1i32, "x", 3.0f64);
    let fp: fn(i32) -> i32 = |n| n * 2;
    let s: &str = "hi";

    println!("Vec<String>        = {}", name_of(&v));
    println!("HashMap<&str, u32> = {}", name_of(&m));
    println!("[u8; 3]            = {}", name_of(&arr)); // 数组带长度，是独立类型
    println!("&[u8]              = {}", name_of(&slice)); // 切片不带长度
    println!("(i32, &str, f64)   = {}", name_of(&tup));
    println!("fn(i32) -> i32     = {}", name_of(&fp)); // 函数指针：类型里只有签名
    println!("&str               = {}", name_of(&s));

    // 闭包是「匿名结构体」：每个闭包字面量一个独立类型，但打印出的名字里只有 {{closure}}，
    // 不区分具体是哪一个 —— 这正好说明字符串形式有信息损失，不能用它来区分类型。
    let cl = |n: i32| n + 1;
    println!("闭包               = {}", name_of(&cl));

    // 泛型函数实例化后，名字里会带上实参
    println!("Option<RefCell<i32>> = {}", type_name::<Option<RefCell<i32>>>());
}

/// ③ 只有类型、没有值也能问 —— `type_name::<T>()` 是纯类型层面的。
fn section_type_level_only() {
    println!("\n== ③ 不存在的值也能问类型 ==");

    // 下面这些类型都没有大小（DST），无法拥有值，但仍可作为类型参数
    println!("str                = {}", type_name::<str>());
    println!("[u8]               = {}", type_name::<[u8]>());
    println!("dyn Fn(i32) -> i32 = {}", type_name::<dyn Fn(i32) -> i32>());
    println!("&dyn std::fmt::Debug = {}", type_name::<&dyn std::fmt::Debug>());

    // 生命周期基本被擦掉：`&'static str` 与 `&'a str` 打印结果完全一样（都是 `&str`），
    // 只有出现在泛型实参位置时才会露出一个 `'_` 占位（见 ④ 的 Ref<'_, String>）
    println!("&'static str       = {}", type_name::<&'static str>());
    println!("&'a str（任意生命周期）= {}", type_name::<&str>());
}

/// ④ 原始 refcell 探针：`Ref<T>`、`&Ref<T>`、`*r`、`&*r`、`&&*r` 分别是什么。
fn section_deref_probe() {
    println!("\n== ④ Deref 场景：RefCell 的借用守卫 ==");

    let cell = RefCell::new(String::from("Hello"));
    let r: Ref<'_, String> = cell.borrow();

    println!("r（Ref 守卫自身）   = {}", type_name_of_val(&r));
    println!("&r（对守卫取引用）  = {}", type_name_of_val(&&r));
    println!("*r（解引用后的位置）= {}", type_name_of_val(&*r)); // String 本身
    println!("&*r                = {}", type_name_of_val(&&*r)); // 补一层才看到 &String

    // *r 只能读不能移动：String 非 Copy
    println!("*r 的内容           = {}", *r);
    // let moved = *r;  // 报错：cannot move out of `*r` which is behind a shared reference

    // Copy 类型则可以经由 *r 直接取值
    let int_cell = RefCell::new(42);
    let val: i32 = *int_cell.borrow();
    println!("Copy 类型 *ri       = {val}");

    // 借用是运行时检查的，drop 掉守卫后才允许可变借用
    drop(r);
    *cell.borrow_mut() = "World".to_string();
    println!("修改后              = {}", cell.borrow());
}
