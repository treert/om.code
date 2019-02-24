using System;


/// <summary>
/// > https://github.com/commandlineparser/commandline
/// 小造轮子，自己简单用用
/// 
/// 限定：
/// 参数：命令之后就是参数了，参数不能是 - 开头，不然会被识别成选项
/// 参数类型：
///     - 基础类型：string bool number(int double ...)
///     - 可以是基础类型的数组
/// 选项格式：`-name[=value]` 默认是bool开关true。
///     - name支持多种别名缩写之类，但是相互间不能冲突。name格式 [-_.0-9a-zA-Z]+，准确来说不能包含=，=是分隔号
///     - 特殊的name可以出现多次，可以用来传入数组什么的。
///     - 不分大小写，防止写乱了
/// 选项类型：和参数一致吧
/// 命令组：
///     - 支持命令组，方便逻辑归类，类似git的 `git clone` `git checkout`
/// </summary>
namespace om.utils.CmdLine
{
    public class Option
    {

    }
}
