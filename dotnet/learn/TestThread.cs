namespace MyTest;

/*
C# 应该是不推荐使用Thread的。有些缺陷
1. thread 里抛出的异常无法捕捉。会导致整个进程退出。【所以thread可以统一在入口处包装一层try catch】
2. thread Abort 接口也删除的了。提供的 Interrupt 的接口了。需要thread 自己捕捉 ThreadInterruptedException 异常。

总的来说就是推销 Task and CancellationToken
*/

class TestThread{
    public static void Run() {
        var th = Thread.CurrentThread;
        Console.WriteLine($"TestThread {Thread.GetCurrentProcessorId()} {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"Environment {Environment.ProcessorCount} {Environment.ProcessId} {Environment.ProcessPath}");

        TestAbort();
        TestInterrupt();
    }

    /* https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/thread-abort-obsolete
        Thread.Abort is not supported and throws PlatformNotSupportedException.
        官方建议使用 CancellationToken.ThrowIfCancellationRequested() 代替
        c#不支持捕捉thread里抛出的异常。thread自己捕捉异常。
    */
    static void TestAbort(){
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var th = new Thread(()=> {
            Thread.Sleep(10);
            Console.WriteLine($"TestThread.TestAbort {Thread.GetCurrentProcessorId()} {Thread.CurrentThread.ManagedThreadId}");
            // Thread.CurrentThread.Abort();
            // token.ThrowIfCancellationRequested();// 也是垃圾方案，得自己在进程入口处捕捉异常
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
    // 注意：Thread.Interrupt() 不会立即终止线程。当线程阻塞时才会触发异常。 Interrupts a thread that is in the WaitSleepJoin thread state.
    static void TestInterrupt(){
        Console.WriteLine("TestThread.TestInterrupt Start");
        var th = new Thread(()=>{
            Thread.SpinWait(10000*10);
            Console.WriteLine("TestThread.TestInterrupt Start Sleep");
            // Thread.Sleep(100);// 必须捕捉异常，不然整个APP 会 Exit 。
            // Console.WriteLine("TestThread.TestInterrupt End Sleep");
            try{
                Thread.Sleep(100);
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
}