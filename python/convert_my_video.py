import argparse
import atexit
from dataclasses import dataclass, field
import logging
import os
from pathlib import Path
import re
import subprocess
import sys
import time
from typing import IO, List

import colorlog

"""
代码几乎都是AI生成的。花了一天还多的时间进行调试组装。
怎么说了，AI很强，但是也不万能。比如为了能获取 ffmpeg 的输出，支持进度条，AI给的方法不好。
"""

@dataclass
class GArgs:
    input_path:list[str] = field(default_factory=list)
    out_dir:str = '.'
    set_bitrate: int = 0
    dry_run: float = 0
    dry_run_out: bool = False
    overwrite: bool = False
    out_ext:str = '.mkv'

g_args = GArgs()
g_exts = ('.mp4', '.mkv', '.avi', '.rmvb', '.wmv', '.mov')

# 配置日志系统
# logging.basicConfig(
#     level=logging.DEBUG,  # 设置日志级别
#     format='%(asctime)s - %(levelname)s - %(message)s',
#     stream=sys.stdout,# 默认是 stderr
# )

def setup_colored_logging():
    """配置彩色日志输出"""
    formatter = colorlog.ColoredFormatter(
        '%(log_color)som-log: %(asctime)s %(levelname)-8s %(message)s%(reset)s',
        datefmt='%Y-%m-%d %H:%M:%S',
        log_colors={
            'DEBUG': 'cyan',
            'INFO': 'green',
            'WARNING': 'yellow',
            'ERROR': 'red',
            'CRITICAL': 'red,bg_white',
        },
        reset=True,
        style='%',
        # stream=sys.stdout,# 默认是 stderr 没有用
    )
    
    console = colorlog.StreamHandler(sys.stdout) # 不传参数，会输出到 sys.stderr 里
    console.setFormatter(formatter)
    
    
    logger = colorlog.getLogger()
    logger.setLevel(logging.INFO)
    logger.setLevel(logging.DEBUG)
    logger.addHandler(console)

setup_colored_logging()


def get_video_codec(input_file):
    """使用 ffprobe 检测视频编码格式"""
    cmd = [
        'ffprobe',
        '-v', 'error',
        '-select_streams', 'v:0',
        '-show_entries', 'stream=codec_name',
        '-of', 'default=noprint_wrappers=1:nokey=1',
        input_file
    ]
    try:
        codec = subprocess.check_output(cmd).decode('utf-8').strip().lower()
        logging.debug(f"get_video_codec {codec}")
        return codec
    except subprocess.CalledProcessError as e:
        logging.error(f"错误：无法检测视频编码 - {e}")
        sys.exit(1)

def get_video_height(input_file):
    """使用 ffprobe 获取视频帧高度"""
    cmd = [
        'ffprobe',
        '-v', 'error',
        '-select_streams', 'v:0',
        '-show_entries', 'stream=height',
        '-of', 'default=noprint_wrappers=1:nokey=1',
        input_file
    ]
    try:
        cmd_out = subprocess.check_output(cmd).decode('utf-8').strip()
        height = int(cmd_out)
        return height
    except subprocess.CalledProcessError as e:
        logging.error(f"错误：无法获取视频高度 - {e}")
        sys.exit(1)

def get_video_duration(input_file):
    """使用ffprobe获取视频总时长（秒）"""
    cmd = [
        'ffprobe', '-v', 'error',
        '-show_entries', 'format=duration',
        '-of', 'default=noprint_wrappers=1:nokey=1',
        input_file
    ]
    result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    return float(result.stdout.strip())

def time_to_seconds(time_str):
    """将时间字符串 (HH:MM:SS.ms) 转换为秒数"""
    try:
        h, m, s = time_str.split(':')
        return int(h) * 3600 + int(m) * 60 + float(s)
    except Exception:
        return 0
        pass
def determine_bitrate(height):
    """根据视频高度确定目标码率（单位：kbps）"""
    if g_args.set_bitrate > 0:
        return g_args.set_bitrate
    elif g_args.set_bitrate < 0:
        return 0

    # 1080p@3000k 以此为基准做缩放。
    if height <= 480:
        return 500    # 480p 或更低
    elif height <= 720:
        return 1200   # 720p
    elif height <= 1080:
        return 3000   # 1080p
    elif height <= 1440:
        return 5000   # 2K
    else:
        return 8000   # 4K 或更高

def print_pipe(stream:IO[bytes]):
    """输出流内容，支持进度条"""
    buffer = bytearray()  # 动态累积 bytes
    while True:
        byte = stream.read(1)  # 每次读取 1 字节
        if not byte:  # 流结束
            if buffer:  # 如果还有剩余数据，输出 【其实没必要判断】
                sys.stdout.buffer.write(buffer)
                sys.stdout.flush()
            break

        # 如果遇到 \r 或 \n，输出当前 buffer 并清空
        if byte in (b'\r', b'\n'):
            # if buffer:
            sys.stdout.buffer.write(buffer)
            sys.stdout.buffer.write(byte)
            sys.stdout.flush()
            buffer.clear()  # 清空 buffer
        else:
            buffer.extend(byte)  # 累积字节
    pass

def transcode_video(input_file, output_file, bitrate):
    """使用 ffmpeg 进行转码（显示实时输出）"""
    # 下面的这些设置需要搭配 av1_nvenc 使用
    cmd = [
        'ffmpeg',
        # '-progress','pipe:1',                           # 效果不佳
        # '-movflags', '+faststart',                      # 报错，没有用
        # '-analyzeduration','200M',                      # 允许 FFmpeg 分析长达 analyzeduration 的数据来检测流信息
        # '-probesize', '100M',                           # 允许 FFmpeg 读取前 probesize 的数据来探测格式
        '-i', input_file,

        ## 元数据
        # '-map_chapters', '0',                           # 复制所有章节数据
        # '-map_metadata', '0',                           # 复制全局元数据
        # '-map_metadata:s:v','0:s:v',                    # 复制视频流元数据
        # '-map_metadata:s:a','0:s:a',                    # 复制音频流元数据
        # '-map_metadata:s:s','0:s:s',                    # 复制字幕流元数据 如果没有数据，会报错，并且不能使用?来标记可选
        # '-map_metadata:s:d','0:s:d',                    # 复制字幕流元数据
        # '-metadata',f'title="{Path(input_file).stem}"', # 标题
        # '-metadata',f'artist="one001"',                 # 作者
        

        #### 只编码第一个视频数据流，其他的全部copy。 可以运行成功。但是内容不对，冗余了
        # '-map', '0', '-c', 'copy',
        # '-map', '0:v:0', '-c:v:0', 'av1_nvenc',


        #### 常规用法
        # '-map', '0', '-map', '-0:v', '-c', 'copy',      # 复制除了视频之外的其他流 需要放在最前面。但是这样不符合正常视频的顺序。【错误用法】
        
        '-map', '0:v:0',                               # 选择第一个视频流
        # '-map', '0:v',                                  # 选择所有的视频流
        # '-pix_fmt','yuv420p',                           # conver流使用的像素格式（如 yuvj420p）已被弃用，需要这个，才不报错
        # '-force_key_frames','00:00:01',                 # 想增加个预览封面的，但是没有用。安装了 k-lite 就有了。
        '-c:v', 'av1_nvenc',                            #
        # '-c:v', 'hevc_nvenc',
        # '-c:v', 'h264_nvenc',
        # '-multipass', 'qres',                           # disabled(default),qres,fullres 似乎有用
        '-rc', 'vbr',                                   # -1(default), constqp,vbr,cbr
        '-preset', 'p7',                                # 最高质量
        # '-b:v', f'{bitrate}k',                        

        '-map', '0:a', '-c:a', 'copy',                  # 复制音频

        '-map', '0:s?', '-c:s', 'copy',                 # 复制字幕

        '-map', '0:t?', '-c:t', 'copy',                 # 复制附件
        
        '-map', '0:d?', '-c:d', 'copy',                 # 复制数据



        '-y'
    ]
    if bitrate > 0:
        cmd.extend(('-b:v', f'{bitrate}k'))             # 如果不设置，不知道 av1_nvenc 是怎么决定 bitrate 的，小文件输出反而变大了。
    else:
        # cmd.extend(('-cq','23'))                        # default 0。仅在 -rc constqp 下生效。基本没用
        pass
    if g_args.dry_run > 0 and not g_args.dry_run_out:
        cmd.extend(('-f', 'null'))
        cmd.append('-')
    else:
        cmd.append(output_file)
    try: # call ffmpeg
        print(" ".join(cmd))
        print("")
        mode = 1
        if mode == 3:
            # 这个看上去最简单。
            try:
                process = subprocess.run(
                    cmd,
                    stdout=sys.stdout,       # 实时打印 stdout
                    stderr=sys.stderr,       # 实时打印 stderr
                    check=True,              # throw CalledProcessError if returncode != 0
                    timeout=g_args.dry_run if g_args.dry_run > 0 else None,
                    )
            except subprocess.TimeoutExpired:
                # 超时当成是正常现象
                pass
            except KeyboardInterrupt:# 有个缺点，会输出一段
                print("") # 
                logging.info(f"Ctrl+C KeyboardInterrupt")
                raise subprocess.CalledProcessError(1, cmd)
        elif mode <= 2:
            # 这些模式没有支持 timeout，如果要实现需要在 loop 里计时
            process:subprocess.Popen = None
            is_timeout = False
            if mode == 2:
                # AI给了这个方案，头大，明明有简单的方案。不支持 timeout，实现麻烦
                process = subprocess.Popen(
                    cmd,
                    stderr=subprocess.STDOUT,
                    stdout=subprocess.PIPE,
                    universal_newlines=False,  # 关闭文本模式 保留 \r（回车符）或其它控制字符（如进度条）
                    bufsize=0,
                )

                # def kill_child():
                #     process.terminate()  # 或 proc.kill()
                # atexit.register(kill_child)  # 父进程退出时触发
                atexit.register(lambda: process.terminate())

                try:
                    print_pipe(process.stdout)
                except KeyboardInterrupt:
                    print("") # 
                    logging.info(f"Ctrl+C KeyboardInterrupt")
                    sys.exit(1)
                except Exception as e:
                    print("") # 
                    logging.error(f"未知错误：{e}")
                    process.terminate()
            elif mode == 1:
                # 第一次尝试，开始时无法处理进度条。后为了定制自己的进度条，这个反而是最佳的了
                # 获取视频总时长
                total_duration = get_video_duration(input_file)

                # 正则表达式匹配进度信息
                time_pattern = re.compile(r'time=(\d+:\d+:\d+\.\d+)') # time may be N/A
                progress_pattern = re.compile(
                    r'frame=\s*[^ ]+\s+fps=\s*[^ ]+\s+q=\s*[^ ]+\s+'
                    r'size=\s*[^ ]+\s+time=([^ ]*)\s+'
                    # r'bitrate=\s*\d+\.?\d*kbits/s\s+speed=\s*\d+\.?\d*x'
                )

                process = subprocess.Popen(
                    cmd,
                    stderr=subprocess.STDOUT,
                    stdout=subprocess.PIPE,
                    universal_newlines=True, # 没法处理进度条的情况，\r 会被转换成 \n
                    encoding='utf-8',
                    errors='ignore',
                    bufsize=1,
                )
                atexit.register(lambda: process.terminate())
                try:
                    start_time = time.time()  # 记录开始时间
                    for line in process.stdout:
                        line:str
                        line = line.rstrip('\n')
                        # 尝试匹配进度信息
                        time_match = progress_pattern.search(line)
                        if time_match:
                            current_time = time_match.group(1)
                            current_seconds = time_to_seconds(current_time)
                            
                            # 计算百分比
                            percent = min(100, (current_seconds / total_duration) * 100)
                            
                            # 在原始进度信息前添加百分比
                            sys.stdout.write(f"\r[{percent:3.0f}%] {line}")
                            sys.stdout.flush()
                        else:
                            if logging.getLogger().getEffectiveLevel() <= logging.DEBUG:
                                # 普通日志信息直接输出
                                sys.stderr.write(line)
                                sys.stderr.write('\n')
                                sys.stderr.flush()
                        if g_args.dry_run > 0:
                            now = time.time()
                            if now - start_time > g_args.dry_run:
                                is_timeout = True
                                process.terminate()
                                break
                    sys.stdout.write('\n')
                except KeyboardInterrupt:
                    sys.stdout.write('\n')
                    logging.info(f"Ctrl+C KeyboardInterrupt")
                    sys.exit(1)
                
            else:
                assert(False)
                pass
            if process:
                process.wait()
                if not is_timeout and process.returncode != 0:
                    raise subprocess.CalledProcessError(process.returncode, cmd)
        
        logging.info(f"转码完成：{output_file}")
    except subprocess.CalledProcessError as e:
        logging.error(f"错误：转码失败 - 返回码 {e.returncode}")
        sys.exit(1)

def find_mp4_files(directory: str) -> List[str]:
    """递归查找目录下的所有MP4文件"""
    mp4_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.lower().endswith(g_exts):
                mp4_files.append(os.path.join(root, file))
    return mp4_files

def process_file(input_file: str, out_dir: str):
    """处理单个视频文件"""
    # 检测编码格式
    codec = get_video_codec(input_file)
    if (codec == 'av1' or 'hevc')and not g_args.overwrite:
        logging.info(f"跳过：{input_file} 已是 AV1 or hevc 编码")
        return

    # 获取视频参数
    height = get_video_height(input_file)
    bitrate = determine_bitrate(height)

    # 生成输出路径
    filename_without_ext = Path(input_file).stem
    output_file = os.path.join(out_dir, f"{filename_without_ext}{g_args.out_ext}")
    if not g_args.overwrite and os.path.exists(output_file):
        logging.warning(f"跳过：{input_file}. output_file exsit {output_file}")
        return
    # output_file = os.path.join(out_dir, f"{filename_without_ext}.mkv")

    # 执行转码
    logging.info(f"start_encode：{input_file}({codec}, {height}p, to {bitrate}kbps). Press Ctrl+C to interrupt.")
    transcode_video(input_file, output_file, bitrate)

def main():
    global g_args
    parser = argparse.ArgumentParser(description="AV1 批量转码工具")
    parser.add_argument("input_path", nargs='+', help="输入文件或目录路径")

    def add_option(*args, default:bool, action:str='nil',**kwargs):
        help = kwargs.get('help','')
        if default:
            kwargs['help'] = f"(def True,set False) {help}"
            parser.add_argument(*args, action='store_false', **kwargs)
        else:
            kwargs['help'] = f"(def False,set True) {help}"
            parser.add_argument(*args, action='store_true', **kwargs)

    parser.add_argument('-o', "--out-dir", default=g_args.out_dir, help="输出目录（默认当前目录）")
    add_option('-f', "--overwrite", default=g_args.overwrite, help="是否可以覆盖目标文件")
    parser.add_argument('-b', "--set-bitrate", type=int, default=g_args.set_bitrate, help=f"set -b:v xk 0:auto_set,>0:set,<0:not_set (default){g_args.set_bitrate}")
    parser.add_argument('-n', "--dry-run", type=float, default=g_args.dry_run, help="指定每个文件试运行几秒，会忽略输出文件")
    add_option('-no', "--dry-run-out", default=g_args.dry_run, help="dry run 时是否输出文件")
    parser.add_argument('-e', '--out-ext', default=g_args.out_ext, help=f'输出文件的编码，默认{g_args.out_ext}')
    args:GArgs = parser.parse_args()
    g_args = args

    # 获取待处理文件列表
    files:list[str]=[]
    for input_path in g_args.input_path:
        if os.path.isfile(input_path) and input_path.lower().endswith(g_exts):
            files.append(input_path)
        elif os.path.isdir(input_path):
            t_files = find_mp4_files(input_path)
            files.extend(t_files)
    if not files:
        logging.warning(f"错误：未找到1个需要处理的视频文件")
        sys.exit(0)

    # 创建输出目录（如果不存在）
    os.makedirs(g_args.out_dir, exist_ok=True)
    if not os.path.isdir(g_args.out_dir):
        logging.error(f"out_dir is not dir {args.out_dir}")
        sys.exit(1)

    # 批量处理
    logging.info(f"找到 {len(files)} 个视频文件\n")
    for file in files:
        process_file(file, g_args.out_dir)
        print("")
    logging.info("全部处理完成")

if __name__ == "__main__":
    main()