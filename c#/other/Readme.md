## 打包exe和dll
1. ILMerge 
    这个工具是MS官方提供的，在 http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=17630 可以下载得到。这个工具能够把几个可执行文件（exe或者dll）打包集成进一个可执行文件中，具体使用方法网上很多，这里不再赘述。值得说明的是，我尝试写了一个.bat批处理来merge，效果非常好。利用pause指令还能随时暂停ILMerge运行过程，可以看到merge失败时是哪里的问题。
2. 嵌入DLL作为资源。
    推荐使用这种方式。这个方法是CLR via C#的作者发明的（貌似，反正我是从他那里学的），原帖的地址http://blogs.msdn.com/b/microsoft_press/archive/2010/02/03/jeffrey-richter-excerpt-2-from-clr-via-c-third-edition.aspx
3. 有个可视化工具 [ILMergeGUI](https://bitbucket.org/wvd-vegt/ilmergegui)
    - 安装ILMerge.msi(来自官网)
    - 运行ILMergeGUI_Portable.exe

> https://zhidao.baidu.com/question/2138162200025161588.html 
> https://www.cnblogs.com/knowledgesea/p/5284645.html 
> https://archive.codeplex.com/?p=ilmergegui 