//! 函数式宏（function-like macro）示例。
//!
//! 调用形式长得像函数：`sql!(...)`，但它的参数是「任意 token」——
//! 不需要是合法的 Rust 表达式，也不需要满足任何函数签名。
//! 这正是函数式宏区别于普通函数的关键：它在**语法层**工作，而不是类型层。
//!
//! 运行：`cargo run --example func_macro`
//!
//! 宏的「卫生性」（哪些名字会被重命名、`$crate` 解决什么）见 `examples/hygiene.rs`，
//! 过程宏（proc macro）见 `docs/macro.md`。

use std::fmt;

/// 宏「解析」出来的 SQL 语句。
#[derive(Debug)]
struct Sql {
    kind: &'static str,
    text: String,
    /// 编译期就数好的 token 个数，运行时零开销
    token_count: usize,
}

impl fmt::Display for Sql {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{:<6} | {:<46} | {:>2} tokens",
            self.kind, self.text, self.token_count
        )
    }
}

/// 把 token 流拼出来的裸字符串稍微美化一下空格。
fn tidy(raw: &str) -> String {
    raw.replace(" ,", ",")
        .replace("( ", "(")
        .replace(" )", ")")
        .replace(" . ", ".")
}

// ---------------------------------------------------------------------------
// 核心：一个函数式宏
// ---------------------------------------------------------------------------
//
// 参数写法 `$($tokens:tt)*` 的含义：
//   tt   —— token tree，可以是任何单个「记号」（标识符、字面量、标点、括号组…）
//   $( )* —— 重复 0 次或多次
// 所以这个宏能吞下「任意」输入，哪怕它根本不是合法的 Rust 表达式。
//
// macro_rules! 的规则按书写顺序尝试，所以内部辅助分支必须排在兜底分支之前。
macro_rules! sql {
    // ① 形状：以 SELECT 开头
    (SELECT $($rest:tt)*) => {
        Sql {
            kind: "SELECT",
            text: tidy(stringify!($($rest)*)),
            token_count: sql!(@count $($rest)*),
        }
    };

    // ② 形状：INSERT INTO <标识符> VALUES <任意 token>
    //    这里用 `$table:ident` 要求表名必须是标识符，宏可以对 token 分类做校验
    (INSERT INTO $table:ident VALUES $($vals:tt)*) => {
        Sql {
            kind: "INSERT",
            text: tidy(concat!(
                stringify!($table), " VALUES ", stringify!($($vals)*)
            )),
            token_count: 2 + sql!(@count $($vals)*),
        }
    };

    // ③ 内部辅助规则：递归地数 token 个数（编译期展开，会被常量折叠）
    (@count) => { 0usize };
    (@count $head:tt $($tail:tt)*) => { 1usize + sql!(@count $($tail)*) };

    // ④ 兜底：什么 token 都收
    ($($tokens:tt)*) => {
        Sql {
            kind: "RAW",
            text: tidy(stringify!($($tokens)*)),
            token_count: sql!(@count $($tokens)*),
        }
    };
}

fn main() {
    println!("{:<6} | {:<46} | count", "kind", "text");
    println!("{}", "-".repeat(74));

    // ① 你举的那个例子：`SELECT * FROM users` 不是合法的 Rust 表达式，
    //    但宏在解析阶段把它们统统当 token 收下，所以照样能编译。
    println!("{}", sql!(SELECT * FROM users));

    // ② SELECT 分支剥掉关键字，剩下的原文原样保留（`,` `>` 都只是普通 token）
    println!("{}", sql!(SELECT id, name FROM users WHERE age > 18));

    // ③ INSERT 分支额外要求表名是 ident，其余仍是任意 token
    //    注意 count = 3：`(1, "Ada", 36)` 整个括号组只算 **一个** token tree，
    //    所以 token 个数 ≠ 肉眼看到的单词个数
    println!("{}", sql!(INSERT INTO users VALUES (1, "Ada", 36)));

    // ④ 兜底分支：哪怕根本不是 SQL、甚至不是合法 Rust，也能收下
    println!("{}", sql!(this is @ not even valid Rust !!!));

    // ⑤ token 个数由宏在编译期递归算出：a  ,  b  ,  c  FROM  t
    let q = sql!(SELECT a, b, c FROM t);
    assert_eq!(q.token_count, 7);
    println!("\n编译期算出的 token_count = {}", q.token_count);

    // 额外说明：`vec!`、`println!`、`format!` 本身就是函数式宏，
    // 它们和这里的 sql! 是同一类东西，只是标准库替你写好了。
    let v = vec![1, 2, 3];
    println!("vec! 也是函数式宏: {v:?}");
}
