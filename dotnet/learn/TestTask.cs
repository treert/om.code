using System.Diagnostics;

namespace MyTest;

/*
> 非常有用的官方文档，比如讲了 await void 的问题 https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios#important-info-and-advice

补充阅读
- Task.Delay vs Thread.Sleep
    - Thread.Sleep 会阻塞线程。Task.Delay 不一定，只是延后。
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


https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

c# 的异步机制可以理解成无栈协程。使用时，必须在函数的定义出使用 async 调用处使用 await 。
不如 go lua 的有栈协程来得方便呐。
是不推荐大规模使用么？

*/
class TestTask{
    public static void Run()
    {
        Console.WriteLine("\nTestTask");

        TestDeadlock();

        TestAny().Wait();

        TestCancelTask();

        try{
            TriggerException().Wait();
        }catch(AggregateException e){
            Console.WriteLine($"catch exception in task msg={e.InnerException?.Message}");
        }

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
        Console.WriteLine("before exception");
        throw new Exception("trigger exception in task");
        #pragma warning disable 0162
        await Task.Delay(10);
        Console.WriteLine("after exception");
        #pragma warning restore
    }

    /*
    Near the end, you see the line await finishedTask;. The line await Task.WhenAny doesn't await the finished task.
    It awaits the Task returned by Task.WhenAny. The result of Task.WhenAny is the task that has completed (or faulted).
    You should await that task again, even though you know it's finished running.
    That's how you retrieve its result, or ensure that the exception causing it to fault gets thrown.
    */
    static async Task TestAny(){
        var task1 = Task.Run(async ()=>{
            await Task.Delay(Random.Shared.Next(1,10));
            return 1;
        });
        var task2 = Task.Run( ()=>{
            Task.Delay(Random.Shared.Next(1,10)).Wait();
            return 2;
        });
        var list = new List<Task<int>> {task1, task2};
        while(list.Count > 0){
            var finishedTask = await Task.WhenAny(list);
            // 这时就可以获取结果了，官方文档有问题呀。不过官方文档建议使用 await 替代 Task.Result/Task.Wait
            // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios#important-info-and-advice
            Console.WriteLine($"finish {finishedTask.Result}");
            var ret = await finishedTask;
            Console.WriteLine($"finish wait {ret}");

            list.Remove(finishedTask);
        }
    }

    /*
        都说可能有 deadlock 但是没构造出例子出来。
        对Task的运行机制理解不够深
    */
    static void TestDeadlock(){
        Console.WriteLine("TestDeadlock");
        static async Task<string> Foo()
        {
            await Task.Delay(1).ConfigureAwait(false);
            return "";
        }
        async static Task<string> Ros()
        {
            return await Bar();
        }
        async static Task<string> Bar()
        {
            return await Foo();
        }

        // 
        Task.WaitAll(Enumerable.Range(0,40).Select(x => Ros()).ToArray());
    }

    static void TestCancelTask(){
        var sw = new Stopwatch();
        sw.Start();
        Console.WriteLine($"TestCancelTask Start {DateTime.Now.ToString("ss.fff")}");
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var t0 = Task.Run(async ()=>{
            await Task.Delay(1000);
            tokenSource.Cancel();
        });
        var t1 = Task.Run(async ()=>{
            while(! token.IsCancellationRequested){
                await Task.Delay(5);
            }
        });
        Task.WaitAll(t0,t1);
        Console.WriteLine($"TestCancelTask End {DateTime.Now.ToString("ss.fff")}");
        sw.Stop();
        Console.WriteLine($"TestCancelTask Cost {sw.ElapsedMilliseconds}ms");
    }
}