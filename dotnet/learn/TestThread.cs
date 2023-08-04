using System.Diagnostics;

namespace MyTest;

/*
一些tips
1. ThreadStatic 修饰的字段不要设置初始值。初始值只会在一个线程生效，其他的都是默认值。


C# 应该是不推荐使用Thread的。有些缺陷
1. thread 里抛出的异常无法捕捉。会导致整个进程退出。【所以thread可以统一在入口处包装一层try catch】
2. thread Abort 接口也删除的了。提供的 Interrupt 的接口了。需要thread 自己捕捉 ThreadInterruptedException 异常。

总的来说就是推销 Task and CancellationToken
*/

class TestThread{

    static TestThread(){
        Console.WriteLine($"static class constructor run for TestThread, threadid={Thread.CurrentThread.ManagedThreadId}");
    }

#pragma warning disable 0169
    [ThreadStatic] static int s_int;
    [ThreadStatic] static object? s_obj;
#pragma warning restore

    public static void Run() {
        var th = Thread.CurrentThread;
        Console.WriteLine($"TestThread ProcessId={Thread.GetCurrentProcessorId()} ThreadId={Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"Environment ProcessorCount={Environment.ProcessorCount}");

        TestAbort();
        TestInterrupt();
        TestThreadLocal();
        TestCancelThread();
    }


    /*
        https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads
        举了若干 Cancel 的 有用的 例子
        1. 注册回调，及时终止一些异步操作。典型代码：`using var ctr = token.Register(()=> xx.CancelAysnc())` 。
            - https://learn.microsoft.com/en-us/dotnet/standard/threading/how-to-register-callbacks-for-cancellation-requests
        2. 同时监听多个 token。典型代码：`using var cts = CancellationTokenSource.CreateLinkedTokenSource(tk1,tk2)`.
            - 这个代码把多个 token 合并成一个。
            - https://learn.microsoft.com/en-us/dotnet/standard/threading/how-to-listen-for-multiple-cancellation-requests
    */
    private static void TestCancelThread()
    {
        Console.WriteLine("TestCancelThread Start");
        var tokenSource = new CancellationTokenSource();
        List<int> list = new();
        var action = async () => {
            list.Add(Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10).ConfigureAwait(false);
            list.Add(Thread.CurrentThread.ManagedThreadId);
            // not store Task.Exception, it will be null
            tokenSource.Token.ThrowIfCancellationRequested();
            if (tokenSource.IsCancellationRequested){
                throw new Exception("Is Cancelled"); // will store in Task.Exception
            }
            return 1;
        };
        // 第二个参数只在任务没是开始运行时才会有用。这样多数情况下，根本没用。
        // A cancellation token allows the work to be cancelled if it has not yet started.
        // var task = Task.Run(action, tokenSource.Token);
        var task = Task.Run(action);
        tokenSource.CancelAfter(5);
        Thread.Sleep(100);
        try{
            var id = task.Result;
            Console.WriteLine($"task result={id}");
        }catch(AggregateException e){
            if (e.InnerException!.GetType() == typeof(TaskCanceledException)){
                Debug.Assert(task.Exception == null && task.Status == TaskStatus.Canceled);
            }
            else{
                // 壳子不一样，内容一样
                Debug.Assert(task.Exception != e);
                Debug.Assert(task.Exception!.InnerException == e.InnerException);

            }
            Console.WriteLine($"task has exeption = {e.InnerException!.GetType().Name}");
        }
        Console.WriteLine("TestCancelThread End");
    }

    /* https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/thread-abort-obsolete
        Thread.Abort is not supported and throws PlatformNotSupportedException.
        官方建议使用 CancellationToken.ThrowIfCancellationRequested() 代替
        c#不支持捕捉thread里抛出的异常。thread自己捕捉异常。
        补充： 如果真的要 Abort 掉一个线程，使用进程，然后用 Process.Kill
            - https://learn.microsoft.com/en-us/dotnet/standard/threading/using-threads-and-threading
            - https://learn.microsoft.com/en-us/dotnet/standard/threading/destroying-threads

    */
    static void TestAbort(){
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var th = new Thread(()=> {
            Thread.Sleep(10);
            Console.WriteLine($"TestThread.TestAbort {Thread.GetCurrentProcessorId()} {Thread.CurrentThread.ManagedThreadId}");
            // Thread.CurrentThread.Abort();
            // token.ThrowIfCancellationRequested();// 也是垃圾方案，得自己在进程入口处捕捉异常. Task 系统会响应这个异常
            // throw new OperationCanceledException("abort thread");
            // throw new Exception("abort thread");
        });
        try{
            th.Start();
            tokenSource.Cancel();
            // th.Interrupt();// Interrupts a thread that is in the WaitSleepJoin thread state.
            th.Join();
        }
        catch(Exception){

        }
        finally{
            Console.WriteLine($"TestThread.TestAbort finish {th.ThreadState}");
        }
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.interrupt?view=net-7.0&redirectedfrom=MSDN#System_Threading_Thread_Interrupt
    // 注意：Thread.Interrupt() 不会立即终止线程。当线程 Sleep 时才会触发异常。 Interrupts a thread that is in the WaitSleepJoin thread state.
    //          某种意义上来说，现在没有任何方法能强制关闭掉单个线程。只有退出整个APP的选项。
    static void TestInterrupt(){
        Console.WriteLine("TestThread.TestInterrupt Start");
        var th = new Thread(()=>{
            Thread.SpinWait(10000*10);
            Console.WriteLine("TestThread.TestInterrupt Start Sleep");
            // Thread.Sleep(100);// 必须捕捉异常，不然整个APP 会 Exit 。
            // Console.WriteLine("TestThread.TestInterrupt End Sleep");
            try{
                Thread.Sleep(100);// 典型用法，可以 Thread.Sleep(Timeout.Infinite); 然后等待打断睡眠。
                Console.WriteLine("TestThread.TestInterrupt End Sleep In Try");
            }
            catch(ThreadInterruptedException){
                Console.WriteLine("TestThread.TestInterrupt was interrupted in sleep");
            }
        });
        // th.IsBackground = true;//
        th.Start();
        th.Interrupt();
        try{
            th.Join();
            th.Join();// 可以重复调用
        }
        finally{}

        Console.WriteLine("TestThread.TestInterrupt End");
    }


    /*  https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadlocal-1?view=net-7.0
        https://learn.microsoft.com/en-us/dotnet/framework/performance/lazy-initialization
    
    */
    static void TestThreadLocal(){
        Console.WriteLine("TestThreadLocal Start");

        Lazy<int> LazyId = new Lazy<int>(()=>Thread.CurrentThread.ManagedThreadId);

        ThreadLocal<int> ThreadId = new ThreadLocal<int>(() =>
        {
            return Thread.CurrentThread.ManagedThreadId;
        });
        
        Action action = () =>
        {
            bool first = !ThreadId.IsValueCreated;
            Console.WriteLine($"LazyId = {LazyId.Value} ThreadId = {ThreadId.Value} {(first?"first":"")}");
        };
        Action[] acts = new Action[Environment.ProcessorCount];
        for(var i = 0; i < acts.Length; i++){
            acts[i] = action;
        }
        // 8*2 core 的机器上，有4-7个线程的样子。
        Parallel.Invoke(acts);
        action();
        Console.WriteLine("TestThreadLocal End");
    }
}