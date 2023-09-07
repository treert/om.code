namespace MyTest;

/*
dotnet add package NLog
> https://github.com/NLog/NLog/wiki/Tutorial
折腾了下。这个还是挺好用的。

对比了好几个 log 库
> https://betterstack.com/community/guides/logging/how-to-start-logging-with-net/
------------- 下面的可看可不看 -------------------

https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
官方文档真是一言难尽。不知道在讲啥！

自定义 logging provider
> https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
> https://github.com/dotnet/docs/tree/main/docs/core/extensions/snippets/configuration/console-custom-logging

微软的 extensions 提供了一层 依赖注入的抽象。可能更好吧，不过感觉非常不好用。


微软提供的 Microsoft.Extensions.Logging 只是提供了个接口。
还需要添加如  Microsoft.Extensions.Logging.Console 才能真正工作。
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Logging.Console
dotnet add package Microsoft.Extensions.Logging.Debug
*/
// need run: 
using Microsoft.Extensions.Logging;

public class TestLog
{
    static TestLog()
    {
        // https://github.com/NLog/NLog/wiki/Configuration-file
        // NLog.config 的属性修改为 Copy-if-new 后，就可以用了。不然相对目录不对，读不到文件。
        // 也可以放到App.config里。不过似乎没有 NLog.config 好。
        // 用这种配置方法，不需要调用Load
        // NLog.LogManager.Setup().LoadConfigurationFromFile();

        // 读取指定路径配置。测试用下而已。
        // NLog.LogManager.Setup().LoadConfigurationFromFile("""E:\ExtGit\om\om.code\dotnet\learn\NLog.config""");

        // 通过代码配置。
        // NLog.LogManager.Setup().LoadConfiguration(builder =>
        // {
        //     builder.ForLogger().FilterMinLevel(NLog.LogLevel.Debug).WriteToConsole();
        //     builder.ForLogger().FilterMinLevel(NLog.LogLevel.Debug).WriteToFile(fileName: "nlog.txt");
        // });
    }
    private static NLog.Logger _nlogger = NLog.LogManager.GetCurrentClassLogger();
    public static void Run()
    {
        TestNLog();
    }

    static void TestNLog()
    {
        using var _ = new LogCall();
        using(_nlogger.PushScopeNested("nlog-scope")){
            _nlogger.Debug("a debug");
            _nlogger.Info("a info");
            _nlogger.Warn("a warn");
            _nlogger.Error("a error");
            _nlogger.Fatal("a fatal");
        }
    }

    // 微软的接口，没找到输出到文件的方法。官方文档也不知道在说啥，无语。
    static void TestMicroLog()
    {
        using var _ = new LogCall();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
                            {
                                options.IncludeScopes = true;
                                options.SingleLine = true;
                                options.TimestampFormat = "HH:mm:ss ";
                            });
        });

        ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
        using (logger.BeginScope("[scope is enabled]"))
        {
            logger.LogInformation("Hello World!");
            logger.LogInformation("Logs contain timestamp and log level.");
            logger.LogInformation("Each log message is fit in a single line.");
        }
    }
}