## 使用 workspace and module 来管理代码
```sh
# 新建工作区目录 workspace
mkdir go
cd go

# 在 workspace 下面初始化一个模块 mypkg
mkdir mypkg
cd mypkg
go mod init mypkg

# 在 workspace 目录下创建工作区
# 这个命令在 workspace 目录下生成了一个 go.work 文件
# 这个文件就将 mypkg 模块和工作区进行了关联
go work init ./mypkg

# 在 workspace 目录下克隆第二个模块 myutils，并将这个模块和工作区关联
mkdir myutils
cd world
go mod init myutils

# 回到 workspace 目录下将第二个模块 myutils 和工作区关联
go work use ./myutils
```
> https://zhuanlan.zhihu.com/p/591522723

## go install 
参数是目录。需要里面的包是个 main package, has main 函数。
```sh
# windows: install mypkg.exe in $GoPath/bin
go install mypkg
```