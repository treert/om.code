//! 生命周期（lifetime）示例：它到底约束什么、什么时候可以省、`'_` 是什么。
//!
//! 结论先行：
//!
//! 1. 生命周期是**类型的一部分**（`&'a T`），只活在编译期；编译产物里没有它的痕迹，
//!    所以 `std::any::type_name` 打印不出它（见 `examples/type_name.rs` ③）。
//! 2. 它描述的是「这个引用指向的数据至少要活到什么时候」——它约束**代码**，
//!    但**不会改变**任何东西实际活多久；标注写错只会编译失败，不会延长寿命。
//! 3. 绝大多数地方编译器自己算（省略规则 + NLL），只有跨函数边界说不清时才需要手写。
//! 4. `'_` 是匿名生命周期占位符：「这里确实有个生命周期参数，但不用给它起名字」。
//!
//! 什么时候必须手写：
//!
//! - 多个引用入参 + 返回引用 → 得说明返回的引用跟着谁（①）
//! - struct / enum 里存引用 → 类型定义本身必须带生命周期参数（②）
//! - 泛型实参位置（`Formatter<'_>`、`Ref<'_, T>`）→ 惯例写 `'_`（struct 字段/返回位置则是硬性要求）（③）
//! - `'static` 相关约束（④）
//!
//! 运行：`cargo run --example lifetime`

use std::any::Any;
use std::cell::{Ref, RefCell};
use std::fmt;

// ---------------------------------------------------------------------------
// ① 函数签名（入参/返回引用）+ NLL：借用什么时候结束
// ---------------------------------------------------------------------------

/// 只有一个引用入参时，省略规则第 2 条生效：输出生命周期 = 输入生命周期。
/// 完全等价于 `fn first_word<'a>(s: &'a str) -> &'a str`，所以这里不用写。
fn first_word(s: &str) -> &str {
    s.split_whitespace().next().unwrap_or("")
}

/// 两个引用入参 + 返回引用：省略规则**失效**，必须手工说明返回的引用跟着谁。
///
/// 下面这行如果把 `<'a>` 和两个 `'a` 去掉，会报（原文，rustc 1.96.0）：
///
/// ```text
/// error[E0106]: missing lifetime specifier
///   = help: this function's return type contains a borrowed value, but the
///     signature does not say whether it is borrowed from `x` or `y`
/// help: consider introducing a named lifetime parameter
/// ```
///
/// 语义要点：`'a` 取的是 `x`、`y` 两者有效期的**交集**。调用方必须保证两个实参
/// 在返回值被用完之前都还活着（通常表现为「短命的那个参数会把返回值一起拖下水」）。
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() >= y.len() { x } else { y }
}

// 另一个经典错误：返回局部变量的引用（和省略规则无关，是纯粹的所有权问题）。
//
// fn dangling() -> &'static str {
//     let s = String::from("x");
//     &s
// }
//
// error[E0515]: cannot return reference to local variable `s`
//   |     ^^ returns a reference to data owned by the current function
//
// 注意：写 `-> &str`（零个引用入参）会先在签名处报 E0106 —— 省略规则要求
// 「恰好一个输入生命周期」才能推到输出，这里一个都没有。

/// 带 `Drop` 的探针：用来证明 NLL **不会**提前销毁任何值。
struct Noisy(&'static str);

impl Drop for Noisy {
    fn drop(&mut self) {
        println!("  [drop] {}", self.0);
    }
}

/// ① 演示：省略规则生效/失效的差别，以及 NLL 让**借用**提前结束（但不提前 drop 值）。
fn section_fn_signatures() {
    println!("== ① 函数签名里的生命周期 ==");

    let s = String::from("hello brave new world");
    // 单入参 → 返回值直接绑到 s 上，s 活着 w 就有效
    let w = first_word(&s);
    println!("first_word(&s)  = {w:?}");

    let a = String::from("abcd");
    let b = String::from("xy");
    // a、b 都必须活到 println 这行之后（'a = 两者交集）
    println!("longest(&a, &b) = {:?}", longest(&a, &b));

    // NLL（non-lexical lifetimes，非词法生命周期）：**借用（loan）** 的有效期到
    // 「最后一次被使用」结束，而不是到词法作用域（大括号）结束。
    //
    // 先分清三样东西，别混：
    //   v      —— 被借用的**所有者**（`Vec<i32>`，普通局部变量），NLL 不动它的存活区间
    //   last   —— **引用值** `&i32`，NLL 只缩短它类型里那个 region，不是「提前结束这个变量」
    //   loan   —— borrowck 记的账「v 正处于不可变借用中」，**这才是 NLL 缩短的对象**
    //
    // 另：`v.push(4)` 要的是 `&mut self`，所以它展开成 `Vec::push(&mut v, 4)` —— 是一次可变借用，
    // 于是和上面那个还没结束的 `&v` 冲突。如果 `last` 在 `push` **之后**才被使用，
    // 就是真的冲突（原文，rustc 1.96.0）：
    //
    //     let mut v = vec![1, 2, 3];
    //     let first = &v[0];          //   | - immutable borrow occurs here
    //     v.push(4);                  //   | ^^^^^^^^^ mutable borrow occurs here
    //     println!("{first}");        //   |      ----- immutable borrow later used here
    //
    //     error[E0502]: cannot borrow `v` as mutable because it is also borrowed as immutable
    //
    // 注意最后一行注解：编译器判定的依据是「借用**后来被用到**的地方」，不是「变量在哪个大括号里」。
    // NLL 之前（Rust 1.31 / 2018 edition 以前）用的是词法作用域，本函数里这种
    // 「先打印 last、再 push」的写法同样会被判为冲突。
    let mut v = vec![1, 2, 3];
    let last = &v[v.len() - 1];
    println!("last            = {last}");

    // 上一行之后 last 不再被使用 → 借用已结束 → 下面两次调用都合法。
    //
    // 这两行是同一个函数的两种写法（方法调用 vs UFCS），输出完全等价：
    //     v.push(4)                    // 你写的
    //     Vec::<i32>::push(&mut v, 4)  // 编译器实际做的：接收者被自动取成 `&mut v`
    //
    // 写成下面这种 UFCS 形式，是为了把那个**隐含的可变借用**摆到明面上 ——
    // `push` 的签名是 `fn push(&mut self, value: T)`，所以 `v.push(..)` 本质就是
    // 一次 `&mut v`，这也正是它会和前面 `&v[..]` 的借用冲突的原因。
    v.push(4);
    Vec::<i32>::push(&mut v, 5); // 等价写法：&mut v 显式可见
    println!("push 之后 v     = {v:?}");

    // 反证：NLL 缩短的只是「借用必须被认为有效」的区间，**不会**提前 drop 任何值。
    // `d` 是带 Drop 探针的值，借用早在上面 `v.push(4)` 时就结束了，但它照样活到本函数返回
    // —— 输出里那行 `[drop] d` 会出现在本函数结束时，而不是借用在 push 时结束的那一刻。
    let d = Noisy("d");
    println!("d 还活着        = {}（等本函数结束才 drop）", d.0);
}

// ---------------------------------------------------------------------------
// ② struct 里存引用：类型定义必须带生命周期参数
// ---------------------------------------------------------------------------

/// 引用原文的一段，**不拷贝**。
///
/// `'a` 的含义：`Excerpt` 的实例不能比它引用的那段数据活得更久。
#[derive(Debug)]
struct Excerpt<'a> {
    part: &'a str,
}

impl<'a> Excerpt<'a> {
    /// 构造：入参的 `&'a str` 决定了实例自身的 `'a`
    fn new(part: &'a str) -> Self {
        Excerpt { part }
    }

    /// 省略规则第 3 条：只要签名里有 `&self`，所有被省略的输出生命周期
    /// **一律**取 `self` 的生命周期，其它引用入参（这里是 `_other`）不参与判断。
    ///
    /// 展开后是：`fn part_ignoring<'s, 'b>(&'s self, _other: &'b str) -> &'s str`
    fn part_ignoring(&self, _other: &str) -> &str {
        self.part
    }
}

/// 整块 `impl` 不需要给生命周期起名字，写 `'_` 即可（Rust 2018+）。
impl fmt::Display for Excerpt<'_> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "\"{}\"", self.part)
    }
}

/// ② 演示：实例的寿命被原数据锚住；以及省略规则第 3 条的实证。
fn section_struct_with_ref() {
    println!("\n== ② struct 里的引用 ==");

    let text = String::from("the quick brown fox");

    let ex: Excerpt<'_> = Excerpt::new(&text); // ex 借用了 text
    println!("excerpt      = {ex}");

    // 只要 ex 还活着，text 就不能被移动/销毁：
    // drop(text);  // error[E0505]: cannot move out of `text` because it is borrowed

    // 规则 3 实证：返回值只跟 self 有关，与 `_other` 的寿命无关。
    let short_lived = String::from("temporary");
    let r = ex.part_ignoring(&short_lived);
    drop(short_lived); // 若 r 绑到了 short_lived，这一行会编译失败
    println!("规则 3 返回值 = {r:?}  （short_lived 已 drop，r 依旧有效）");
}

// ---------------------------------------------------------------------------
// ③ `'_`：匿名生命周期占位符
// ---------------------------------------------------------------------------

/// 语义上 `'_` 等于「给我一个新参数，但我不用它的名字」。
///
/// 这里写 `Excerpt<'_>` 是**惯例写法**，注意别把「惯例」当成「必须」：
///
/// - 函数参数 / `let` 标注位置，写光秃秃的 `Excerpt` **也能编译**，
///   只是会触发默认关闭的 lint：
///   `elided_lifetimes_in_paths` → `warning: hidden lifetime parameters in types are deprecated`
///   `help: indicate the anonymous lifetime`（即让你补上 `'_`）。
/// - 真正**必须**写的地方是「省略规则管不到」的位置，例如 struct 字段：
///   `struct Holder { w: Wrap }` → `error[E0106]: missing lifetime specifier`。
///   函数**返回**位置也一样：没有任何引用入参时写 `-> &str` 就是 E0106。
///
/// 之所以到处都能看到 `'_`，是因为开这个 lint（或 follow rustc 惯例）之后它就是标准写法。
fn describe(e: Excerpt<'_>) -> String {
    format!("{e} ({} 字节)", e.part.len())
}

/// ③ 演示：`'_` 的几种出现位置。
fn section_placeholder_lifetime() {
    println!("\n== ③ `'_` 匿名生命周期 ==");

    // 1) 函数参数里的泛型实参位置 —— 惯例写法（不写 `'_` 也能编译，只是会被 lint 提醒）
    let s = String::from("placeholder demo");
    println!("describe(Excerpt<'_>) = {}", describe(Excerpt { part: &s }));

    // 2) 局部变量类型标注：等价于不标注，只是把「这里有个我不管的生命周期」写明
    let cell = RefCell::new(String::from("Hello"));
    let r: Ref<'_, String> = cell.borrow();
    println!("Ref<'_, String>       = {}", *r);
    drop(r);

    // 3) 标准库里最常见的一个：`fmt::Display::fmt` 的签名（见 examples/func_macro.rs:21）
    //
    //        fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result
    //
    //    `Formatter<'a>` 里的 'a 是「底层输出缓冲区」的有效期，由调用方决定，
    //    这里写 `'_` 让编译器去推断；而且它是 f 那层 `&mut` **之后**的独立参数，
    //    与 `&self` 的生命周期没有任何关系（容易被想当然地搞混）。
    println!(
        "Formatter<'_> 里 '_ 与 &self 无关：{}",
        describe(Excerpt { part: &s })
    );
}

// ---------------------------------------------------------------------------
// ④ 'static
// ---------------------------------------------------------------------------

/// `T: 'static` 读作「**T 里不含短命的借用**」，而不是「T 在运行时永远活着」。
/// 拥有所有权的类型（String、Vec、i32…）天然满足，跟它们何时被 drop 无关。
fn to_any<T: 'static>(value: T) -> Box<dyn Any> {
    Box::new(value)
}

/// ④ 演示：`'static` 的两种含义。
fn section_static() {
    println!("\n== ④ 'static 不只是「永远活着」 ==");

    // 含义 1：数据被写进二进制，程序期间一直有效 —— 字面量天生如此
    let lit: &'static str = "编译期就定好的字面量";
    println!("字面量                = {lit:?}");

    // 含义 2：作为 trait bound 时表示「不含短命借用」，String 满足
    let owned = String::from("owned");
    let any = to_any(owned);
    println!(
        "String 满足 T: 'static = {}",
        any.downcast_ref::<String>().unwrap()
    );

    // 反过来，借用局部变量的 &str 不满足 'static：
    // let local = String::from("local");
    // to_any(local.as_str());  // error[E0597]: `local` does not live long enough

    // 真想让运行时产物的引用活到程序结束，可以主动泄漏（内存永不回收，慎用）
    let leaked: &'static str = Box::leak(String::from("leak").into_boxed_str());
    println!("Box::leak 造出的      = {leaked:?}");
}

fn main() {
    println!("生命周期只存在于编译期，运行时看不到它的任何痕迹\n");
    section_fn_signatures();
    section_struct_with_ref();
    section_placeholder_lifetime();
    section_static();
}
