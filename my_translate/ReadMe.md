## 工具使用说明
- tray_launch_my_translate.pyw 会在状态栏添加一个管理图标，右键菜单管理翻译服务器。
  - 这个是用 pyw 启动，这样后台长久运行。
- my_translate_web.py 是简单的web服务器脚本
- my_translate.py 是简单的cli脚本
- copy_subtitle_to_potplayer.bat 是为了 potplayer 服务的
  - 给potplayer 增加一个实时翻译的插件

## 模型文件
模型文件单独下载放到指定目录中。
- ct-models                         ct2 模型，转换而成
  - nllb-200-1.3B-ct2
  - nllb-200-3.3B-ct2
  - nllb-200-distilled-1.3B-ct2               目前使用的这个。
- sp-models
  - sentencepiece.bpe.model                   nllb-200 库里的
- llm-models                        原始模型，转换成 ctranslate2 的格式了，所以不是必须的
  - nllb-200-1.3B
  - nllb-200-3.3B
  - nllb-200-distilled-1.3B
  - fasttext-language-identification          目前来说，是废物


## 工具实现简单说明
1. 使用 lingua 检测输入文本的语言。【最早想使用 fasttext 来着的，非常垃圾，还不如langdetect 】
2. 使用 ctranslate2 调用 nllb-200 模型来翻译。