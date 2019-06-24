alias egrep='egrep --color=auto'
alias fgrep='fgrep --color=auto'
alias grep='grep --color=auto'
alias l='ls -CF'
alias la='ls -A'
alias ll='ls -alF'

# set color: ls
if [ `uname` = "Darwin" ]; then
    # macos 
    # > https://www.jianshu.com/p/488869d76447
    export CLICOLOR=1
    export LSCOLORS=gxfxaxdxcxegedabagacad
else
    alias ls='ls --color=auto'
fi