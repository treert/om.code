# VS Code / Cursor 里的 Rust 语法高亮 —— TextMate 正则 vs 语义 token

> 起因：把 `examples/func_macro.rs:98` 那行**临时改成**
> `println!("{}", sql!(SELECT id, name D Dabc DD FROM users WHERE age > 18));`
> 来观察染色（这行只是为了验证，不属于示例的正式内容）。
> 结果宏体里的 `SELECT` / `FROM` / `WHERE` 被染成一种颜色，单个大写的 `D` 又是另一种颜色，
> 看着像编辑器「读懂了 SQL」——其实**完全是正则**，跟宏的语义毫无关系。
>
> 本文证据来自**本机实测**读取的两个文件：
>
> - 编辑器内置语法（Cursor / CodeBuddy CN 同款，17.27 KB）：
>   `%LOCALAPPDATA%\Programs\cursor\resources\app\extensions\rust\syntaxes\rust.tmLanguage.json`
>   `scopeName: source.rust`，文件头声明源自 `dustypomerleau/rust-syntax`。
> - rust-analyzer 扩展：
>   `%USERPROFILE%\.vscode\extensions\rust-lang.rust-analyzer-0.3.3081-win32-x64\package.json`
>
> 凡属「机制推断」（没在本机逐项跑过）的地方，文中会显式标注。

配套示例：[`examples/func_macro.rs`](../examples/func_macro.rs)（`sql!` 宏本体）、
相关文档：[`macro.md`](./macro.md)（宏机制）、[`rust-analyzer.md`](./rust-analyzer.md)（LSP 本体）。

---

## 1. 结论：编辑器里有**两层**着色

| 层 | 谁提供 | 原理 | 何时出现 |
| --- | --- | --- | --- |
| ① 基础语法 | **编辑器内置**（VS Code / Cursor 自带） | TextMate 语法 = 一堆**正则**，不解析代码 | 打开文件立刻 |
| ② 语义高亮 | **rust-analyzer**（LSP semantic tokens） | 真解析 + 类型推导，按 token 下发 | LSP 起来之后 |

两个关键事实（本机 `package.json` 实测）：

1. **rust-analyzer 扩展里根本没有 Rust 的 TextMate 语法。** 它的 `contributes.grammars` 只有一条：

   ```json
   "grammars": [
     { "language": "ra_syntax_tree", "scopeName": "source.ra_syntax_tree",
       "path": "ra_syntax_tree.tmGrammar.json" }
   ]
   ```

   即：`source.rust` 的语法**不在插件里，在编辑器内核里**（`extensions/rust/`）。插件只叠加语义高亮。

2. 两层会**打架/覆盖**，且这点是官方承认的。`package.json` 里
   `rust-analyzer.semanticHighlighting.comments.enable` 的说明原文：

   > In some editors (e.g. vscode) semantic tokens override other highlighting grammars.
   > By disabling semantic tokens for comments, other grammars can be used to highlight their contents.

   翻译：**在 VS Code 里，语义 token 会盖住语法层**；想让别的语法给你自己的东西上色，就得关掉对应区域上的语义 token。

---

## 2. TextMate 层：纯正则，而且「宏体」这个概念不存在

### 2.1 宏只被匹配「宏名 + `!`」，不吃括号

内置语法里处理宏的规则**只有一条单行 `match`，没有 `begin` / `end`**：

```json
{ "comment": "macros", "name": "meta.macro.rust",
  "match": "(([a-z_][A-Za-z0-9_]*!)|([A-Z_][A-Za-z0-9_]*!))",
  "captures": { "2": { "name": "entity.name.function.macro.rust" },
                "3": { "name": "entity.name.type.macro.rust" } } }
```

它只吃掉 `sql` + `!` 两个 token，**吃完就继续往右扫**。于是 `sql!(...)` 里面的内容
被下面这套「普通 Rust 规则」逐条正则匹配 —— 这就是「串味」的全部原因。

> 对比：普通函数调用 `foo(...)` 确实有一条 `begin: "([A-Za-z0-9_]+)(\\()"` / `end: "\\)"` 的**区域**规则，
> 但那只是换个 entry 名（`meta.function.call.rust`），区域内部引用的 patterns 还是同一套
> `constants / types / macros / keywords / …` 通用规则，对宏体着色没有本质区别。

### 2.2 顶层规则顺序（决定谁先吃掉 token）

`patterns` 数组是**从上到下、先匹配先赢**，本机文件里的顺序为：

```text
块注释 → 行注释 → 属性 → lvariables(self/super) → constants → gtypes(Some/None,Ok/Err)
→ functions → types → keywords → lifetimes → macros → namespaces → punctuation → strings → variables
```

注意 **`constants` 和 `types` 都排在 `macros` 前面**。

### 2.3 与截图直接相关的三条正则（原文照抄）

```json
// constants：全大写常量 —— 要求「至少两个连续大写字母」开头
{ "name": "constant.other.caps.rust", "match": "\\b[A-Z]{2}[A-Z0-9_]*\\b" }

// types：类型名 —— 单个大写字母开头，且后面不跟 !
{ "name": "entity.name.type.rust", "match": "\\b_?[A-Z][A-Za-z0-9_]*\\b(?!!)" }

// variables：小写/数字/下划线开头
{ "name": "variable.other.rust",
  "match": "\\b(?<!(?<!\\.)\\.)(?:r#(?!(crate|[Ss]elf|super)))?[a-z0-9_]+\\b" }
```

另有一条专门救 `>` / `<` 的规则（`comment: "less than, greater than (special case)"`）：
它**要求符号两侧各有一个空白**，且左右边界是词边界或括号时，才把 `<` / `>` 判成比较运算符。
所以 `age > 18` 里的 `>` 会染成 `keyword.operator.comparison.rust`，而 `a>b`（无空格）不会被这条命中。

### 2.4 逐 token 推演：截图那一行为什么长成那样

输入：`println!("{}", sql!(SELECT id, name D Dabc DD FROM users WHERE age > 18));`

| token | 命中的规则 | scope（= 主题上色的依据） |
| --- | --- | --- |
| `println` / `sql` | `#macros` | `entity.name.function.macro.rust` |
| `!` | 被上一条整体吃掉 | （同上，不再单独匹配） |
| `(` `)` `,` `;` | `#punctuation` | `punctuation.brackets.round.rust` 等 |
| `"{}"` | `#strings` → `#interpolations` | `string.quoted.double.rust`（`{}` 另加 `meta.interpolation.rust`） |
| `SELECT` `FROM` `WHERE` `DD` | `constants` 的 `\b[A-Z]{2}...` | **`constant.other.caps.rust`**（常量色） |
| `D` `Dabc` | `types` 的 `\b_?[A-Z]...(?!!)` | **`entity.name.type.rust`**（类型色） |
| `id` `name` `users` `age` | `variables` | `variable.other.rust` |
| `>` | 上面的「特殊比较」规则（`age` 与 `18` 之间正好有空格） | `keyword.operator.comparison.rust` |
| `18` | `constants` 的十进制规则 | `constant.numeric.decimal.rust` |

**可验证的预测**（自己改代码试）：

- `D` 与 `Dabc` 应该**同色** —— 都进 `types`（只要求首字母大写）；
- `DD` 与 `SELECT` / `FROM` / `WHERE` 应该**同色** —— 都进 `constants`（要求连续两个大写）；
- `D` 与 `DD` 应该**不同色** —— 分界线就是 `[A-Z]{2}` 这个量词。

换句话说，你截图的「颜色分布」是**正则边界**的产物，`sql!` 换成任何别的宏名、内容换成任何
大写单词，结果都一样；跟 SQL、跟宏、跟 rust-analyzer 都没关系。

---

## 3. 语义层：rust-analyzer 能补什么、补不了什么

RA 通过 LSP 下发 **semantic tokens**，并用 `contributes.semanticTokenModifiers` 定义了一批修饰符
（本机 `package.json` 实测，摘录）：

| id | 官方 description |
| --- | --- |
| `macro` | Style for **tokens inside of macro calls** |
| `procMacro` | Style for tokens inside of proc-macro calls |
| `injected` | Style for doc-string injected highlighting（文档注释里的代码块） |
| `unresolvedReference` | Style for names which can not be resolved due to compilation errors |

同时用 `contributes.semanticTokenScopes` 把语义 token **映射回 TextMate scope**，让主题能上色，例如：

```json
"language": "rust",
"scopes": {
  "macroBang":        ["entity.name.function.macro.rust"],
  "variable":         ["variable.other.rust"],
  "constParameter":   ["constant.other.caps.rust"],
  "keyword":          ["keyword.other.rust"]
}
```

那么为什么截图里的 `SELECT` 没被 RA「纠正」？因为：

- RA 只对**它解析出来、且能映射回源码位置**的 token 发语义高亮；
- `sql!` 里那些 token 的去向是：被宏规则匹配掉、再进 `stringify!` **变成字符串内容**，
  在语义层没有对应实体 → **没有 semantic token 覆盖** → 编辑器就保留第 2 节那套 TextMate 正则色。
- RA 的 `macro` 修饰符只是给「来自宏调用的 token」**打标签**，它不是 SQL 解析器，也不可能
  猜到 `sql!` 的私有 DSL 语义。

> 一句话：**语法层不知道你用了宏，语义层又理解不了你的 DSL**，夹缝里的 token 就只能被通用正则染。

---

## 4. 想让 `sql!` 真正按 SQL 上色：**注入语法**（不需要改 rust-analyzer）

做法是**自己写一个 VS Code 扩展**，贡献一段 `injectTo` 到 `source.rust` 的注入语法。
不需要动 rust-analyzer 一行代码 —— 因为基础语法本来就在编辑器内核里，注入的目标 scope
`source.rust` 是公开可用的。

```jsonc
// package.json（你自己的扩展）
"grammars": [{
  "scopeName": "source.rust.sql",
  "path": "./syntaxes/rust-sql.injection.json",
  "injectTo": ["source.rust"]          // 注入进 Rust 语法
}]
```

```jsonc
// syntaxes/rust-sql.injection.json
{
  "scopeName": "source.rust.sql.injection",
  "injectionSelector": "L:source.rust",  // L: = 优先级高于被注入的语法
  "patterns": [{
    "begin": "\\bsql!\\s*\\(",           // ★ 给宏调用补一个「区域」
    "end": "\\)",
    "contentName": "meta.embedded.block.sql",
    "patterns": [{ "include": "source.sql" }]   // 复用编辑器内置 SQL 语法
  }]
}
```

要点：

- 第 2.1 节说过，内置语法**没有** `begin`/`end` 区域 —— 这段注入干的事就是**替它补一个**，
  把 `sql!( ... )` 内部的 token 从「普通 Rust 规则」里抢出来。
- `injectionSelector` 的 `L:` 前缀表示把你的规则排在被注入语法**左边**（优先级更高）。
- 这套是 **TextMate 机制，只有 VS Code / Cursor 这类基于 `vscode-textmate` 的编辑器有效**；
  Neovim 的 `nvim-treesitter`、JetBrains 系走的是完全不同的注入机制。
- 另一种等效（但更重）的做法：扩展注册一个 `DocumentSemanticTokensProvider`，
  自己只对 `sql!(...)` 区间发 token。它是**覆盖**而非注入，还要处理和 RA 的 token 优先级。
- 只想要「好看」的话到此为止；**它只影响颜色**。

---

## 5. 想让宏内部能跳转 / 查找引用：注入**做不到**，得上 LSP

这是两个完全不同的能力，别混：

| 能力 | 机制 | 注入语法能不能给 |
| --- | --- | --- |
| 上色 | TextMate / semantic tokens | ✅ 能 |
| 跳转定义、查找引用、悬停、补全、重命名 | **LSP**（语言服务器算出来再回传 span） | ❌ 完全不能 |

因为注入语法只是「给某段文本贴个 scope 名」，它没有任何跨文件索引，也不知道 `users` 是个表还是变量。
真要宏内导航，只有三条路：

**路 A：指望 rust-analyzer 的宏展开映射**（零成本，但覆盖有限）

RA 会把可展开宏的展开结果与源码做 span 映射，因此宏体里
**能对应到真实条目的标识符**可以正常 Ctrl+Click / 悬停。
但 `sql!` 这种把 token 丢进 `stringify!` 的用法，token 变成了字符串内容，映射断开，
`SELECT` / `users` 这类「只在 DSL 里有意义」的东西**永远跳不动**。
（此段为机制推断，可自行验证：在 `vec![some_local]` 里对 `some_local` 试 Ctrl+Click，能跳；换成 `sql!(...)` 里的词就不行。）

**路 B：自己写一个 LSP server**（推荐给「真想做好」的场景）

VS Code 允许**多个扩展同时为同一个语言（`rust`）注册语言服务器**，互不冲突。所以你可以：

1. 写个小 LSP（Node/Rust 都行），只做一件事：从 `.rs` 文本里扫出 `sql!( ... )` 的区间；
2. 在这个区间内自己做跳转定义 / 悬停 / 补全（比如把表名索引到一个 schema 文件）；
3. 顺便把第 4 节的注入语法一起打包 —— 颜色和导航都齐了。

**不需要改 rust-analyzer**，两者并行工作。

**路 C：fork / 改 rust-analyzer**（成本最高）

把 DSL 语义塞进 RA 内部。除非你要的是「官方内置」体验，否则不建议：
RA 没有官方的第三方语义插件 API，改了就得自己跟上游同步。

> 所以你的理解基本正确：**染色可以「外挂注入」，导航必须落到 LSP** ——
> 要么自己再起一个 server，要么改 rust-analyzer。

---

## 6. 自查方法（强烈建议先做这一步）

1. **看某个 token 到底是谁染的**：命令面板 →
   `Developer: Inspect Editor Tokens and Scopes`，光标停在 `SELECT` 上，
   会直接显示它的 TextMate scope（应为 `constant.other.caps.rust`）以及有没有语义 token 覆盖。
   这比猜颜色可靠得多。
2. **本机文件位置**（不同版本号 / 编辑器会变，直接按目录找）：

   ```text
   <编辑器安装目录>\resources\app\extensions\rust\syntaxes\rust.tmLanguage.json   # 语法层
   %USERPROFILE%\.vscode\extensions\rust-lang.rust-analyzer-*\package.json        # 语义层声明
   ```

3. **开关语义高亮做 A/B 对照**：设置里关掉 `editor.semanticHighlighting.enabled`，
   颜色会「退回」纯 TextMate 层 —— 一眼就能看出哪些颜色原本是 RA 给的、哪些是内置正则给的。

---

## 7. 相关

- 宏本身怎么工作：[`macro.md`](./macro.md)
- LSP 工具本体：[`rust-analyzer.md`](./rust-analyzer.md)
- 配套示例：[`examples/func_macro.rs`](../examples/func_macro.rs)
