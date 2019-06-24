"==================="
" 基本设置"
"==================="
set number "显示行号"
set autoread "文件在Vim之外修改过，自动重新读入"
set nocp "使用vim而非vi"
map 9 $ "通过9跳转到行末尾,0默认跳转到行首"
map <silent>  <C-A>  gg v G "Ctrl-A 选中所有内容"

"==================="
" 程序开发相关的设置"
"==================="
syn on "开启语法高亮功能"

"============="
" 设置缩进"
"============="
set cindent "c/c++自动缩进"
set smartindent
set autoindent "参考上一行的缩进方式进行自动缩进"
filetype indent on "根据文件类型进行缩进"
set softtabstop=4 "4 character as a tab"
set shiftwidth=4
set smarttab

"======================"
"设置文件编码，解决中文乱码问题"
"======================"
set fileencodings=ucs-bom,utf-8,latin1
set fileencoding=utf-8
set encoding=utf-8

"=========================="
" 不要交换文件和备份文件，减少冲突"
"=========================="
set noswapfile
set nobackup
set nowritebackup