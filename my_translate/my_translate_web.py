import argparse
from dataclasses import dataclass
import sys
from flask import Flask, request, jsonify, render_template
from typing import List
import json

import waitress

@dataclass
class GArgs:
    debug:bool = False

g_args = GArgs()

app = Flask(__name__)

# 假设这是你已有的翻译函数
def translate_line(text: str, tgt_lang: str = 'zho_Hans', src_lang: str = 'auto', score_limit: float = -1) -> str:
    import my_translate
    # 这里是你原有的翻译实现
    return my_translate.translate_line(text, tgt_lang, src_lang, score_limit)

@app.route('/translate', methods=['POST'])
def translate_api():
    # 根据 Content-Type 选择不同的解析方式
    if request.content_type == 'application/x-www-form-urlencoded':
        # 解析 form-urlencoded 数据
        text = request.form.get('text', '')
        tgt_lang = request.form.get('tgt_lang', 'zho_Hans')
        src_lang = request.form.get('src_lang', 'auto')
        score_limit = request.form.get('score_limit', -20, type=float)
    else:
        # 默认解析 JSON 数据
        data = request.get_json()
        text = data.get('text', '')
        tgt_lang = data.get('tgt_lang', 'zho_Hans')
        src_lang = data.get('src_lang', 'auto')
        score_limit = data.get('score_limit', -20)

    if isinstance(text, str):
        lines = text.splitlines()
        rets = []
        for line in lines:
            line = line.strip()
            if len(line) > 0:
                result = translate_line(line, tgt_lang, src_lang, score_limit)
                if g_args.debug:
                    # print("input:  ", line)
                    # print("output: ", result)
                    # 上面的写法，launcher 获取不到这些日志
                    sys.stdout.write(f"input:  {line}\n")
                    sys.stdout.write(f"output: {result}\n")
                    sys.stdout.flush()
                rets.append(result)
            else:
                rets.append('')
        return jsonify({
            'translation': '\n'.join(rets)
            })
    else:
        return jsonify({'error': 'Invalid input'}), 400

@app.route('/')
def index():
    return render_template('index.html')


'''
curl -X POST http://localhost:5000/translate \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello world", "tgt_lang": "zho_Hans", "src_lang": "auto"}'
'''

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="My Translate Server")

    def add_option(*args, default:bool, action:str='nil',**kwargs):
        help = kwargs.get('help','')
        if default:
            kwargs['help'] = f"(default True, set False) {help}"
            parser.add_argument(*args, action='store_false', **kwargs)
        else:
            kwargs['help'] = f"(default False, set True) {help}"
            parser.add_argument(*args, action='store_true', **kwargs)

    parser.add_argument("-p",'--port', type=int, default=5000, help='server port. default 5000')
    add_option('-d', "--debug", default=g_args.debug, help="run in debug mode")
    add_option('-o', "--outside", default=False, help="publish to external network")

    args = parser.parse_args()
    g_args.debug = args.debug
    port = args.port
    host = args.outside and '0.0.0.0' or '127.0.0.1'
    import my_translate
    if g_args.debug:
        app.run(host=host, port=port, debug=True)
    else:
        sys.stdout.write(f"Running on http://{host}:{port}\n")
        waitress.serve(app, host=host, port=port)