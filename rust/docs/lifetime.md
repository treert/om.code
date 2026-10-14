# 生命周期（lifetime）—— 引用的有效期约束

> 配套可运行实验：[`examples/lifetime.rs`](../examples/lifetime.rs)（`cargo run --example lifetime`）。
> 本文的报错原文与行为均在本机实测过，环境见文末「本机实测」。

## 定位

生命周期回答的是**一个编译期问题**：

> 这个引用的目标，至少要有效到什么时候？

它有三个容易被搞错的性质：

| 问题 | 答案 |
| --- | --- |
| 它是运行时对象吗？ | **不是**。`&'a T` 里的 `'a` 是类型的一部分，编译后完全擦除，`type_name` 打印不到它（见 [`examples/type_name.rs`](../examples/type_name.rs) ③） |
| 它能让数据活得更久吗？ | **不能**。它只负责在推断不成立时报编译错误，不改变任何数据的实际寿命 |
| 它等于「变量活多久」吗？ | **不等于**。它标的是**区域（region）**，是若干约束的交集；NLL 之后，借用的有效期按「最后一次使用」算，而不是词法作用域 |

谁在检查：借用检查器（borrow checker）。所以要理解生命周期，先记住一句话：
**大部分生命周期不是「你写出来的」，是编译器算出来的；你只在它算不出来时补标注。**

## NLL：借用检查的判定基准换了一次

**NLL = Non-Lexical Lifetimes，非词法生命周期**。这是借用检查器的一次大改造，也是「region」这个词
在今天的准确含义来源。一句话概括：**把「借用有效多久」的判定基准，从词法作用域（大括号）
换成了「最后一次被使用」。**

```rust
let mut v = vec![1, 2, 3];  // v：所有者，Vec<i32>，不是引用
let first = &v[0];          // first：引用值 &i32；同时产生一个「对 v 的不可变借用」
println!("{first}");        // 最后一次使用 first
v.push(4);                  // 这里还能不能可变借用？
```

### 先分清三个不同的东西

这个例子里 `v` 是**被借用的所有者**，`first` 才是引用。NLL 缩短的对象是第三个东西：

| 对象 | 是什么 | NLL 动了它吗 |
| --- | --- | --- |
| `v` | 所有者：`Vec<i32>`，一个普通局部变量 | **没有**。它的存活区间照旧到作用域结束 |
| `first` | 引用值：`&i32`（本质是指针，且是 `Copy`） | 只缩短了它**类型里**那个 region；`first` 这个变量本身没有提前结束 |
| **借用 / loan** | borrowck 的一条记账：「`v` 正处于不可变借用中」 | **这才是 NLL 缩短的对象** |

所以「借用的有效期」指的是上面第三行那个 loan 的 region，**不是** `v` 的寿命，也**不是**
`first` 变量的作用域。`examples/lifetime.rs` ① 里那句注释要按这个理解。

顺带把两边的借用都展开一下，能直接看出冲突的是哪两个 loan：

```rust
&v[0]                          // 等价于 &*Index::index(&v, 0) —— 借出 &v
v.push(4);                     // 你写的
Vec::<i32>::push(&mut v, 4);   // 编译器实际做的：接收者自动取 &mut
```

`&mut self` 意味着**整个调用期间**都持有 `&mut v`（调用返回即释放）；而 `&v[0]` 那个 loan
在 NLL 下只撑到 `first` 最后一次被使用。判定的就是这两个 loan 能不能共存。

`Vec::<i32>::push(&mut v, 5)` 这种 UFCS 写法在 `examples/lifetime.rs` ① 里**真的写了一行**
（紧跟在 `v.push(4)` 后面），这样「隐含的 `&mut`」在代码里是看得见、能编译、能跑出结果的，
不只是注释里的说法。

### 词法判定 vs NLL 判定

| | 「借用」的有效期是 | `v.push(4)` 的结果 |
| --- | --- | --- |
| **NLL 之前**（词法判定） | 从 `&v[0]` 到**产生该借用的那个块**结束 | ❌ E0502 |
| **NLL 之后**（现在默认） | 从 `&v[0]` 到 `first` **最后一次被使用**那一点 | ✅ 合法 |

上表右列的报错，就是要求「借用**后来**还被用到」才会出现（本机原文）：

```text
error[E0502]: cannot borrow `v` as mutable because it is also borrowed as immutable
 --> probe.rs:6:5
  |
3 |     let first = &v[0];
  |                  - immutable borrow occurs here
...
6 |     v.push(4);
  |     ^^^^^^^^^ mutable borrow occurs here
7 |     println!("{first}");
  |                ----- immutable borrow later used here
```

第三条注解（`immutable borrow later used here`）正是 NLL 的口吻：**判定依据是「用到的位置」，
不是「变量在哪个大括号里」。** `examples/lifetime.rs` ① 里 `last` 打印完就 `v.push(4)`，
因为后面不再使用 `last`，所以能编过。

几个必须分清的点：

| 问题 | 答案 |
| --- | --- |
| NLL 让生命周期消失了吗？ | 没有。只是 `'a` 从「一个词法作用域」变成了「**一组程序点（program point）的集合**」——这就是「region 是若干约束的交集」的由来 |
| 它跑在哪里？ | 不在 AST 上，而是在 **MIR**（rustc 的中层 IR，已展平成控制流图）上做**活跃性分析**（liveness），所以能看出「哪条控制流上这个引用还被用到」 |
| 它和「省略规则」是一回事吗？ | **不是**。NLL 减少的是「为了凑作用域而人为加的 `{ }` 或标注」；省略规则是另一套机制，NLL 没动它，E0106 该报还是报 |
| 什么版本开始默认？ | Rust 1.31（2018 edition）起默认，2015 edition 随后也切了过去；本机 1.96.0 属于 NLL 之后很多年，无需任何开关 |
| 它完美了吗？ | 没有。还有路径敏感的少数场景判不准（社区称 NLL 的 "problem case #3"，典型是「条件分支里插入/取出借用」），后续方案 **Polonius** 用 Datalog 重做，目前仍在 nightly 的 `-Z polonius` 后面，未进 stable |

**术语坑**：「非词法」**不是**说生命周期和代码位置无关 —— 恰恰相反，NLL 之后生命周期**极其依赖位置**
（就差一个语句，结论就反了）。它否定的只是「按大括号划分」这一种粗粒度做法。

**NLL 不会提前 drop 任何值**：它缩短的只是「借用必须被认为有效」的区间，
所有值的销毁时机依旧由作用域和所有权决定。实测（`examples/lifetime.rs` ① 里带 `Drop` 探针的 `Noisy`）：

```text
last            = 3
push 之后 v     = [1, 2, 3, 4, 5]
d 还活着        = d（等本函数结束才 drop）
  [drop] d         ← 借用早在 v.push(4) 时就结束了，值却仍然活到函数末尾
```

实际意义：NLL 只会让你**更早地能借、能移**，不会替你释放内存。

**亲眼看一眼 MIR**（本机实测可用）：

```powershell
cargo rustc --example lifetime -- --emit=mir
# 产出 target/debug/examples/lifetime.mir，借用被降成普通赋值，例如：
#   _33 = <Vec<i32> as Index<usize>>::index(move _34, move _35) -> [return: bb21, unwind: bb33];
```

注意 `--emit=mir` 给的是**借用检查之后、且优化过**的 MIR，看不到活跃区间标记；
真正的「借用检查输入 / 活跃性区间」需要 nightly 的 `-Z dump-mir` 系列开关（本机 stable 未实测）。

## 省略规则（elision）—— 编译器替你补的三条规则

函数签名里凡是没写生命周期的地方，按下面三条自动补：

| # | 规则 | 展开示例 |
| --- | --- | --- |
| 1 | **参数位置**每个被省略的生命周期，各自成为一个**独立**的新参数 | `fn f(x: &str, y: &str)` → `fn f<'a, 'b>(x: &'a str, y: &'b str)` |
| 2 | 输入位置**恰好一个**生命周期时，它赋给**所有**输出 | `fn f(x: &str) -> &str` → `fn f<'a>(x: &'a str) -> &'a str` |
| 3 | 只要有 `&self` / `&mut self`，`self` 的生命周期赋给**所有**输出（规则 2 的优先例外） | `fn f(&self, x: &str) -> &str` → `fn f<'s, 'b>(&'s self, x: &'b str) -> &'s str` |

三个后果：

- 规则 1 意味着**两个入参不会自动共用同一个生命周期**，所以「多入参 + 返回引用」必然落在规则之外 → E0106（见下）。
- 规则 2 只在「恰好一个」时生效。零个引用入参时，`fn dangling() -> &str` 也是 E0106。
- 规则 3 是**无条件**的：即使签名里还有别的引用入参，返回值也只跟 `self` 走。
  `examples/lifetime.rs` ②里用「把 `_other` 提前 drop、返回值依旧可用」实证了这一点。

> 反直觉点：`fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result` 里，
> `Formatter<'_>` 的那个 `'_` **不是** `&self` 的生命周期，而是 `f` 那层 `&mut` 之后的
> 另一个独立参数（规则 1）。之所以容易混淆，是因为规则 3 让人以为「有 self 就都跟 self 走」——
> 规则 3 只决定**输出**，不决定参数之间是否共享。

## `'_`：匿名生命周期

`'_` 表示「这里确实有一个生命周期参数，但我不给它起名字，请编译器推断」。
它和 `'a` 语义等价，区别只是不需要（也不能）在别处引用这个名字。

**能省和不能省，实测结论（rustc 1.96.0）**：

| 位置 | 写光秃秃的 `Wrap`（`Wrap<'a>` 的省写） | 结论 |
| --- | --- | --- |
| 函数参数 `fn f(w: Wrap)` | **能编译**，只触发默认关闭的 lint：`warning: hidden lifetime parameters in types are deprecated` + `help: indicate the anonymous lifetime` | 惯例写 `'_`（`-W elided-lifetimes-in-paths` 可强制） |
| `let` 类型标注 `let w: Wrap` | **能编译**，无警告 | 惯例写 `'_` |
| struct / enum 字段 `struct H { w: Wrap }` | `error[E0106]: missing lifetime specifier` | **必须**写 `'_` 或具名 |
| 函数返回、且没有任何引用入参 `fn f() -> &str` | `error[E0106]: missing lifetime specifier` | **必须**具名或 `'static` |

也就是说：`Formatter<'_>`、`Ref<'_, String>` 这类写法**首先是惯例**（开了 lint 之后就是硬要求），
真正「不写就编译不过」的位置是字段定义和返回类型。别把这两者混为一谈。

## 必须手写标注的三类场景

### ① 多个引用入参 + 返回引用

```rust
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() >= y.len() { x } else { y }
}
```

去掉标注后的报错原文：

```text
error[E0106]: missing lifetime specifier
  = help: this function's return type contains a borrowed value, but the signature
    does not say whether it is borrowed from `x` or `y`
help: consider introducing a named lifetime parameter
```

语义：`'a` 取 `x`、`y` 有效期的**交集**，调用方必须保证两者在返回值用完前都还活着
（实际表现是「短命的那个参数会把返回值一起拖下水」）。

### ② struct / enum 里存引用

```rust
struct Excerpt<'a> { part: &'a str }
```

类型定义本身必须带参数，`impl` 侧可以沿用 `impl<'a> Excerpt<'a>` 或写 `impl Excerpt<'_>`。
实例的寿命被原数据锚住：`&text` 借还活着时，`text` 不能被移动或销毁（E0505）。

### ③ 显式表达约束关系

`&'a T`、`T: 'a`、`where 'b: 'a`（`'b` 至少和 `'a` 一样长）等，用于把「谁活得比谁久」
写进类型。日常代码里绝大多数情况用不到，碰到 trait 设计或自引用结构时才会需要。

## `'static`

`'static` 有两种含义，别混：

1. **活得和程序一样久**：字符串字面量的类型就是 `&'static str`，数据被写进二进制。
2. **作为 trait bound**：`T: 'static` 读作「`T` 里**不含**短命的借用」，**不是**「`T` 在运行时永远活着」。
   拥有所有权的类型（`String`、`Vec<T>`、`i32`…）天然满足，跟它们何时被 drop 无关：

   ```rust
   fn to_any<T: 'static>(v: T) -> Box<dyn Any> { Box::new(v) }
   to_any(String::from("owned"));      // OK
   // to_any(local.as_str());          // E0597：`local` does not live long enough
   ```

反向操作也存在：想让运行时产物的引用变成 `'static`，可以主动泄漏（内存永不回收，慎用）：

```rust
let leaked: &'static str = Box::leak(String::from("leak").into_boxed_str());
```

`Box<dyn Trait + 'static>` 里的 `'static` 走的也是含义 2（「这种 trait 对象里不带短命借用」）。

## 常见报错速查（原文实测）

| 错误码 | 首行原文 | 典型触发 | rustc 给的 help |
| --- | --- | --- | --- |
| E0106 | `missing lifetime specifier` | 多引用入参 + 返回引用；字段 / 返回位置省略生命周期 | `consider introducing a named lifetime parameter` |
| E0515 | `cannot return reference to local variable \`s\`` | 返回局部变量引用（`-> &'static str { &local }`） | 注解：`returns a reference to data owned by the current function` |
| E0597 | `` `local` does not live long enough `` | 借用的目标先被 drop 了 | 注解：`borrowed value does not live long enough` |
| E0502 | ``cannot borrow `v` as mutable because it is also borrowed as immutable`` | 不可变借用**后来还被用到**时又想可变借用（判据是 NLL 的「用到哪」） | 注解三连：`immutable borrow occurs here` / `mutable borrow occurs here` / `immutable borrow later used here` |
| E0505 | ``cannot move out of `text` because it is borrowed`` | 借用未结束时移动 / drop 原值 | `consider cloning the value if the performance cost is acceptable` |
| E0716 | `temporary value dropped while borrowed` | 临时值（如 `String::from("x").as_str()`）产生的引用被留了下来 | `consider using a let binding to create a longer lived value` |

读报错的通用方法：**看两条 `-->` 标注**——一条指向「借用发生的地方」，一条指向
「被借用的值必须活到的地方」，E0597 就是这两条对不上的产物。

## 与其他文档/示例的交叉引用

- 可运行实验：`examples/lifetime.rs`（①函数签名 + NLL（含 `Noisy` 的 Drop 反证）②struct 引用 ③`'_` ④`'static`）
- 生命周期被擦除的实证：`examples/type_name.rs` ③（`&'static str` 与 `&str` 打印结果一样）
- `Formatter<'_>` 的真实出处：`examples/func_macro.rs:21`
- 让编译器不把它当错误、而是当建议的机制：见 [`clippy.md`](./clippy.md) 与 [`rustc.md`](./rustc.md) 的 lint 级别控制

## 注意 / 常见误区

- **`'a` 不是「某个变量的作用域」**，而是编译器为一段区域取的名字；同一个 `'a` 出现在多个位置
  时，它代表的是这些约束的**交集**，而不是「某个具体的生命期长度」。
- **加了生命周期标注不会让程序跑起来**：所有标注都是约束，写错只会编译失败，
  绝不会把悬垂引用「修好」。看到 E0597 / E0515 应该去想**数据的所有权该放哪**，
  而不是「再加个生命周期试试」。
- **`'_` ≠ `'static`**：前者是「你替我推」，后者是「活到程序结束」。写 `'_` 永远不会延长寿命。
- **规则 3 只影响输出**：`&self` 出现时返回值一律跟 `self`，不要以为返回的引用同时受其他入参约束。
- **借用检查的粒度是「变量 / 路径」，不是堆内存**：`&v[0]` 实际指向 `Vec` 内部的缓冲区，
  而 `v.push(4)` 在容量不够时会 reallocate、让原指针悬垂。borrowck 不认识堆，
  它只能在「`v` 被借用了」这个粒度上记账，所以宁保守也不放过 —— E0502 必须报。
- 单文件里临时验证生命周期问题，最快的方式是直接 `rustc --edition 2024 file.rs`（见 [`rustc.md`](./rustc.md)），
  比在 examples 里反复改快得多。

## 本机实测

```text
$ rustc --version
rustc 1.96.0 (ac68faa20 2026-05-25)

$ cargo run --example lifetime
生命周期只存在于编译期，运行时看不到它的任何痕迹

== ① 函数签名里的生命周期 ==
first_word(&s)  = "hello"
longest(&a, &b) = "abcd"
last            = 3
push 之后 v     = [1, 2, 3, 4, 5]
...
规则 3 返回值 = "the quick brown fox"  （short_lived 已 drop，r 依旧有效）
```

```text
$ cargo rustc --example lifetime -- --emit=mir
$ ls target/debug/examples/*.mir
target/debug/examples/lifetime.mir
```

上表中的错误码与 help 文案（含 NLL 那条 E0502），是用 `rustc --edition 2024` 逐个编译最小反例
抓出来的原文（非记忆复述）；MIR 的形态是直接打开 `lifetime.mir` 确认的，
所以文中没有写 `StorageLive` / `StorageDead` 这类标记 —— 它们在 stable 的 `--emit=mir` 输出里
（借用检查之后、优化之后）已经看不到了。
