using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MyTest;

class LogCall : IDisposable
{
    string m_msg = string.Empty;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LogCall()
    {
        // var xx = System.Reflection.MethodBase.GetCurrentMethod().Name;
        // https://stackoverflow.com/questions/2652460/how-to-get-the-name-of-the-current-method-from-code
        var st = new StackTrace();
        var method = st.GetFrame(1)?.GetMethod();
        if (method is not null) {
            m_msg = $"{method.DeclaringType?.Name}.{method.Name}";
        }
        _Log(true);
    }
    public LogCall(string msg)
    {
        m_msg = msg;
        _Log(true);
    }
    public void Dispose()
    {
        _Log(false);
    }

    void _Log(bool is_start)
    {
        Console.ForegroundColor = is_start ? ConsoleColor.Green : ConsoleColor.DarkGreen;
        if (m_msg.Length > 0)
        {
            var str = (is_start ? "Start" : "End");
            var id = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"--- {m_msg} - {str} - theadid={id} ---");
        }
        Console.ResetColor();
    }
}