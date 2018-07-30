## fxxk
多线程实现有个死锁问题。`Cancel`时如果最终调用了`lock(xxx)`会发生死锁。
没找到问题。
排除的几个问题
1. `lock(this)`和`lock(m_lock_obj)`效果一样

找到问题了。
Winform的UI事件响应时也有一个锁，`Control.Invoke`时就要获取这把锁。
工作线程用this作为锁。
这样就有两把锁，且UI线程和工作线程都有使用了，然后就死锁了。
解决办法是：**不要同时锁上两把锁**，不要在一个锁的区域内获取另外的锁。UI线程控制不了，调整下自己的工作线程逻辑。
思索：
1. go语言的channel把锁封装在了内部，这样一次只会用到一把锁，值得参考。
2. 获取管理对象状态时还是直接访问属性的方便，用锁比较方便，用channel之类的思路就麻烦了。不过不小心调用个回调可能就死锁了。
  - 也许可以采用汇报状态的做法。
3. 日志和日志的显示可以，现在的处理很浪费。

## 卡顿
发现UI会卡顿。
调整了下默认线程数为4，不自动滚动richTextBox，在公司4核电脑上可接受了。

1. richTextBox处理log时，每次都用list.ToArray()，当日志有1万多行是，基本算卡死了。
2. 调整log方式后，发现richTextBox只提供了AppendText函数，于是自己来滚动，发现会很卡顿。这个组件不行啊。
3. 默认不自动滚动日志，还是有明显卡顿。CPU占用率也不是很高，不知为什么。调整默认线程数为4，好像好了一些，能接受，懒得对比了，。

## 超时
c# webclient之类的单个host有连接数限制，默认就两个。多线程需要设置下。
`ServicePointManager.DefaultConnectionLimit = Math.Max(thread_limit + 3, ServicePointManager.DefaultConnectionLimit);`。

## 下载llvm文档，巨大无比
发现是doxygen生成了几G的数据，虽然表面地址是`llvm.org/docs/doxygen/html`目录下，但实际是301跳转搞的。
修改下爬虫，追踪301跳转，获取实际的url地址。

## 发现
搜索发现了一篇问答，讨论如何把 Html Site 变成 ebook的。
> https://ebooks.stackexchange.com/questions/2/how-can-i-convert-an-html-site-into-an-ebook

### 下载
里面提到个[Httrack](http://www.httrack.com/page/2/en/index.html),可以下载整个网站（不知能不能只下载特定目录）。

### 转电子书
chm是比较倾向的格式，但是没找到好的方法用winform创建。倒是找到些工具，就是都有不满意的地方。Hugechm不能设置index索引，比较遗憾。
epub说是比较好的文档，也暂时没发现文章讲这么编程创建。

下载的cplusplus在文件里运行的好好的，但是hugechm编译成chm后，目录UI显示错误。应该是脚本出错了，蛋疼。
chm是微软私有，依赖IE，但又被微软抛弃了。就每个替代格式的，比如Google出个依赖Chrome的webdoc。

