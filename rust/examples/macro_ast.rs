//! 用 `macro_rules!` 的**递归展开**，在编译期搭出一棵 DSL 语法树。
//!
//! 运行：`cargo run --example macro_ast`
//!
//! 想验证的三件事：
//!
//! 1. 宏的「递归」不是函数递归，而是**宏自己再展开一次自己**。终止条件是：
//!    某条**不再自调用**的规则被匹配上（`func_macro.rs` 里的 `(@count) => { 0usize }` 就是这个基）。
//! 2. 「递归 + `tt` 拆分」确实能完成一次递归下降式的解析，并长出**嵌套**的树 ——
//!    树有多深，看宏展开了多少次，而不是运行时循环了多少次。
//! 3. 边界同样明确：`macro_rules!` 只会「按顺序匹配、命中即停、不回溯」，
//!    而且**没有**「返回 (节点, 剩余 token)」这种能力（宏的产物只能是 token，不能是元组），
//!    所以真正需要优先级 / 回溯 / 好报错的 DSL，得换过程宏（`docs/macro.md` §4）。

use std::fmt;

/// DSL 的「语法树」。
///
/// 注意：解析发生在**编译期**，这棵树的**形状**由宏展开时的 token 结构决定；
/// 但 `Vec` / `String` 是在**运行时**建出来的（宏只能吐 token，吐不出值来）。
#[derive(Debug, PartialEq)]
enum Node {
    Num(i64),
    Sym(String),
    /// `op` 是 `&'static str`：运算符来自 `stringify!`，编译期就定了，无需堆分配
    Call {
        op: &'static str,
        args: Vec<Node>,
    },
}

impl fmt::Display for Node {
    /// 按 s-expression 渲染：`(+ 1 (* 2 3))`，方便和输入肉眼对照
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Node::Num(n) => write!(f, "{n}"),
            Node::Sym(s) => write!(f, "{s}"),
            Node::Call { op, args } => {
                write!(f, "({op}")?;
                for a in args {
                    write!(f, " {a}")?;
                }
                write!(f, ")")
            }
        }
    }
}

/// 树在运行时是真的数据，可以正常遍历求值
fn eval(n: &Node) -> i64 {
    match n {
        Node::Num(v) => *v,
        Node::Sym(_) => 0, // 标识符当变量看，这里一律当 0
        Node::Call { op, args } => match *op {
            "+" => args.iter().map(eval).sum(),
            "*" => args.iter().map(eval).product(),
            other => panic!("eval 不认识这个运算符: {other}"),
        },
    }
}

// ---------------------------------------------------------------------------
// 宏 ①：前缀式 DSL，直接长出嵌套的树
// ---------------------------------------------------------------------------
//
// 关键手法两条：
//
// - **`@` 内部规则**（前缀 token `@` 正常调用不会出现，见 docs/macro.md §1.4）；
// - **规则顺序即语义**：所有 `@` 规则必须排在最后的入口兜底规则之前。
//   否则 `dsl!(@group ...)` 这种内部调用会被 `($($t:tt)*)` 接走，无限递归 ——
//   这正是 §1.4「首个匹配胜出、兜底放错位置不报错」的实战版。
macro_rules! dsl {
    // ---- 叶子：裸 token（不带括号）----
    (@node $n:literal) => { Node::Num($n) };
    (@node $s:ident) => { Node::Sym(stringify!($s).to_string()) };

    // ---- 括号组：先把外层括号剥掉，再重新按「组内」的规则匹配 ----
    // 之所以要分两步，是因为一个括号组对宏来说只是**一个** tt，
    // 想看里面就得先把它整个匹配下来，再用 `$($inner:tt)*` 摊开。
    (@node ($($inner:tt)*)) => { dsl!(@group $($inner)*) };

    // ---- 组内只有一个 token → 还是叶子 ----
    (@group $n:literal) => { Node::Num($n) };
    (@group $s:ident) => { Node::Sym(stringify!($s).to_string()) };

    // ---- 组内 ≥2 个 token → 第一个是运算符，其余**每个 tt 递归成一个子节点** ----
    // `$( dsl!(@node $arg) ),*` 就是「对剩余 token 逐个递归」：
    // 子节点个数完全由括号里写了几个 tt 决定 —— `(+ a b c)` 的三个参数就是三个子节点。
    (@group $op:tt $($arg:tt)*) => {
        Node::Call {
            op: stringify!($op),
            args: vec![ $( dsl!(@node $arg) ),* ],
        }
    };

    // ---- 入口兜底：**必须最后** ----
    ($($t:tt)*) => { dsl!(@group $($t)*) };
}
//
// 顺带暴露一个 `macro_rules!` 的软肋：上面 `$op:tt` 只要求「一个 token」，
// 并不要求它是运算符。所以错的输入不会报错，只会长出怪树（实测）：
//
//   dsl!(1 2)  ->  Call { op: "1", args: [Num(2)] }      // 数字被当成了运算符
//
// 想校验「运算符必须是符号」，函数式声明宏做不到：`tt` 不区分 token 的种类，
// 能细分的碎片说明符里也没有「标点」。这活儿得交给过程宏。

// ---------------------------------------------------------------------------
// 宏 ②：从左到右「munch」，用累加器拼出左结合的树
// ---------------------------------------------------------------------------
//
// 这个是 `func_macro.rs` 里 `@count` 的加强版：`@count` 把 1 一直加下去，
// 这里把「已经建好的左子树」当累加器一路带着走。
//
// 为什么非要累加器？因为宏**没法返回两个东西**：理想中的递归下降要写成
// 「parse 一个节点，再拿到剩下的 token」，但宏的产物是一段 token，
// 拿不回「剩下多少」。于是只能反过来：**剩余 token 当参数往下传，
// 已建好的（左）子树塞在 `[...]` 里一起传**，这就是经典的 accumulator 手法。
macro_rules! add_chain {
    // 剩余 token 吃完 → 累加器即结果
    (@munch [$acc:expr]) => { $acc };

    // 第一步：累加器还空着，收下最左边的数字
    (@munch [] $n:literal $($rest:tt)*) => {
        add_chain!(@munch [ Node::Num($n) ] $($rest)*)
    };

    // 递归步：吃掉 `+ <数字>`，把「已有左子树」包成新节点，带着它继续
    // （能塞进 `[$acc:expr]` 是因为 `expr` 碎片允许任意表达式，
    //   所以已经长出来的子树可以整体当作一个表达式再传进宏）
    (@munch [$acc:expr] + $n:literal $($rest:tt)*) => {
        add_chain!(@munch [
            Node::Call { op: "+", args: vec![$acc, Node::Num($n)] }
        ] $($rest)*)
    };

    // 入口兜底：同样必须最后
    ($($t:tt)*) => { add_chain!(@munch [] $($t)*) };
}
//
// 这条路的极限也在这里：一旦匹配了 `+`，选择就**定死了**，没有回溯的可能。
// 所以 `add_chain!(1 + 2 * 3)` 不会解析成 `1 + (2 * 3)`。
// 而它失败的样子很反直觉 —— 实测（rustc 1.96.0）不是「没有规则匹配」，而是：
//
//   error: recursion limit reached while expanding `add_chain!`
//     |  ($($t:tt)*) => { add_chain!(@munch [] $($t)*) };
//     |                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//     |  let b = add_chain!(1 + 2 * 3);
//     |          --------------------- in this macro invocation
//
// 原因：`@munch [Node::Num(1)] * 3` 三条 `@` 规则全匹配不上，
// 于是被最后那条入口兜底 `($($t:tt)*)` 接走，又包了一层 `@munch [] ...`，
// 每转一圈多套一层、永远卡在同一个位置，直到撞上 `recursion_limit` ——
// 兜底分支不仅会「静默改变语义」，还会把「无规则匹配」变成「无限递归」。
// （想让它干脆地报「这里不该出现 `*`」，macro_rules 也做不到：没有否定匹配、
//   `compile_error!` 也没法把 span 定位到那个 token 上。）
// 优先级、结合性、报错信息、IDE 补全 —— 这些统统是过程宏 + `syn` 的活儿。

fn main() {
    println!("== ① 嵌套：一棵真正的树 ==");
    // 解析发生在编译期；下面三行打印的是同一棵运行时树
    let t = dsl!(+ 1 (* 2 3));
    println!("输入    : (+ 1 (* 2 3))");
    println!("显示    : {t}");
    println!("Debug   : {t:?}");
    println!("求值    : {}", eval(&t));
    println!(
        "顶层子节点数 = {} （括号里写了 1 和 (* 2 3)，两个 tt → 两个子节点）",
        match &t {
            Node::Call { args, .. } => args.len(),
            _ => 0,
        }
    );

    println!("\n== ② token 分类：ident 收成 Sym，arity 由书写决定 ==");
    let t2 = dsl!(and (> x 0) (< x 10));
    println!("显示    : {t2}");

    println!("\n== ③ 递归的基：`$( ... )*` 允许 0 次，`@group $op:tt` 也允许零个参数 ==");
    let t3 = dsl!(+);
    println!("dsl!(+) 显示 : {t3}   →  求值 {}", eval(&t3));
    let t4 = dsl!(sum);
    println!("dsl!(sum)    : {t4}");

    println!("\n== ④ 左结合：也是递归，只是把左子树当累加器 ==");
    let t5 = add_chain!(1 + 2 + 3 + 4);
    println!("输入    : 1 + 2 + 3 + 4");
    println!("显示    : {t5}");
    println!("求值    : {}", eval(&t5));

    println!("\n== ⑤ 树是运行时真数据，可以随手断言 ==");
    assert_eq!(
        t,
        Node::Call {
            op: "+",
            args: vec![
                Node::Num(1),
                Node::Call {
                    op: "*",
                    args: vec![Node::Num(2), Node::Num(3)],
                },
            ],
        }
    );
    println!("assert_eq! 通过：这棵树就是手写出来那棵树");

    println!("\n补充：递归层数吃的是编译期预算 `recursion_limit`（默认 128），");
    println!(
        "      嵌套写深了会报 `recursion limit reached while expanding ...`，见 docs/macro.md §1.5。"
    );
}
