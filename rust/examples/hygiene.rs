//! 宏卫生性（hygiene）示例 —— `macro_rules!` 到底「隔离」什么、「共享」什么。
//!
//! 运行：`cargo run --example hygiene`
//!
//! 一句话：`macro_rules!` 是**部分卫生**的，规则按「名字的类别」分，不是一刀切：
//!
//! | 名字从哪来 | 归属哪个上下文 | 效果 |
//! | --- | --- | --- |
//! | 宏体里新写的**局部变量 / 标签** | 定义处（def-site） | 调用处看不见、也撞不上（真正卫生） |
//! | 调用处**传进来**的 token（`$e` / `$n`…） | 调用处（call-site） | 就是调用处那个名字，两边互相可见 |
//! | 宏体里定义的**条目**（fn / const / struct…） | 调用处（call-site） | 调用处能直接按名字用（不卫生） |
//! | 宏体里写的**路径**（不加 `$crate`） | 调用处 | 在调用处所在的模块里解析（不卫生） |
//!
//! 每条结论都由本文件的**真实输出**或**真实编译报错**支撑；会报错的写法以注释形式
//! 留在原位，并附上 rustc 报错原文，保证本文件始终能编译。
//!
//! 展开机制（token 树、`$()` 重复、递归、规则顺序）见 `examples/func_macro.rs`；
//! 过程宏（proc macro）见 `docs/macro.md`。

// ---------------------------------------------------------------------------
// ① 局部变量：宏体里 let 出来的 x，与调用处的 x 是两个不同的 x
// ---------------------------------------------------------------------------
macro_rules! define_local_x {
    () => {
        let x = 1;
        // 注意：连格式串里的 `{x}` 也解析到宏体内这个 x（字面量 token 属于定义处上下文）
        println!("  宏体内的 x   = {x}");
    };
}

// ---------------------------------------------------------------------------
// ② 传进来的 token：保持在调用处的上下文
// ---------------------------------------------------------------------------
// 宏体里也有一个 x，但 `$e` 是调用处传进来的 token，它里面的 x 只能是调用处的 x。
macro_rules! with_local_x {
    ($e:expr) => {{
        let x = 1;
        println!("  宏体内的 x   = {x}");
        $e
    }};
}

// ---------------------------------------------------------------------------
// ③ 用传进来的 ident 声明变量 = 声明调用处那个名字
// ---------------------------------------------------------------------------
macro_rules! declare {
    ($n:ident) => {
        let $n = 1;
    };
}

// ---------------------------------------------------------------------------
// ④ 条目名不卫生：宏体里的 fn / const 会在调用处所在的作用域里定义出来
// ---------------------------------------------------------------------------
macro_rules! def_items {
    () => {
        fn generated() -> &'static str {
            "宏体里定义的 fn"
        }
        const GENERATED: usize = 7;
    };
}
//
// 非编译示例：同一个作用域里调用两次 —— 条目是**真的**定义了两次，于是重名。
//
// fn two_calls() {
//     def_items!();
//     def_items!();
// }
//
// 实测报错（rustc 1.96.0）：
//   error[E0428]: the name `generated` is defined multiple times
//    --> probe.rs:4:9
//     |
//   4 |         fn generated() -> &'static str {
//     |         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ redefined here
//     |         previous definition of the value `generated` here
//  12 |     def_items!();
//     |     ------------ in this macro invocation
//     |
//     = note: `generated` must be defined only once in the value namespace of this block

// ---------------------------------------------------------------------------
// ⑤ 标签：卫生 —— 宏体里的 'lbl 与调用处的 'lbl 互不可见
// ---------------------------------------------------------------------------
macro_rules! one_round {
    ($e:expr) => {{
        'lbl: loop {
            break 'lbl;
        }
        $e
    }};
}
//
// 非编译示例：调用处想用**宏体内**那个 'lbl —— 看不见，于是报未声明。
//
// macro_rules! run_in_loop {
//     ($e:expr) => {{ 'lbl: loop { $e } }};
// }
// fn main() {
//     run_in_loop!(break 'lbl);
// }
//
// 实测报错（rustc 1.96.0）：
//   error[E0426]: use of undeclared label `'lbl`
//    --> probe.rs:12:24
//     |
//  12 |     run_in_loop!(break 'lbl);
//     |                        ^^^^ undeclared label `'lbl`
//   （另有 warning: unused label —— 宏内那个 'lbl 因此没人用）

// ---------------------------------------------------------------------------
// ⑥ 路径：默认在调用处解析，`$crate::` 把它拉回宏定义所在的 crate
// ---------------------------------------------------------------------------
mod inner {
    pub fn tag() -> &'static str {
        "crate::inner::tag"
    }
}

macro_rules! call_tag_with_crate {
    () => {
        $crate::inner::tag()
    };
}
//
// 非编译示例：把 `$crate::` 去掉后，同一段调用就编不过了。
//
// macro_rules! call_tag { () => { inner::tag() }; }
// mod guest { pub fn run() -> &'static str { call_tag!() } }
//
// 实测报错（rustc 1.96.0）：
//   error[E0433]: cannot find module or crate `inner` in this scope
//    --> probe.rs:10:9
//     |
//  10 |         inner::tag()
//     |         ^^^^^ use of unresolved module or unlinked crate `inner`
//     |
//     = help: consider importing this module
//   15 +     use crate::inner;
//
// 即：宏体里的裸路径在**调用处**的模块里解析，`inner` 在 guest 里并不存在。

mod guest {
    // 本模块没有 `use crate::inner;`，`inner` 这个名字在这里根本不可见，
    // 能编过完全靠宏里的 `$crate::`。
    pub fn run() -> &'static str {
        call_tag_with_crate!()
    }
}

fn main() {
    println!("== ① 局部变量卫生 ==");
    let x = 2;
    define_local_x!();
    println!("  调用处的 x   = {x}        ← 宏里的 x 没有串出来");

    println!("\n== ② 传入的 token 属于调用处 ==");
    let x = 100;
    println!("  $e 里的 x    = {}", with_local_x!(x));

    println!("\n== ③ 传入的 ident 就是调用处的名字 ==");
    let x = 2;
    println!("  declare! 之前 x = {x}");
    declare!(x);
    println!("  declare! 之后 x = {x}        ← 被宏里那句 let 遮蔽了（同名，非卫生）");

    println!("\n== ④ 条目名不卫生 ==");
    def_items!();
    println!("  {} / GENERATED = {}", generated(), GENERATED);

    println!("\n== ⑤ 标签卫生 ==");
    let mut rounds = 0;
    'lbl: loop {
        one_round!(println!("  $e 里的 'lbl 指向调用处这个循环"));
        rounds += 1;
        if rounds >= 2 {
            break 'lbl;
        }
    }
    println!("  调用处的 'lbl 跑了 {rounds} 轮，与宏内那个 'lbl 互不相干");

    println!("\n== ⑥ $crate 决定路径在哪解析 ==");
    println!("  guest::run() = {}", guest::run());
    println!("\n小结：局部变量和标签卫生；传入的 token、条目名、裸路径都不卫生。");
}
