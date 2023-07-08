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

public class TestChannel{
    class Item{
        public int i=1;
        public string s = "1234";
    }
    public static void Run(){
        {
            var ch = Channel.CreateUnbounded<Item>();
            // 生产
            var producer = Task.Run(()=>{
                Thread.Sleep(10);
                Parallel.For(0,10,i => {
                    ch.Writer.WriteAsync(new Item{i=i,s=i.ToString()});
                });
                Thread.Sleep(10);
                Parallel.For(10,20,i => {
                    ch.Writer.WriteAsync(new Item{i=i,s=i.ToString()});
                });
                Thread.Sleep(10);
                ch.Writer.Complete();
            });

            //消费数据
            var consumer = Task.Run(async ()=>{
                while (await ch.Reader.WaitToReadAsync())
                {
                    if (ch.Reader.TryRead(out var message))
                    {
                        Console.WriteLine($"read {message}");
                    }
                }
            });
            Console.WriteLine("finish test unbound channel");
        }
    }
}