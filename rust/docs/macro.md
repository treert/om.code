# 宏（macro）—— 编译期的 token 重写

> 配套可运行实验：[`examples/func_macro.rs`](../examples/func_macro.rs)（token 树匹配 + 递归展开）、
> [`examples/hygiene.rs`](../examples/hygiene.rs)（卫生性：局部变量 / 标签 / 条目名 / `$crate`）。
> 过程宏没有配套 example，原因见 [§4.1](#41-为什么-examples-里放不了过程宏)；最小配方已在本机实测跑通（[§4.2](#42-最小可跑配方实测)）。
> 本文所有报错原文与输出均在 rustc / cargo **1.96.0** 上实测，命令见文末「本机实测」。

## 定位：宏在编译流程的哪一步

```text
源码 ──lexer──▶ token 流 ──parser──▶ AST ──▶ ★宏展开★ ──▶ 名字解析 ──▶ 类型检查 ──▶ MIR ──▶ 代码生成
```

三句话定性：

1. 宏是**语法层**的重写器，不是函数：吃进去的是 token，吐出来的还是 token，运行时不留任何痕迹。
2. 所以 `sql!(SELECT * FROM users)` 能编译 —— 在那个阶段 `*`、`FROM` 只是普通 token，
   还没轮到「`*` 是不是解引用」「`FROM` 是不是未定义标识符」这类语义问题（`examples/func_macro.rs` ①）。
3. 「宏有运行时开销」只可能指**展开结果本身**的开销。`func_macro.rs` 里的 `token_count` 是由
   递归展开出的常量加法，会被常量折叠成一个立即数，等于白送。

两大类宏，实现机制完全不同：

| | 声明宏 `macro_rules!` | 过程宏 proc macro |
| --- | --- | --- |
| 谁来做重写 | **编译器内置**的模式匹配引擎 | **你自己写**的编译期程序 |
| 定义位置 | 代码里直接写（有作用域规则，见 §1.6） | 独立的 `proc-macro = true` crate |
| 能力边界 | 结构化匹配 + 替换，不能做任意计算 | 任意计算，可真正 parse 成 AST |
| 外观 | 只有 `name!(...)` 一种 | `name!(...)` / `#[attr]` / `#[derive(X)]` 三种 |
| 卫生性 | 部分卫生（§2） | 默认**完全不卫生**，靠 `Span` 控制（§4.3） |

---

## 1. 声明宏：匹配器就是「token 树」结构匹配

### 1.1 `tt` 与括号组：为什么宏能吞下「不是合法 Rust」的东西

`tt` = token tree，是宏系统里最小的结构单位：

- 单个标识符 / 字面量 / 标点 → 一个 `tt`；
- **一整个括号组**（`(...)` / `[...]` / `{...}`）连同里面的内容 → 也只算**一个** `tt`。

`examples/func_macro.rs` 的输出就是这条规则的直接证据（`cargo run --example func_macro`）：

```text
INSERT | users VALUES (1, "Ada", 36) | 3 tokens
```

`(1, "Ada", 36)` 肉眼看是 5 个东西，对宏来说只有 **1 个** `tt`；另外 2 个来自分支 ② 在
`$($vals:tt)*` 之前就吃掉并写死的 `$table` 与 `VALUES`。所以：

> **token 个数 ≠ 肉眼看到的单词个数。** 数 token 时必须按「括号组折叠」的口径数。

同理，`SELECT a, b, c FROM t` 的 rest 部分是 `a , b , c FROM t` → 7 个 token，
正好等于 `func_macro.rs` ⑤ 里 `assert_eq!(q.token_count, 7)` 的期望值。

`tt` 是唯一「不挑输入」的碎片分类，这也是函数式宏能接收任意 token 的根本原因。

### 1.2 fragment specifier：对 token 做「分类校验」

写法是 `$<名字>:<说明符>`，先把这两半分清楚（探针 n1 测名字、n2 测说明符，均 `rustc --edition 2024` 单独编译）：

- **说明符**（冒号右边）是**关键字**：只能用下表那几种，自己起名写别的直接
  `error: invalid fragment specifier \`x\``（探针 n2；rustc 的 help 会列出完整清单，并额外提到
  `expr_2021` / `pat_param` 两个 edition 兼容写法）；
- **名字**（冒号左边）**随便起**：它只是这条规则内部的局部变量名，把 `$rest` 改成 `$x` 毫无影响；
- 唯一的硬约束是**两处同名**（连重复层级都要一致），只改一边分别报：

```text
# 展开处用了 matcher 里不存在的名字
error: attempted to repeat an expression containing no syntax variables matched as repeating at this depth
  --> probe.rs:2:37
  |     ($($rest:tt)*) => { stringify!($($x)*) };
  |                                       expected a repeatable metavariable: `$rest`

# 名字对了，但展开处漏写了 $( )*
error: variable `x` is still repeating at this depth
  --> probe.rs:5:33
  |     ($($x:tt)*) => { stringify!($x) };
```

`examples/func_macro.rs` 里一个宏内部就并存 `rest` / `table` / `vals` / `head` / `tail` / `tokens`
六个名字，各自也都在展开处同名出现 —— 这就是「名字随意」的现成证据。

| 分类 | 接受 | 典型用途 |
| --- | --- | --- |
| `ident` | 一个标识符 | 表名、变量名（`func_macro.rs` ② 的 `$table:ident`） |
| `expr` | 一个合法表达式 | `vec!` 的元素、`println!` 的参数 |
| `ty` / `path` / `pat` / `stmt` / `item` / `block` / `literal` / `vis` / `lifetime` / `meta` | 对应语法结构 | 各类语法糖宏 |
| `tt` | 不分类，万能 | 兜底、内部辅助规则 |

分类不匹配时，这条规则**直接不参与匹配**（会继续往下试别的规则，全试完才报
`no rules expected`，见 §5）。所以 `sql!(INSERT INTO 1 VALUES x)` 不会报错，而是掉进兜底分支。

**关键机制：`expr` 捕获到的东西是「一个不透明的整体」。** 实测（探针 t2）：

```rust
macro_rules! n_trees {
    ()                              => { 0usize };
    ($head:tt $($tail:tt)*)         => { 1usize + n_trees!($($tail)*) };
}
macro_rules! outer { ($e:expr) => { n_trees!($e) } }

outer!(1 + 2 * 3)   // 实测输出 1 —— 整个表达式作为【一个】tt 交给下游
```

也就是说：`$e:expr` 之后你**拿不回**它内部那 5 个 token，它在后续匹配中就是一块整体。
想让下游能拆开细细处理，从一开始就必须用 `tt` 收。

### 1.3 重复：`$( ... )*` / `+` / `?`，以及「zip 展开」

- `$( $x:tt )*`、`+`、`?` 分别表示 0+ / 1+ / 0 或 1 次；
- 分隔符由 **matcher** 写定：`$( $x:expr ),*` 是逗号分隔，把 `,` 改成 `;` 就是分号分隔
  （这就是 `vec![1, 2, 3]` 与 `vec![0; 10]` 能共存于同一个宏的原因）；
- 转录时多个重复组**按位置 zip**，长度对不上直接编译错误。

### 1.4 规则顺序：自上而下，**首个匹配胜出**

宏的规则表是一个有序列表，命中即停，不存在「最优匹配」。实测（探针 o2）：

```rust
macro_rules! which {
    ($($t:tt)*)         => { "general" };   // 宽泛的写在前面
    (SELECT $($t:tt)*)  => { "select"  };
}
which!(SELECT a)        // 实测输出 general
```

而且 rustc 1.96.0 **不会**对「永远用不到的第二条规则」报任何警告 —— 兜底分支放错位置是
**静默**的坑。这正是 `func_macro.rs` 把 `($($tokens:tt)*)` 兜底放在最后、并且把
`@count` 内部规则也排在它前面的原因。

配套惯例是「内部规则」加一个正常调用绝不会出现的前缀 token（`@`）：

```rust
sql!(@count $($tail)*)   // 只有内部辅助分支会匹配 @count …，用户调用不会误伤
```

### 1.5 递归展开与 `recursion_limit`

宏体里可以调用自身，形成递归展开。实测边界（探针 lim*）：

| 场景 | 结果 |
| --- | --- |
| `let n = count!(127 个 token);`（不套在别的宏里） | ✅ 通过 |
| `let n = count!(128 个 token);` | ❌ `recursion limit reached while expanding \`count!\`` |
| `println!("{}", count!(126 个 token))`（**套了一层 `println!`**） | ❌ 默认报错；加 `#![recursion_limit = "129"]` 才通过 |

结论有两点，第二点很反直觉：

1. 默认上限就是 **128**，计数的是**整个展开栈的嵌套深度**，不是「某个宏递归了几层」；
2. 所以外面多套一层宏（`println!` 自己也会展开成 `format_args!` 等）就会把这 1~2 层预算吃掉，
   同样 126 个 token 的递归，写法一改就从「能过」变成「超限」。

报错原文（含官方 help）：

```text
error: recursion limit reached while expanding `count!`
   = help: consider increasing the recursion limit by adding a `#![recursion_limit = "256"]` attribute to your crate (`r1`)
```

`#![recursion_limit = "N"]` 是 crate 级属性，只能写在内层属性的位置（文件最上方）。
它调的是**编译期**预算，和运行时栈没关系；调太大只会让编译器更慢、更吃内存。

### 1.6 作用域：`macro_rules!` 默认是「文本作用域」

和函数不同，`macro_rules!` 定义的宏**必须出现在使用点之前**（同一个文件里往下写才算数）。
实测（探针 o1）：

```text
error: cannot find macro `m` in this scope
 --> probe.rs:3:5
  |
3 |     m!();
  |     ^ consider moving the definition of `m` before this call
  |
note: a macro with the same name exists, but it appears later
```

跨模块 / 跨 crate 使用时有两条路：

- `#[macro_export]`：把宏挂到 crate 根，Rust 2018 起可以用 `use crate::sql;` 这类**路径作用域**导入；
- 放在 `mod macros` 里再 `use`：同样走路径作用域。

### 1.7 怎么「看见」展开结果

宏报错难读，是因为报的位置是**展开之后**的。目前本机**没有**装 `cargo-expand`：

```text
$ cargo expand --version
error: no such command: `expand`
```

要看展开结果，先装一个（需要网络）：

```powershell
cargo install cargo-expand
cargo expand --example func_macro
```

或者 nightly 的 `rustc -Zunpretty=expanded`。声明宏的调试也有一条应急路：
`--emit=mir` / 直接看 `stringify!` 的输出（`func_macro.rs` 就是这么确定 token 边界的）。

---

## 2. 卫生性（hygiene）：`macro_rules!` 是**部分**卫生

「卫生」指：宏内部的名字会不会与调用处的名字互相污染。`macro_rules!` 的答案按**名字的类别**分，
不是一刀切。四条规则（全部实证于 `examples/hygiene.rs`）：

| 名字从哪来 | 归属哪个上下文 | 效果 |
| --- | --- | --- |
| 宏体里新写的**局部变量** | 定义处 def-site | 调用处看不见、也撞不上 —— **卫生** |
| 宏体里新写的**标签**（`'lbl:`） | 定义处 def-site | 调用处看不见 —— **卫生** |
| 调用处**传进来**的 token（`$e` / `$n`…） | 调用处 call-site | 就是调用处那个名字，两边互相可见 |
| 宏体里定义的**条目**（fn / const / struct…） | 调用处 call-site | 调用处能直接按名字用 —— **不卫生** |
| 宏体里写的**路径**（不加 `$crate`） | 调用处 | 在调用处所在模块里解析 —— **不卫生** |

### 2.1 局部变量：卫生

```rust
macro_rules! define_local_x { () => { let x = 1; println!("{x}"); } }

let x = 2;
define_local_x!();          // 实测打印 1
println!("{x}");            // 实测打印 2 —— 宏里的 x 没有串出来
```

注意：连宏体里 `println!("{x}")` 格式串中的隐式捕获 `{x}`，也解析到宏体内那个 `x`
（字面量 token 属于定义处上下文）。实测输出见 `examples/hygiene.rs` ① / ②。

顺带一个方言味很重的官方警告（变量没被用到时会冒出来）：

```text
warning: unused variable: `x`
  |
5 |         let x = 1;
  |             ^
  |
help: `x` is captured in macro and introduced a unused variable
```

### 2.2 传进来的 token：属于调用处

```rust
macro_rules! with_local_x {
    ($e:expr) => {{ let x = 1; println!("{x}"); $e }};
}

let x = 100;
with_local_x!(x)            // 实测：先打印 1（宏体内的 x），再打印 100
```

`$e` 里的 `x` 是**调用处的** `x`：token 带着它被写下来时的上下文，宏改不了。
这也是「卫生」只单向生效的表现：宏能保护自己的名字，但拦不住调用者传进来的名字。

### 2.3 用传进来的 `ident` 声明变量 = 声明调用处那个名字

```rust
macro_rules! declare { ($n:ident) => { let $n = 1; }; }

let x = 2;
println!("{x}");            // 2
declare!(x);
println!("{x}");            // 实测 1 —— 被遮蔽了
```

这里不报错只是因为 Rust 允许**遮蔽**（shadowing）；但注意 rustc 的警告会指向调用处的
`let x = 2`，说明宏里 `let $n` 用的确实是调用处那个 `x`（同名），而不是被重命名过的新名字。

### 2.4 条目名：不卫生

```rust
macro_rules! def_items {
    () => {
        fn generated() -> &'static str { "宏体里定义的 fn" }
        const GENERATED: usize = 7;
    };
}

def_items!();
println!("{} / {}", generated(), GENERATED);   // 实测：宏体里定义的 fn / 7
```

宏体里的条目是**真的**在调用处所在的作用域里定义出来的。因此同一个作用域调用两次就是重名：

```text
error[E0428]: the name `generated` is defined multiple times
 --> probe.rs:4:9
  |
4 |         fn generated() -> &'static str {
  |         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ redefined here
  |         previous definition of the value `generated` here
  |
  = note: `generated` must be defined only once in the value namespace of this block
```

这条「不卫生」是有意为之：`thread_local!`、`lazy_static!` 之类宏能让调用处按名字使用宏里
定义的静态量，靠的就是它。

### 2.5 标签：卫生

```rust
macro_rules! one_round { ($e:expr) => {{ 'lbl: loop { break 'lbl; } $e }} }

'lbl: loop { one_round!(...); break 'lbl; }    // 两个 'lbl 互不相干，实测编译通过
```

调用处**看不到**宏体内的 `'lbl`，实测（探针 h4）：

```text
error[E0426]: use of undeclared label `'lbl`
  --> probe.rs:12:24
   |
12 |     run_in_loop!(break 'lbl);
   |                        ^^^^ undeclared label `'lbl`
（另有 warning: unused label —— 宏内那个 'lbl 因此没人用）
```

### 2.6 `$crate`：把路径解析拉回宏定义所在的 crate

宏体里的**裸路径在调用处解析**。实测（探针 h6）把一个只用到 `inner::tag()` 的宏放到
没有 `use crate::inner;` 的模块里调用：

```text
error[E0433]: cannot find module or crate `inner` in this scope
  --> probe.rs:10:9
   |
10 |         inner::tag()
   |         ^^^^^ use of unresolved module or unlinked crate `inner`
   |
   = help: consider importing this module
15 +     use crate::inner;
```

把路径写成 `$crate::inner::tag()`，同一段调用立刻编译通过（`examples/hygiene.rs` ⑥，
实测输出 `guest::run() = crate::inner::tag`）。

> `$crate` 会被替换成「宏定义所在那个 crate」的特殊路径标记，因此**导出给别人用的宏，
> 凡是引用自己 crate 里东西的地方都应该写 `$crate::`**，否则用户一旦没有把对应模块
> 导入自己的作用域就会编译失败。

### 2.7 卫生性可以怎么被破坏

- 宏作者自己把 ident 当参数往外抛（§2.3），调用处就能和它撞名；
- 宏体里直接写下要在调用处生效的名字（§2.4）；
- 用 `#[macro_export]` 之后路径解析变化（§2.6）。

---

## 3. 与 C 预处理器宏的区别

| | C `#define` | Rust `macro_rules!` |
| --- | --- | --- |
| 匹配粒度 | 文本 / 预处理 token，无结构 | token 树结构（括号组天生成对） |
| 校验 | 无 | fragment 分类（`ident` / `expr`…）做语法校验 |
| 卫生性 | 无 | 局部变量与标签卫生 |
| 递归 | 靠手写展开 | 原生递归 + 深度上限 |
| 报错位置 | 指向展开后代码 | 同样指向展开后代码（附 `in this macro invocation` 注解） |

也别忘了：`vec!`、`println!`、`format!`、`concat!`、`stringify!` 全都是标准库用
`macro_rules!` 写好的函数式宏，和 `sql!` 是同一类东西。

---

## 4. 过程宏（proc macro）

### 4.1 为什么 `examples/` 里放不了过程宏

过程宏不是「写一段宏体」，而是**写一个编译期程序**，必须以 `[lib] proc-macro = true` 的形式
独立成 crate：

| 事实 | 证据 |
| --- | --- |
| 过程宏必须由 `proc-macro = true` 的 lib crate 提供 | 探针 `pm_lab`（proc-macro）+ `use_pm`（依赖它）跑通 |
| **定义它的 crate 内部**用不了它 | 这是自举限制：宏的定义就是那个 crate 自己 |
| 同一 package 的**另一个 target 却可以**（反直觉） | 探针：`pm_lab` 里加 `[[bin]]`，在 `src/main.rs` 写 `pm_lab::token_count!(a + (b * c))`，实测编译通过并输出 3 |
| 必须能在「另一个 crate」里被消费 | `use_pm` 的 `main.rs` 同时用了函数式 / 属性 / 派生三种宏 |

本仓库不把它做成 `examples/*.rs` 的原因：`examples/` 是本 package 的 target，要用上过程宏就得
让**本 crate 自己**提供 `proc-macro` lib，那会把整个 package 变成过程宏宿主，和
[`AGENTS.md`](../AGENTS.md) 里「每个 example 都是一份独立可跑、不引入额外依赖的单文件」
的约定冲突。所以配方放在本文，需要动手时新建一个 lab 目录即可（下面就是实测过的那套）。

### 4.2 最小可跑配方（实测）

三个文件，**不依赖 syn / quote**，只用 std 自带的 `proc_macro`（因此不需要网络）：

```toml
# pm_lab/Cargo.toml
[package]
name = "pm_lab"
version = "0.1.0"
edition = "2024"

[lib]
proc-macro = true
```

```rust
// pm_lab/src/lib.rs
use proc_macro::{TokenStream, TokenTree};

/// ① 函数式过程宏：数顶层 token tree 的个数
#[proc_macro]
pub fn token_count(input: TokenStream) -> TokenStream {
    let trees: Vec<String> = input.clone().into_iter().map(|t| t.to_string()).collect();
    println!("[pm] token_count 收到 {} 个顶层 token tree: {trees:?}", trees.len());
    format!("{}usize", trees.len()).parse().unwrap()
}

/// ② 属性宏：两个入参（属性内容 + 被标注的 item），这里原样返回 item
#[proc_macro_attribute]
pub fn trace(attr: TokenStream, item: TokenStream) -> TokenStream {
    println!("[pm] trace attr = `{attr}`");
    item
}

/// ③ 派生宏：找到 `struct <Name>`，为它生成 impl
#[proc_macro_derive(Answered)]
pub fn answered(input: TokenStream) -> TokenStream {
    let mut name = String::new();
    let mut it = input.into_iter();
    while let Some(tt) = it.next() {
        if let TokenTree::Ident(id) = &tt {
            if id.to_string() == "struct" {
                if let Some(TokenTree::Ident(n)) = it.next() {
                    name = n.to_string();
                }
                break;
            }
        }
    }
    println!("[pm] derive Answered 作用在 `{name}`");
    format!("impl {name} {{ pub fn answer() -> u32 {{ 42 }} }}").parse().unwrap()
}
```

```rust
// use_pm/src/main.rs（use_pm/Cargo.toml 里写 pm_lab = { path = "../pm_lab" }）
use pm_lab::{Answered, token_count, trace};

#[derive(Answered)]
struct Q;

#[trace(level = 2)]
fn hello() -> u32 { 1 }

fn main() {
    println!("token_count!(a + (b * c)) = {}", token_count!(a + (b * c)));
    println!("token_count!(a + b * c)   = {}", token_count!(a + b * c));
    println!("Q::answer() = {}", Q::answer());
    println!("hello()     = {}", hello());
}
```

实测输出（`cargo run --manifest-path use_pm/Cargo.toml`）—— 前 4 行是**编译期**打印，
后 4 行才是运行结果：

```text
[pm] derive Answered 作用在 `Q`
[pm] trace attr = `level = 2`
[pm] token_count 收到 3 个顶层 token tree: ["a", "+", "(b * c)"]
[pm] token_count 收到 5 个顶层 token tree: ["a", "+", "b", "*", "c"]
token_count!(a + (b * c)) = 3
token_count!(a + b * c)   = 5
Q::answer() = 42
hello()     = 1
```

三点值得记住：

1. **过程宏在编译期真的会执行你的代码**，`println!` 直接打到 cargo 的编译输出里 ——
   调试过程宏最土也最有效的手段就是这个。
2. 括号组算一个 tree 的规则**同样适用**：`a + (b * c)` 是 3 个 tree，`a + b * c` 是 5 个。
3. 三种宏的签名不同：函数式与属性宏是 `TokenStream → TokenStream`，属性宏与派生宏的
   **输入是「被标注/被派生的那个 item」的 token 流**（派生宏拿不到结构体之外的上下文，
   属性宏会额外收到属性内容 `attr`）。

工程上写过程宏时几乎一定会上 `syn` + `quote`：

| 库 | 作用 |
| --- | --- |
| `syn` | 把 `TokenStream` 解析成 AST（`DeriveInput` 等），省掉手写 §4.2 里那种找 `struct` 的循环 |
| `quote` | 反向：用 `quote! { impl #name { ... } }` 这种插值语法生成 `TokenStream` |
| `proc-macro2` | 让上面两个库能在单元测试 / 非 proc-macro 环境里用 |

（这三个需要从 crates.io 拉依赖，本机本次**未实测**，仅列位置。）

### 4.3 卫生性差异：过程宏默认「全都不卫生」

`macro_rules!` 在局部变量/标签上替你做了卫生，过程宏**没有这层保护**：你在
`TokenStream` 里生成的名字，其上下文取决于每个 token 的 `Span`。常用的是：

```rust
use proc_macro::Span;
let span = Span::call_site();   // 生成的名字「看起来像是调用处写的」→ 会和调用处撞名
```

也就是说，过程宏里生成的辅助变量要么起个足够古怪的名字，要么自己构造 span 来隔离
（`Span::def_site()` 至今仍在 nightly 后面）。这也是为什么过程宏生态里经常能看到
`__serde_xxx` 这种带前缀的临时名。

---

## 5. 报错速查（原文实测）

| 报错首行 | 触发 | rustc 附带的注解 / help |
| --- | --- | --- |
| `error: no rules expected \`1\`` | 所有规则都没匹配上 | `no rules expected this token in macro call`；`note: while trying to match meta-variable \`$a:ident\`` |
| `error: cannot find macro \`m\` in this scope` | 宏定义在使用点之后（文本作用域） | `consider moving the definition of \`m\` before this call`；`note: a macro with the same name exists, but it appears later` |
| `error: recursion limit reached while expanding \`count!\`` | 递归展开超过 `recursion_limit`（默认 128） | `consider increasing the recursion limit by adding a \`#![recursion_limit = "256"]\`` |
| `error[E0426]: use of undeclared label \`'lbl\`` | 调用处想引用宏体内的标签 | 另有 `warning: unused label` 指向宏体内那个标签 |
| `error[E0433]: cannot find module or crate \`inner\` in this scope` | 宏体里的裸路径在调用处解析不到 | `help: consider importing this module` + `use crate::inner;` |
| `error[E0428]: the name \`generated\` is defined multiple times` | 宏体里的条目在调用处重复定义 | `must be defined only once in the value namespace of this block` |
| `warning: unused label` | 宏体内的标签没被自己用到 | 常与 E0426 同时出现 |
| `warning: unused variable` + `help: \`x\` is captured in macro and introduced a unused variable` | 宏体里的 `let` 声明了没用的变量 | 原文语法如此（`a unused variable`） |

读宏报错的通用方法：**先找 `in this macro invocation` 那条注解**，它指向真正写下宏调用的位置；
再回到报错点看是「matcher 不匹配」还是「展开后的代码本身有问题」。

---

## 6. 常见误区

- **`tt` 不等于「一个单词」**：括号组整体算一个（§1.1）。数 token 前先把括号折叠掉。
- **`$name:tt` 里 `tt` 是关键字，`name` 才随便起**：把名字写到冒号右边（如 `($tt:x)`）直接
  `error: invalid fragment specifier \`x\``；名字那头唯一的约束是「展开处同名」（§1.2，探针 n2 / n1）。
- **`$e:expr` 之后就不能再拆了**：想细粒度处理，一开始就用 `tt` 收（§1.2，探针 t2 实测为 1 个 tt）。
- **`macro_rules!` 不能拼接标识符**：不存在「用 token 拼出新名字」的能力
  （`concat_idents!` 至今未稳定）。要动态造名字只能上过程宏。
- **兜底分支放错位置不会报错**，只会静默改变语义（§1.4，探针 o2 实测无警告）。
- **`recursion_limit` 数的是整个展开栈**，外面套一层 `println!` 也会吃掉预算（§1.5）。
- **宏一定先定义后使用**，和函数/类型相反（§1.6）。
- **卫生性不是全局的**：局部变量和标签卫生，条目名和路径不卫生（§2）。
- **过程宏必须独立成 crate**，且不能在定义它的 crate 内部使用（§4.1）。

---

## 交叉引用

- 声明宏的 token 树匹配、重复展开、递归计数：`examples/func_macro.rs`
- 卫生性四条规则的可运行实证：`examples/hygiene.rs`
- 生命周期被擦除 / 类型名探针：`examples/type_name.rs`、[`lifetime.md`](./lifetime.md)
- 编译器与 lint 级别（宏报错如何变成警告）：[`rustc.md`](./rustc.md)、[`clippy.md`](./clippy.md)
- 看展开结果要装的第三方子命令（`cargo-expand`）：[`cargo.md`](./cargo.md)

## 本机实测

```text
$ rustc --version
rustc 1.96.0 (ac68faa20 2026-05-25)

$ cargo run --example func_macro
SELECT | * FROM users                        |  3 tokens
SELECT | id, name FROM users WHERE age > 18  |  9 tokens
INSERT | users VALUES (1, "Ada", 36)         |  3 tokens
RAW    | this is @ not even valid Rust!!!   | 10 tokens
编译期算出的 token_count = 7

$ cargo run --example hygiene
== ① 局部变量卫生 ==
  宏体内的 x   = 1
  调用处的 x   = 2        ← 宏里的 x 没有串出来
...
  guest::run() = crate::inner::tag

$ cargo expand --version
error: no such command: `expand`
```

递归上限的实测口径（探针，`rustc --edition 2024` 单独编译）：

```text
let n = count!(127 个 token);                      -> OK，输出 127
let n = count!(128 个 token);                      -> recursion limit reached
println!("{}", count!(126 个 token))               -> recursion limit reached
  同上 + #![recursion_limit = "129"]               -> OK，输出 126
```

§4.2 的过程宏配方用 `cargo run --manifest-path .probe/use_pm/Cargo.toml` 实跑，
输出即文中所列；`pm_lab` 的 `[[bin]]` 版本（同 package 内使用自身过程宏）也实跑通过。
