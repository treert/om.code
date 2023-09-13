using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MyTest;

/*
https://devblogs.microsoft.com/dotnet/configureawait-faq/
非常棒的文章，讲了ConfigureAwait相关的原理。【里面举的死锁例子不好（或者说不完整），好的例子见 Test_ConcurrentExclusiveSchedulerPair_And_DeadLock 】
*/

/* tips
1. do not use ref and out in Task main function.
2. compute-bound method should exposed as sync function, I/O-bound method should exposed as async function.
    - https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-task-based-asynchronous-pattern#workloads
3. MyAwaitable 的实现有些诡异呀。OnComplete 的调用条件和想象的不一样。YieldAwaitable 也是一个样，这个名字应该叫做 DoWorkToComplete
*/

/*
1. Task.Start 一般不用调用的。
    - 只有通过构造函数创建的Task需要调用 Start 进入 hot state
    - 函数返回的的 task 应该都已经Start了。
    - https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap#task-status
2. Task.IsCompleted 包含三种状态：RanToCompletion,Faulted,Canceled。失败和放弃都算完成了。
    - Any continue code will run
    - Any await or read Result will success or throw exception

1. Cannot await in the body of a lock statement. 编译报错
    - SemaphoreSlim.WaitAsync() and SemaphoreSlim.Release() 可以实现lock await 的功能。【不推荐这么用】
    - https://www.jenx.si/2019/08/23/c-locks-and-async-tasks/
*/

/*
> 非常有用的官方文档，比如讲了 await void 的问题 https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios#important-info-and-advice

补充阅读
- Task.Delay vs Thread.Sleep 见 TestBlock()
    - await Task.Delay 不阻塞线程。
    - Thread.Sleep 和 Task.Delay.Await 会阻塞线程，线程池资源-1。
- await Task.Delay() vs. Task.Delay().Wait()
    - 官方推荐使用 await 关键字. await 不阻塞当前线程。
    - 最佳答案 https://stackoverflow.com/questions/26798845/await-task-delay-vs-task-delay-wait
    - Wait() can cause deadlock issues once you attached a UI to your async code.
        - 也许 Task.Delay().Wait() 类似 Thread.Sleep 吧
- async void VS async Task
    - 官方文档说了寂寞 https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#15154-evaluation-of-a-void-returning-async-function
    - 值得一看 https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#avoid-async-void
    - 回答里有些简洁的结论 https://stackoverflow.com/questions/45447955/why-exactly-is-void-async-bad
    - 个人总结：async Task 就是正常的异步任务。async void 不知道为啥不作为 async Task 的语法糖。
        - async void 有一些缺点, 避免使用。
            - Can not await it. So caller may exit before it has completed, this maybe painful.
            - Any unhandled exceptions will terminate your process (ouch!) 【异常不会抛给父级】
    - async void should only be used for event handlers. 【出自官方文档】
- Task.Yield() 作用类似与降低优先级。IsCompleted = false, 这样一定会调用一下 OnComplete。假设一个单线程调度器每次把回调压到最后面执行，那它就可以降低后续代码的优先级了。
    - https://blog.csdn.net/gqk01/article/details/131180845


https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

c# 的异步机制可以理解成无栈协程。使用时，必须在函数的定义出使用 async 调用处使用 await 。
不如 go lua 的有栈协程来得方便呐。
是不推荐大规模使用么？

*/
class TestTask
{
    public static void Run()
    {
        Console.WriteLine("\nTestTask");

        TestAny();

        TestCancelTask();

        TestBlock();

        TestUpValue();

        try
        {
            TriggerException().Wait();
        }
        catch (AggregateException e)
        {
            Console.WriteLine($"catch exception in task msg={e.InnerException?.Message}");
        }

        Test_ConcurrentExclusiveSchedulerPair_And_DeadLock();

        // TestDeadlock();

        TestCustomAwaitable();

    }

    class MyAwaitable : INotifyCompletion
    {
        public void OnCompleted(Action continuation)
        {
            // 如果await 时，IsComplete = True 这儿是不会调用的。【总感觉有什么不对】
            Debug.Assert(InnerIsIsCompleted == false);
            Console.WriteLine($"MyAwaitable OnCompleted {DateTimeOffset.Now} {DateTimeOffset.Now.ToUnixTimeMilliseconds()} thread_id={Thread.CurrentThread.ManagedThreadId}");
            Task.Run(continuation);
        }

        public MyAwaitable GetAwaiter() {
            Console.WriteLine($"MyAwaitable GetAwaiter thread_id={Thread.CurrentThread.ManagedThreadId}");
            return this;
        }

        public MyAwaitable(){
            Console.WriteLine($"MyAwaitable Construct {DateTimeOffset.Now} {DateTimeOffset.Now.ToUnixTimeMilliseconds()} thread_id={Thread.CurrentThread.ManagedThreadId}");
            m_start_ts = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        private long m_start_ts;
        public bool InnerIsIsCompleted => m_start_ts + 20 < DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public bool IsCompleted {
            get{
                var xx = InnerIsIsCompleted;
                Console.WriteLine($"MyAwaitable IsCompleted {xx} thread_id={Thread.CurrentThread.ManagedThreadId}");
                return xx;
            }
        }
        public void GetResult() { 
            // 经过测试，发现这一步同步卡住，直到 IsComplete == True
            Console.WriteLine($"MyAwaitable GetResult thread_id={Thread.CurrentThread.ManagedThreadId}");
        }
    };

    private static void TestCustomAwaitable()
    {
        using var log = new LogCall();
        Task.Run(async () =>
        {
            var xx = new MyAwaitable();
            await xx;
            Console.WriteLine($"TestCustomAwaitable finish MyAwaitable.IsCompleted={xx.InnerIsIsCompleted} {DateTimeOffset.Now} {DateTimeOffset.Now.ToUnixTimeMilliseconds()} thread_id={Thread.CurrentThread.ManagedThreadId}");
        }).Wait();
    }


    private static void Test_ConcurrentExclusiveSchedulerPair_And_DeadLock()
    {
        using var log = new LogCall();

        static async Task _Wait()
        {
            await Task.Yield();// 这个比 Task.Delay(1) 好。它是立即执行完，然后通知调度器去执行后续回调的。
            return;
        };
        {
            var ces = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel: 1);
            var t = Task.Factory.StartNew(() =>
            {
                _Wait().Wait();
            }, default, TaskCreationOptions.None, ces.ExclusiveScheduler);
            bool is_finish = !t.Wait(5);
            Console.WriteLine($"t is_deadlock={is_finish} {t.Status} ");
        }
        {
            // 因为 maxConcurrencyLevel 是1 也会卡住，如果是 2, 就不会卡住了
            var ces = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel: 1);
            var t = Task.Factory.StartNew(() =>
            {
                _Wait().Wait();
            }, default, TaskCreationOptions.None, ces.ConcurrentScheduler);
            bool is_finish = !t.Wait(5);
            Console.WriteLine($"t is_deadlock={is_finish} {t.Status}");
        }
    }

    /*
        Test_ConcurrentExclusiveSchedulerPair_And_DeadLock 的例子更好。

        这两个例子在 WPF 的 UI 线程里执行都会卡死。原因是 UI 定制的 SynchronizationContext 是单线程的。
        这两个例子隐含的两次回调存在相互依赖。就卡死了。
    */
    static void TestDeadlock()
    {
        using var _ = new LogCall();
        // example 1
        async Task<string> Foo()
        {
            await Task.Delay(1);
            return "";
        }
        Foo().Wait();
        // example 2. Unhandled exception. System.InvalidOperationException: The current SynchronizationContext may not be used as a TaskScheduler.
        // Task.Delay(1).ContinueWith((_) => { },TaskScheduler.FromCurrentSynchronizationContext()).Wait();
    }

    private static void TestUpValue()
    {
        using var log = new LogCall();
        Task[] tts = new Task[3];
        for (int i = 0; i < tts.Length; i++)
        {
            int k = i;
            tts[i] = Task.Run(() =>
            {
                Console.WriteLine($"thread_id={Thread.CurrentThread.ManagedThreadId} i={i} k={k}");
            });
        }
        Task.WaitAll(tts);
    }

    private static void TestBlock()
    {
        using var log = new LogCall();
        Thread? h1 = null;
        var t1 = Task.Run(async () =>
        {
            await Task.Yield();
            int id1 = Thread.CurrentThread.ManagedThreadId;
            Interlocked.Exchange(ref h1, Thread.CurrentThread);
            await Task.Delay(100);
            int id2 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"finish t1 {id1} {id2}");
        });

        Thread? h2 = null;
        var t2 = Task.Run(async () =>
        {
            int id1 = Thread.CurrentThread.ManagedThreadId;
            await Task.Yield();
            Interlocked.Exchange(ref h2, Thread.CurrentThread);
            Task.Delay(100).Wait();
            int id2 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"finish t2 {id1} {id2}");
        });

        Thread? h3 = null;
        var t3 = Task.Run(async () =>
        {
            int id1 = Thread.CurrentThread.ManagedThreadId;
            await Task.Yield();
            Interlocked.Exchange(ref h3, Thread.CurrentThread);
            Thread.Sleep(100);
            int id2 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"finish t3 {id1} {id2}");
        });

        Thread.Sleep(10);
        // thread status h1=Background h2=Background, WaitSleepJoin h3=Background, WaitSleepJoin
        Console.WriteLine($"thread status h1={h1?.ThreadState} h2={h2?.ThreadState} h3={h3?.ThreadState}");
        Console.WriteLine($"thread status t1={t1.Status} t2={t2.Status} t2={t2.Status}");
        Task.WaitAll(t1, t2, t3);
        Console.WriteLine($"thread status h1={h1?.ThreadState} h2={h2?.ThreadState} h3={h3?.ThreadState}");
        Console.WriteLine($"thread status t1={t1.Status} t2={t2.Status} t2={t2.Status}");
    }

    /*
        When code running asynchronously throws an exception, that exception is stored in the Task. 
        The Task.Exception property is a System.AggregateException because more than one exception may be thrown during asynchronous work.
        Any exception thrown is added to the AggregateException.InnerExceptions collection.
        If that Exception property is null, a new AggregateException is created and the thrown exception is the first item in the collection.

        The most common scenario for a faulted task is that the Exception property contains exactly one exception.
        When code awaits a faulted task, the first exception in the AggregateException.InnerExceptions collection is rethrown.
    */
    static async Task TriggerException()
    {
        using var log = new LogCall("TriggerException");// must pass name explict
        await Task.Delay(10);
        Console.WriteLine("before exception");
        throw new Exception("trigger exception in task");
#pragma warning disable 0162
        await Task.Delay(10);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("after exception");// will not run
#pragma warning restore
    }

    /*
    Near the end, you see the line await finishedTask;. The line await Task.WhenAny doesn't await the finished task.
    It awaits the Task returned by Task.WhenAny. The result of Task.WhenAny is the task that has completed (or faulted).
    You should await that task again, even though you know it's finished running.
    That's how you retrieve its result, or ensure that the exception causing it to fault gets thrown.
    */
    static void TestAny()
    {
        using var _ = new LogCall();
        var task1 = Task.Run(async () =>
        {
            await Task.Delay(Random.Shared.Next(1, 10));
            return 1;
        });
        var task2 = Task.Run(() =>
        {
            Task.Delay(Random.Shared.Next(1, 10)).Wait();
            return 2;
        });
        var tt = Task.Run(async () =>
        {
            var list = new List<Task<int>> { task1, task2 };
            while (list.Count > 0)
            {
                var finishedTask = await Task.WhenAny(list);
                // 这时就可以获取结果了，官方文档有问题呀。不过官方文档建议使用 await 替代 Task.Result/Task.Wait
                // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios#important-info-and-advice
                Console.WriteLine($"finish {finishedTask.Result}");
                var ret = await finishedTask;
                Console.WriteLine($"finish wait {ret}");

                list.Remove(finishedTask);
            }
        });
        tt.Wait();
    }

    static void TestCancelTask()
    {
        using var _ = new LogCall();
        var sw = new Stopwatch();
        sw.Start();
        Console.WriteLine($"TestCancelTask Start {DateTime.Now.ToString("ss.fff")}");
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var t0 = Task.Run(async () =>
        {
            await Task.Delay(20);
            tokenSource.Cancel();
        });
        var t1 = Task.Run(async () =>
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(5);
            }
        });
        var t2 = t1.ContinueWith((task) =>
        {
            // t1 is canceled. t2 run also.
            Console.WriteLine($"t1.Status={t1.Status}");
        });
        try
        {
            // Task.WaitAll(t0,t2);// no exception
            Task.WaitAll(t0, t1);// throw exception AggregateException(TaskCanceledException)
        }
        catch (Exception e)
        {
            // Task.WaitAll(t0,t1) will run this.
            Console.WriteLine($"get exception {e.Message} inner={e.InnerException?.Message}");
        }

        Console.WriteLine($"TestCancelTask End {DateTime.Now.ToString("ss.fff")}");
        sw.Stop();
        Console.WriteLine($"TestCancelTask Cost {sw.ElapsedMilliseconds}ms");
    }
}