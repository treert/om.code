//! 过程宏（proc macro）实验：三种过程宏各来一个，**全部只用 std 的 `proc_macro`**，
//! 不依赖 `syn` / `quote`，也不需要联网。
//!
//! 为什么这个东西不能像别的实验一样躺在 `examples/*.rs` 里：
//!
//! 1. 过程宏是「编译期执行的程序」，编译器要求它独立成一个 `proc-macro = true` 的 crate；
//! 2. 更要命的是**定义它的 crate 自己用不了它**（自举限制）—— 宏的定义就是那个 crate 本身。
//!
//! 所以这里拆成两个 crate：本 crate 只定义，隔壁 `use_pm` 负责消费。
//! 在仓库根目录 `rust/` 下运行：`cargo run -p use_pm`。
//!
//! 三个宏分别对应三种形态（也是编译器给三种不同的入口签名）：
//!
//! | 宏 | 签名 | 本例里的活儿 |
//! | --- | --- | --- |
//! | `calc!(...)` | `TokenStream -> TokenStream` | **手写带优先级的中缀解析器**（声明宏做不到的那部分） |
//! | `#[timed]` | `(attr, item) -> TokenStream` | 把函数体原样搬进闭包，前后插计时 |
//! | `#[derive(Describe)]` | `item -> TokenStream` | 扫出结构体字段名，生成 `impl` |

use proc_macro::{Delimiter, Group, Span, TokenStream, TokenTree};

// ---------------------------------------------------------------------------
// ① 函数式过程宏：calc!(1 + 2 * 3)
// ---------------------------------------------------------------------------
//
// 这是「为什么需要过程宏」最直白的答案。声明宏（examples/macro_ast.rs）命中即停、
// 不回溯，`1 + 2 * 3` 这种带优先级的中缀输入根本没法正确解析。
// 而过程宏里就是**普通的 Rust 代码** —— 递归下降、算优先、随便写。

/// 把 `calc!(1 + 2 * 3)` 解析成一棵带优先级的树，再展开成一个 `i64` 表达式。
///
/// 生成的表达式是**完全括号化**的，所以「优先级有没有生效」看编译期打印的
/// `[pm_lab] calc 解析出: ...` 一眼就知道。
#[proc_macro]
pub fn calc(input: TokenStream) -> TokenStream {
    let mut p = Parser {
        toks: input.into_iter().collect(),
        pos: 0,
    };
    match p.parse_expr(0) {
        // 解析完必须把输入吃干净，否则 `1 + 2 3` 这种会被静默吞掉
        Ok(expr) if p.pos == p.toks.len() => {
            eprintln!("[pm_lab] calc 解析出: {expr}");
            expr.parse().expect("生成的 token 一定合法")
        }
        Ok(_) => error_at(p.toks.last(), "表达式解析完了，后面还有多余的 token"),
        Err(e) => error_at(e.tok.as_ref(), &e.msg),
    }
}

struct Parser {
    toks: Vec<TokenTree>,
    pos: usize,
}

struct ParseError {
    /// 出错位置：**留着这个 token 就是为了拿它的 `Span`**
    tok: Option<TokenTree>,
    msg: String,
}

impl Parser {
    fn peek(&self) -> Option<&TokenTree> {
        self.toks.get(self.pos)
    }

    /// 中缀解析（Pratt 写法）：`min_bp` = 「当前这一层允许吞下的最小绑定力」。
    ///
    /// 优先级就靠它：`*` 的绑定力是 2，`+` 是 1，所以解析 `1 + 2 * 3` 时，
    /// 右边的 `2` 看到 `*` 的 2 ≥ 1，会继续把 `2 * 3` 搂进去，于是得到 `(1 + (2 * 3))`。
    fn parse_expr(&mut self, min_bp: u8) -> Result<String, ParseError> {
        let mut lhs = self.parse_atom()?;
        while let Some((op, bp)) = self.peek_op() {
            if bp < min_bp {
                break;
            }
            self.pos += 1; // 吃掉运算符
            // 右边用 bp + 1 起步 → 同优先级左结合：10 - 4 - 3 解析成 ((10 - 4) - 3)
            let rhs = self.parse_expr(bp + 1)?;
            lhs = format!("({lhs} {op} {rhs})");
        }
        Ok(lhs)
    }

    /// 看一个 token 就能定优先级 —— 这里正好说明「过程宏能自由 lookahead」，
    /// 而声明宏只能按写死的顺序去试。
    fn peek_op(&self) -> Option<(&'static str, u8)> {
        match self.peek()? {
            TokenTree::Punct(p) => match p.as_char() {
                '+' => Some(("+", 1)),
                '-' => Some(("-", 1)),
                '*' => Some(("*", 2)),
                '/' => Some(("/", 2)),
                _ => None,
            },
            _ => None,
        }
    }

    /// 最底层：一个数字字面量，或者一个括号子表达式（递归下降）。
    fn parse_atom(&mut self) -> Result<String, ParseError> {
        // 先把 token 拷出来（TokenTree: Clone），免得「读 self.toks」和「改 self.pos」
        // 在同一个 match 里被借用检查拦住
        match self.peek().cloned() {
            // 注意：括号组对 token 流来说只是**一个** TokenTree，
            // 所以这里要把它的 stream 取出来，用一个新的 Parser 递归解析。
            Some(TokenTree::Group(g)) if g.delimiter() == Delimiter::Parenthesis => {
                let mut sub = Parser {
                    toks: g.stream().into_iter().collect(),
                    pos: 0,
                };
                let e = sub.parse_expr(0)?;
                if sub.pos != sub.toks.len() {
                    return Err(ParseError {
                        tok: self.toks.get(self.pos).cloned(),
                        msg: "括号里的表达式没解析完".into(),
                    });
                }
                self.pos += 1;
                Ok(format!("({e})"))
            }
            Some(TokenTree::Literal(l)) => {
                let lit = l.to_string();
                self.pos += 1;
                Ok(lit)
            }
            other => Err(ParseError {
                tok: other,
                msg: "这里需要一个数字，或者一个括号子表达式".into(),
            }),
        }
    }
}

/// 造一个 `compile_error!`，并**把 span 改到出错的那个 token 上**。
///
/// 这几行就是「过程宏报错能精确指到某个 token」的全部秘密：
/// 生成 `compile_error!(...)` 的 token 流 -> 逐个 `set_span(出错的 span)`。
/// 声明宏做不到这件事（`compile_error!` 在 macro_rules 里只能定位到整个调用）。
fn error_at(tok: Option<&TokenTree>, msg: &str) -> TokenStream {
    let span = tok.map(TokenTree::span).unwrap_or_else(Span::call_site);
    let ts: TokenStream = format!("compile_error!({msg:?})")
        .parse()
        .expect("compile_error! 的调用形式一定合法");
    ts.into_iter()
        .map(|mut t| {
            t.set_span(span);
            t
        })
        .collect()
}

// ---------------------------------------------------------------------------
// ② 属性宏：#[timed] / #[timed("标签")]
// ---------------------------------------------------------------------------
//
// 属性宏比函数式宏多收到一个 `attr`（属性括号里的东西），并且**入参是被标注的那个 item**。
// 下面这个宏干的事很典型：拆开「签名」和「函数体」，中间/两边插代码。

/// 给函数加计时。`#[timed]` 用函数名当标签，`#[timed("手写循环")]` 用自定义标签。
///
/// 生成的结构是：
///
/// ```text
/// <原签名> {
///     let __pm_t0 = std::time::Instant::now();
///     let __pm_ret = (|| { <原函数体> })();
///     eprintln!("[timed] <标签> 用时 {:?}", __pm_t0.elapsed());
///     __pm_ret
/// }
/// ```
///
/// 用闭包包住函数体而不是直接拼接，是为了让函数体里的 `return` / `?` 仍然按原语义工作。
/// 代价是 **`async fn` 贴不了**（`await` 会落在一个非 async 的闭包里）。
///
/// `__pm_` 前缀不是洁癖：过程宏默认**完全不卫生**（生成的 token 拿的是调用处的 span），
/// 想不撞名只能自己起足够古怪的名字，或者去动 span（`Span::def_site` 还在 nightly 后面）。
#[proc_macro_attribute]
pub fn timed(attr: TokenStream, item: TokenStream) -> TokenStream {
    let toks: Vec<TokenTree> = item.into_iter().collect();

    // 找函数体：第一个 `{...}` 组。它之前全是签名，之后（正常情况）什么都没有。
    let Some(idx) = toks
        .iter()
        .position(|t| matches!(t, TokenTree::Group(g) if g.delimiter() == Delimiter::Brace))
    else {
        return error_at(toks.first(), "#[timed] 只能贴在带函数体的 fn 上");
    };

    // 函数名：`fn` 后面那个 ident（这里只够用，真要处理泛型/`where` 还得上 syn）
    let mut name = String::from("fn");
    let mut expect_name = false;
    for t in &toks[..idx] {
        match t {
            TokenTree::Ident(i) if expect_name => {
                name = i.to_string();
                break;
            }
            TokenTree::Ident(i) => expect_name = i.to_string() == "fn",
            _ => expect_name = false,
        }
    }

    // 标签：`#[timed("x")]` 里的那个字符串字面量（去掉引号），否则退回函数名
    let label = match attr.into_iter().next() {
        None => name.clone(),
        Some(TokenTree::Literal(l)) => l.to_string().trim_matches('"').to_string(),
        Some(other) => other.to_string(),
    };
    eprintln!("[pm_lab] timed 包住了 fn {name}，标签 = {label}");

    // 拼接输出：签名原样保留，只把函数体那段 token **整体搬进**闭包
    //（搬的是 token 本身，不是字符串，所以函数体里的报错位置还是准的）
    let body = match &toks[idx] {
        TokenTree::Group(g) => g.stream(),
        _ => unreachable!("上面刚断言过它是个 brace 组"),
    };

    // 注意：TokenStream::from_str 要求括号**成对**，所以不能写
    // `"... let __pm_ret = (||".parse()` 这种「半截」模板（实测会 panic：
    // LexError("this file contains an unclosed delimiter")）。
    // 这些半截的壳子只能用 Group::new 手工搭 —— 正好也是「过程宏就是在拼 token 树」的实操。
    let mut closure: TokenStream = "||".parse().expect("两个竖线，合法");
    closure.extend(std::iter::once(TokenTree::Group(Group::new(
        Delimiter::Brace,
        body,
    )))); // `|| { 原函数体 }`

    let mut block: TokenStream = "let __pm_t0 = std::time::Instant::now(); let __pm_ret ="
        .parse()
        .expect("模板片段合法");
    block.extend(std::iter::once(TokenTree::Group(Group::new(
        Delimiter::Parenthesis,
        closure,
    )))); // `(|| { ... }`
    block.extend(std::iter::once(TokenTree::Group(Group::new(
        Delimiter::Parenthesis,
        TokenStream::new(),
    )))); // `()` —— 调用那个闭包
    block.extend(
        format!("; eprintln!(\"[timed] {label} 用时 {{:?}}\", __pm_t0.elapsed()); __pm_ret")
            .parse::<TokenStream>()
            .expect("模板里只插了一个标签，一定合法"),
    );

    let mut out: TokenStream = toks[..idx].iter().cloned().collect();
    out.extend(std::iter::once(TokenTree::Group(Group::new(
        Delimiter::Brace,
        block,
    ))));
    out
}

// ---------------------------------------------------------------------------
// ③ 派生宏：#[derive(Describe)] + 字段上的 #[describe(skip)]
// ---------------------------------------------------------------------------
//
// 派生宏【只能】收到被派生的那个 item 的 token（拿不到别的上下文），
// 而且它只负责生成 `impl` —— **trait 本身由使用方提供**（serde 就是这个套路）。

/// 为结构体生成「字段名清单」；字段上写 `#[describe(skip)]` 可跳过。
///
/// 第二参数 `attributes(describe)` 是必须的：不声明的话，编译器会把
/// `#[describe(skip)]` 当成未注册的属性直接报错。
#[proc_macro_derive(Describe, attributes(describe))]
pub fn describe(input: TokenStream) -> TokenStream {
    let toks: Vec<TokenTree> = input.into_iter().collect();

    // 结构体名：`struct` 后面的 ident
    let mut name = None;
    let mut expect_name = false;
    for t in &toks {
        match t {
            TokenTree::Ident(i) if expect_name => {
                name = Some(i.to_string());
                break;
            }
            TokenTree::Ident(i) => expect_name = i.to_string() == "struct",
            _ => expect_name = false,
        }
    }
    let Some(name) = name else {
        return error_at(toks.first(), "Describe 目前只支持 struct");
    };

    // 字段列表：第一个 `{...}` 组，按顶层逗号切开
    //（组对 token 流是原子的，所以按逗号切不会切到 `HashMap<K, V>` 里面去）
    let Some(fields) = toks.iter().find_map(|t| match t {
        TokenTree::Group(g) if g.delimiter() == Delimiter::Brace => Some(g.stream()),
        _ => None,
    }) else {
        return error_at(toks.first(), "Describe 需要带 `{ ... }` 字段列表的 struct");
    };

    let mut names = Vec::new();
    for field in split_top_level(fields, ',') {
        // 辅助属性 `#[describe(skip)]`：这里只做「里面出现没出现 skip」的粗糙判断，
        // 这正是手写 token 扫描的天花板 —— 真正要读属性参数就得请 syn 出场
        let skipped = field.iter().any(|t| match t {
            TokenTree::Group(g) if g.delimiter() == Delimiter::Bracket => {
                g.to_string().contains("skip")
            }
            _ => false,
        });
        if skipped {
            continue;
        }
        if let Some(n) = field_name(&field) {
            names.push(n);
        }
    }
    eprintln!("[pm_lab] describe 作用在 struct {name}，字段 {names:?}");

    let list = names
        .iter()
        .map(|n| format!("{n:?}"))
        .collect::<Vec<_>>()
        .join(", ");
    // 生成代码里的 `Describe` 走**调用处**的名字解析，所以 use_pm 里得先导入这个 trait
    format!(
        "impl Describe for {name} {{
            fn describe() -> &'static [&'static str] {{ &[{list}] }}
        }}"
    )
    .parse()
    .expect("生成的 impl 一定合法")
}

/// 按**顶层**分隔符切分 token，括号组内部的逗号不算
fn split_top_level(ts: TokenStream, sep: char) -> Vec<Vec<TokenTree>> {
    let mut out = vec![Vec::new()];
    for t in ts {
        if matches!(&t, TokenTree::Punct(p) if p.as_char() == sep) {
            out.push(Vec::new());
        } else {
            out.last_mut().expect("至少有一组").push(t);
        }
    }
    out.retain(|g| !g.is_empty()); // 尾随逗号会产生一个空组
    out
}

/// 从一段字段 token 里挑出字段名：跳过 `#[...]` 属性、跳过 `pub` / `pub(crate)`
fn field_name(chunk: &[TokenTree]) -> Option<String> {
    let mut i = 0;
    while i < chunk.len() {
        match &chunk[i] {
            TokenTree::Ident(id) if id.to_string() == "pub" => {
                i += 1;
                // `pub(crate)` / `pub(in path)` 后面还有一个括号组
                //（`if let ... && ...` 这种 let chain 是 edition 2024 才稳定的）
                if let Some(TokenTree::Group(g)) = chunk.get(i)
                    && g.delimiter() == Delimiter::Parenthesis
                {
                    i += 1;
                }
            }
            TokenTree::Ident(id) => return Some(id.to_string()),
            _ => i += 1, // `#`、属性组、以及别的噪音
        }
    }
    None
}
