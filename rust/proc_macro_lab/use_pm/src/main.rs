//! 消费隔壁 `pm_lab` 里定义的三类过程宏。
//!
//! 运行：在仓库根目录 `rust/` 下执行 `cargo run -p use_pm`
//!
//! 观察两个层次：
//!
//! - **编译期**：cargo 的构建输出里会先冒出几行 `[pm_lab] ...` —— 过程宏在编译期
//!   真的执行了你的代码（改了代码要重新编译才会再打印一次）；
//! - **运行期**：打印的是那些宏**生成出来的代码**的运行结果。

use pm_lab::{Describe, calc, timed};

/// `#[derive(Describe)]` 要实现的 trait —— 由**使用方**提供，不由宏提供。
///
/// 这正是 serde 那一套的套路：派生宏只吐 `impl`，trait 和它的名字都在调用处解析。
/// 所以下面生成代码里的 `Describe` 是**裸名字**，必须在这里可见。
trait Describe {
    fn describe() -> &'static [&'static str];
}

#[derive(Describe)]
struct User {
    id: u64,
    name: String,
    /// 辅助属性：`attributes(describe)` 声明过，编译器才不会把它当未知属性报错
    #[describe(skip)]
    password_hash: String,
}

#[derive(Describe)]
pub struct Config {
    pub(crate) retries: u32,
    verbose: bool,
}

/// `#[timed]` 不给参数时用函数名当标签
#[timed]
fn sum_to(n: u64) -> u64 {
    (1..=n).sum()
}

/// 给了参数就用参数当标签
#[timed("手写循环")]
fn sum_loop(n: u64) -> u64 {
    let mut acc = 0;
    for i in 1..=n {
        acc += i;
    }
    acc
}

/// 函数体里同时有 `?` 和 `return`，用来确认「包进闭包」没有改变语义
#[timed("早退 + ?")]
fn checked_add(a: &str, b: &str) -> Option<i64> {
    let a: i64 = a.parse().ok()?; // 这两个 `?` 是在闭包里起作用的
    if a == 0 {
        return None; // 这个 return 返回的是闭包，闭包再交给函数返回
    }
    let b: i64 = b.parse().ok()?;
    Some(a + b)
}

fn main() {
    // ① 函数式过程宏：带优先级的中缀解析 —— 声明宏做不了的那部分。
    //    编译期打印的 `[pm_lab] calc 解析出: ...` 会把解析器补的括号亮出来。
    println!("calc!(1 + 2 * 3)     = {}", calc!(1 + 2 * 3)); // (1 + (2 * 3)) = 7
    println!("calc!(1 + 2 * 3 * 4) = {}", calc!(1 + 2 * 3 * 4));
    println!("calc!((1 + 2) * 3)   = {}", calc!((1 + 2) * 3)); // 括号是 DSL 的，不是 Rust 的
    println!("calc!(10 - 4 - 3)    = {}", calc!(10 - 4 - 3)); // 左结合 → 3，不是 9
    println!("calc!(100 / 5 / 2)   = {}", calc!(100 / 5 / 2)); // 同样是左结合 → 10
    //
    // 输入写错时，报错会**精确指到出错的那个 token**（这是 `error_at` 里 set_span 的功劳，
    // 声明宏永远做不到）。实测（这两行不能留在文件里，否则编译不过）：
    //
    //   error: 这里需要一个数字，或者一个括号子表达式
    //     --> use_pm/src/main.rs:64:23
    //      |
    //   64 |     let _ = calc!(1 + * 2);
    //      |                       ^
    //
    //   error: 表达式解析完了，后面还有多余的 token
    //     --> use_pm/src/main.rs:65:25
    //      |
    //   65 |     let _ = calc!(1 + 2 3);
    //      |                         ^
    //
    // 对比一下声明宏的失败长相（`examples/macro_ast.rs` 里那条无限递归），
    // 差别就是「知道自己在哪个 token 上出错」。

    // ② 属性宏：签名原样保留，函数体被搬进闭包，前后插了计时
    println!("\nsum_to(1_000_000)   = {}", sum_to(1_000_000));
    println!("sum_loop(1_000_000) = {}", sum_loop(1_000_000));
    println!("checked_add(2, 3)   = {:?}", checked_add("2", "3"));
    println!("checked_add(0, 3)   = {:?}", checked_add("0", "3")); // 闭包里的 return 仍然有效
    println!("checked_add(\"x\", 3)  = {:?}", checked_add("x", "3")); // 闭包里的 ? 也仍然有效

    // ③ 派生宏
    let u = User {
        id: 7,
        name: "Ada".to_string(),
        password_hash: "****".to_string(),
    };
    let c = Config {
        retries: 3,
        verbose: true,
    };
    println!("\nUser::describe()    = {:?}", User::describe());
    println!("Config::describe()  = {:?}", Config::describe());
    // 把字段都读一遍，纯粹是为了不冒 dead_code 警告
    println!(
        "u = {}, {} (hash 长度 {}) / c = {}, {}",
        u.id,
        u.name,
        u.password_hash.len(),
        c.retries,
        c.verbose
    );
    // 编译期打印与运行期输出对照一下：`password_hash` 被 #[describe(skip)] 摘掉了
}
