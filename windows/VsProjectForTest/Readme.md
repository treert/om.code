## 测试同时读写文件
File.Open有两个权限设置，FileAccess 和 FileShare。
连续打开两次，第二次可能会出错。测试了下，找个规律
1. 只有Read,Write,ReadWrite组合可以兼容
2. 第二次打开时的不出错条件: share2 Contain acess1, share1 Contain access2。