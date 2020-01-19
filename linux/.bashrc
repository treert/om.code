PS1='\[\e[1;36m\]\u\[\e[35m\]@\[\e[32m\]\h\[\e[31m\]:\[\e[34m\]\w\[\e[33m\]\$ \[\e[0m\]'

export LANG="zh_CN.UTF-8"

alias egrep='egrep --color=auto'
alias fgrep='fgrep --color=auto'
alias grep='grep --color=auto'
alias l='ls -CF'
alias la='ls -A'
alias ll='ls -alF'

# set color: ls
# > useful color preview tool https://geoff.greer.fm/lscolors/
# > https://unix.stackexchange.com/questions/2897/clicolor-and-ls-colors-in-bash
if [ `uname` = "Darwin" ]; then
    # macos 
    export CLICOLOR=1
    export LSCOLORS=exfxcxdxbxegedabagacad
else
    alias ls='ls --color=auto'
    export LS_COLORS='di=34:ln=35:so=32:pi=33:ex=31:bd=34;46:cd=34;43:su=30;41:sg=30;46:tw=30;42:ow=30;43'
fi