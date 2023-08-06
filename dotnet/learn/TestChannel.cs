/*header
    > File Name: TestChannel.cs
    > Create Time: 2023-07-06 星期四 20时10分28秒
    > Author: treertzhu
*/

/*
cs 的 channel 类似 go 的 channel. 适用于生产消费模型。
*/

using System.Threading.Channels;

namespace MyTest;

public class TestChannel
{
    record Item
    {
        public int i = 1;
        public int idx = 0;
    }
    public static void Run()
    {
        TestProduceAndConsume();
    }

    static void TestProduceAndConsume()
    {
        using var _ = new LogCall();
        var ch = Channel.CreateUnbounded<Item>();
        // 生产。其实是多生产者。
        var producer = Task.Run(() =>
        {
            Thread.Sleep(10);
            int cnt = 0;
            Console.WriteLine("produce 11");
            Parallel.For(0, 10, async i =>
            {
                // 这种写法也保证不了顺序的。因为是2条命令。
                int nn = Interlocked.Add(ref cnt, 1);
                await ch.Writer.WriteAsync(new Item { i = i, idx = nn });
            });
            Thread.Sleep(10);
            Console.WriteLine("produce 22");
            Parallel.For(10, 20, async i =>
            {
                int nn = Interlocked.Add(ref cnt, 1);
                await ch.Writer.WriteAsync(new Item { i = i, idx = nn });
            });
            Thread.Sleep(10);
            ch.Writer.Complete();
            Console.WriteLine("complete channel");
        });

        //消费数据
        var consumer = Task.Run(async () =>
        {
            while (await ch.Reader.WaitToReadAsync())
            {
                if (ch.Reader.TryRead(out var message))
                {
                    Console.WriteLine($"read {message}");
                }
            }
        });
        Task.WaitAll(producer, consumer);
    }
}