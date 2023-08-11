## 建立 本地 nuget 库
可以在本地建立 nuget 库。使用起来还是比较简单的。
```sh
nuget add new_package.1.0.0.nupkg -source <local.dir>
```
> https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds

看了一圈文档，实践后，感觉对个人开发没有必要，并不方便。
不过以后开发时，库代码可以往这个方向靠近。

在想要不要像 go 一样，搞个中央库 om.net 。

## 吐槽
微软的文档，啰里啰嗦没重点，信息密度好低。
