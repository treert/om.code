import argparse
import atexit
from dataclasses import dataclass, field
from datetime import datetime
import json
import logging
import math
import os
from pathlib import Path
import re
import shlex
import subprocess
import sys
import time
from typing import IO, List

import colorlog

r"""
对于不同封装格式的处理
- 公共的部分:
    - encode 1 video to av1
    - encode 1 video to opus                                # 一些视频有多条音轨。想了想只选一条。
- mkv:
    - copy all subtitle, attachment, data
    - set -metadata:s:v:0 BPS-eng={bitrate*1000}           # 这个数据需要重新设置
    - set -metadata:s:a:0 BPS-eng={opus_bitrate*1000}
- webm:
    - encode audio to opus                                  # webm only support it
    - set BPS-eng like mkv
- mp4:
    - clear many metadata
    - 默认情况下，音频也转码成了 opus，刚开始时使用的 copy

各个封装格式的问题
- mkv: 
    - 编码后元数据好多都是错误的。而且还不能自动修复。【蛋疼的是元数据里有视频音频码率】
- webm: mkv简化版
    - 和mkv问题一样，元数据好些是错误的。
    - 音频得编码成 opus (aac有专利)
- mp4: 
    - 字幕，章节信息之类的有问题。（视频，音频码率没问题，这些不是以元数据的方式存在）
        - copy audio 时，音频码率可能也是错误的。
    - 对音频的支持貌似有兼容问题（大公司利益斗争导致），最好编码成 aac
        

mkv 和 webm 没有把码率信息直接编码进流中，而是用元数据的方法补充的。对于直播来说很对，播放中途是可以修改码率的。

在元数据这边折腾好久，有点想直接删掉好了。

本来想默认使用 mkv，因为想保留字幕，mp4 对字幕的支持很拉。
后来想想现在有AI语音识别和翻译了，似乎没必要还特别保留原始字幕。

---------------------- 遇到的一些问题
1. 可变帧率mp4 编码后的文件播放时周期的有卡顿感。豆包最后给了方案，先转换成恒定帧率的视频。Bash 脚本
ffmpeg -i input_vfr.mp4 \ 
  -r $(ffprobe -v error -select_streams v:0 -show_entries stream=avg_frame_rate -of default=noprint_wrappers=1:nokey=1 input_vfr.mp4) \ 
  -c:v copy \ 
  -af aresample=async=1 -c:a aac \  # 让声音和画面同步，对于 可变帧率的wmv可能有用 
  output_cfr.mp4

----------------------- 码率
这个工具只能起到压缩的作用，输入的低品质视频，输出还是低品质的。码率高是高品质的必要条件，但不充分。
确定码率的方案搜索 determine_bitrate_for_file

这儿列一个比例表。
1080p   AV1     H265    H264    
AV1     100     130     250
H265    75      100     180
H264    40      55      100

1080p 60fps 比 30fps 要 +40% 作用。不过对于这个工具来说没影响。

PS: 下载的一些 1080p 的视频 h265编码，码率不足1000k，效果还可以，就离谱。

----------------------- 其他
- transcode_video 函数，可以看看 ffmpeg 的使用方式
- determine_bitrate_for_file 函数，可以看到 ffprobe 的使用方式。
"""

determine_bitrate_for_file_help =  """
    根据 set_bitrate 确定视频码率，单位是 kbps。，多种模式
    >0: 用户设置的码率
    =0: 留给 av1_nvenc 来决定，目前看上去就是 2000 
    -1: determine_bitrate_by_height
    -2: 根据输入视频编码方式确定一个压缩比例
"""

@dataclass
class GArgs:
    input_path:list[str] = field(default_factory=list)
    out_dir:str = '.'
    set_bitrate: int = -2           # determine_bitrate_for_file_help
    set_opus_bitrate: int = 128     # 强制使用 opus 编码音频。默认如此吧，体积小，反正我听不出来。
    test_time:int = 0
    dry_run: float = 0
    dry_run_out: bool = False
    overwrite: bool = False
    out_ext:str = '.mp4'            # 默认封装格式
    log_debug:bool = False
    faststart:bool = False          # -movflags faststart，缺点：1. ctrl+c 中断编码，mp4损坏。2. 可能多一次pass,对磁盘有峰值压力【似乎没什么好处呢】

g_args = GArgs()
g_support_input_exts = ('.mp4', '.mkv', '.avi', '.rmvb', '.wmv', '.mov', '.webm')
g_support_output_exts = [".mp4", ".mkv", ".webm"]
# 高级编码，默认这种输入文件不处理
g_hq_codec = ['av1'] 
# g_hq_codec = ['av1','hevc'] 

# 配置日志系统
# logging.basicConfig(
#     level=logging.DEBUG,  # 设置日志级别
#     format='%(asctime)s - %(levelname)s - %(message)s',
#     stream=sys.stdout,# 默认是 stderr
# )
logger = colorlog.getLogger()
def setup_colored_logger():
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
    # console handler
    console = colorlog.StreamHandler(sys.stdout) # 不传参数，会输出到 sys.stderr 里
    console.setFormatter(formatter)
    
    logger.setLevel(logging.INFO)
    if g_args.log_debug:
        logger.setLevel(logging.DEBUG)
    logger.addHandler(console)


def format_cmds(cmds:list[str]):
    """格式化命令数组，主要是对参数做空格分割。"""
    ret = []
    for it in cmds:
        if it.startswith('-'):
            ret.extend(it.split())  # 如果有错误的空格，可能会凉。不要那么做就行。
        else:
            ret.append(it)
    return ret

def get_video_codec(input_file):
    """使用 ffprobe 检测视频编码格式"""
    # ffprobe -v error -select_streams v:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1
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
        logger.debug(f"get_video_codec {codec}")
        return codec
    except subprocess.CalledProcessError as e:
        logger.error(f"错误：无法检测视频编码 - {e}")
        sys.exit(1)

def get_video_bitrate(input_file):
    # ffprobe -v error -select_streams v:0 -show_entries stream=bit_rate -of json
    cmd = [
        'ffprobe',
        '-v', 'error',
        '-select_streams', 'v:0',
        '-show_entries', 'stream=bit_rate',
        '-of', 'json', # 换成上面的 default=noprint_wrappers=1:nokey=1 也可以
        input_file
    ]
    
    result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    output = json.loads(result.stdout)
    
    try:
        bitrate = int(output['streams'][0]['bit_rate'])
        return bitrate // 1000
    except (KeyError, IndexError):
        return None

def get_video_height(input_file):
    """使用 ffprobe 获取视频帧高度"""
    # ffprobe -v error -select_streams v:0 -show_entries stream=height -of default=noprint_wrappers=1:nokey=1
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
        logger.error(f"错误：无法获取视频高度 - {e}")
        sys.exit(1)

def get_video_duration(input_file):
    """使用ffprobe获取视频总时长（秒）"""
    # ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1
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


def determine_bitrate_for_file(input_file) -> int:
    if g_args.set_bitrate > 0:
        return g_args.set_bitrate
    if g_args.set_bitrate == 0:
        return 0

    # ffprobe -v error -select_streams v:0 -show_entries stream -of json
    cmd = [
        'ffprobe',
        '-v', 'error',
        '-select_streams', 'v:0',
        '-show_entries', 'stream',
        '-of', 'json',
        input_file
    ]
    
    try:
        result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        output = json.loads(result.stdout)
        info = output['streams'][0]
        if g_args.set_bitrate == -1:
            height = int(info['height'])
            return determine_bitrate_by_height(height)
        if g_args.set_bitrate == -2:
            codec_name = str(info['codec_name']).lower()
            tgt_bit_rate = None     # 故意这样，方便触发异常
            bit_rate = None         # 故意这样，方便触发异常
            if g_args.out_ext == '.mp4':
                bit_rate = int(info['bit_rate'])
            else: # 估算码率
                file_size = os.path.getsize(input_file)
                duration = get_video_duration(input_file)
                bit_rate = int(file_size/duration*8)
                bit_rate = bit_rate - min(256, bit_rate // 4)*1000 # 剔除音频
                # print(f"file_size={file_size}, duration={duration}, bit_rate={bit_rate}")
                
            if codec_name == 'hevc':
                tgt_bit_rate = bit_rate * 0.8
            elif codec_name == 'h264':
                tgt_bit_rate = bit_rate * 0.4
            elif codec_name == 'vc1':
                tgt_bit_rate = bit_rate * 0.3
            
            return int(tgt_bit_rate / 1000)
    except Exception as e:
        logger.error(f"错误：无法确定输出视频码率 set_bitrate={g_args.set_bitrate} - {e}")
        sys.exit(1)

def determine_bitrate_by_height(height):
    """根据视频高度确定目标码率（单位：kbps）"""

    # Netflix AV1 推荐码率表​
    if height <= 480:
        return 500    # 480p 或更低
    elif height <= 720:
        return 1200   # 720p
    elif height <= 1080:
        return 2500   # 1080p 最早设置的 3000k
    elif height <= 1440:
        return 5000   # 2K
    else:
        return 8000   # 4K 或更高

def print_pipe(stream:IO[bytes]):
    """输出流内容，支持进度条, 这个函数先保留吧"""
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

def transcode_video(input_file, output_file, bitrate:int):
    """使用 ffmpeg 进行转码（显示实时输出）"""
    # 下面的这些设置需要搭配 av1_nvenc 使用
    cmds = [
        'ffmpeg',
        ## 全局参数
        '-y',                                           # 覆盖输出文件（无需确认）
        # '-loglevel warning',                            # 设置日志级别
        '-hide_banner',                                 # 隐藏 FFmpeg 版本信息
        '-stats',                                       # 显示进度统计信息。会把进度信息输出到 stdout 里，默认是 stderr
        # '-ignore_unknown',                              # 用途优先，只是 mp4 编码 hdmv_pgs_subtitle 时还是报错

        '-i', input_file,
        ### 切片参数
        # 切片的同时转码，可能导致输出的视频周期性的卡顿。
        # '-ss 00:01:00',                                 # 开始时间 说法: -i 之前，关键帧定位，速度更快。【但是输入视频是可变帧率时会出现卡顿现象（放后面也不行，头大）】
        # '-t 60.5'                                       # 持续时间 推荐放在后面，逐帧定位​​​，更精确。
        # '-to 00:02:00.500',                             # 结束时间 to = ss + t, 和 t 冲突

        ### 一些没用的参数
        # '-progress pipe:1',                           # 效果不佳
        # '-analyzeduration 200M',                      # 允许 FFmpeg 分析长达 analyzeduration 的数据来检测流信息
        # '-probesize 100M',                            # 允许 FFmpeg 读取前 probesize 的数据来探测格式

        ## 元数据

        ### copy metadata 折腾一圈，对于转码无用，转码后的一些元信息要变化的
        # '-map_chapters 0',                            # 复制所有章节数据 黑客帝国动画版原始有两个 Menu, 对比元数据，发现就是 Chapter数据，但是这个指令没有用
        # '-map_metadata 0',                            # 复制全局元数据
        # '-map_metadata:s:v 0:s:v',                    # 复制视频流元数据
        # '-map_metadata:s:a 0:s:a',                    # 复制音频流元数据
        # '-map_metadata:s:s 0:s:s',                    # 复制字幕流元数据 如果没有数据，会报错，并且不能使用?来标记可选
        # '-map_metadata:s:d 0:s:d',                    # 复制字幕流元数据

        ### set metadata
        # '-map_chapters -1',                             # 剔除所有章节数据 ffprobe mp4 可能报错，算了，不要了
        # '-map_metadata -1',                             # 剔除所有元数据
        '-metadata', f'title={Path(output_file).stem}',   # 标题 有些title里有乱码，全部设置成文件名好了。放在
        # '-metadata', f'artist=one001',                    # 作者
        # '-fflags +genpts -write_tmcd 0',                # 原来想用来修复 mkv 元数据的，实际没用. +genpts 好像可以修复时间戳
    ]

    # cmds.append('-map_metadata -1') # 愉快的决定了，删掉

    ## 修正一些元数据
    if g_args.out_ext == '.mp4':
        # cmds.append('-map_metadata -1')   # 清掉元数据 【有必要保留吗？】
        cmds.append('-map_metadata:s:v -1')
        cmds.append('-map_metadata:s:a -1')
        cmds.append('-map_chapters -1')   # 清掉章节数据。黑客帝国动画电影有两种章节数据，编码成 mp4 后 ffprobe 有个小错误输出 
        pass
    # if g_args.out_ext in ('.mkv','.webm'):
    if True: # 不管什么类型，都设置下，也没啥坏处。
        # mkv 的视频码率显示不准，必要时直接设置下
        if bitrate > 0:
            cmds.extend([
                f'-metadata:s:v:0 BPS-eng={bitrate*1000}',
            ])
        ## 下面的这些命令实际会删掉所有元信息，而不是指定类型的。
        # cmds.append('-map_metadata:s:v -1')     # 需要重新编码视频
        # if g_args.out_ext == '.webm':
        #     cmds.append('-map_metadata:s:a -1') # webm 还需要额外编码音频
        # cmds.append('-map_metadata 0') # 不管放在哪里都没用

    # 只测试编码前几秒
    if g_args.test_time > 0:
        cmds.append(f'-t {g_args.test_time}')

    ## 按顺序复制 Stream

    ### 视频流
    cmds.extend([
        '-map 0:v:0',                                   # 复制视频 只一个
        # '-map 0:v',                                     # 选择所有的视频流
        # '-pix_fmt yuv420p',                             # conver流使用的像素格式（如 yuvj420p）已被弃用，需要这个，才不报错
        # '-force_key_frames 00:00:01',                   # 想增加个预览封面的，但是没有用。安装了 k-lite 就有了。
        '-c:v av1_nvenc',                               # -c 这类的是输出选项，按理来说应该放在最后面，不过感觉放在这儿更好
        '-multipass qres',                              # disabled(default),qres,fullres 好像有点用，这样设置不会变慢多少
        # '-rc vbr',                                      # -1(default), constqp,vbr,cbr
        # '-cq 23',                                        # default 0。仅在 -rc constqp 下生效。 23 码率 10M，太大了
        '-preset p7',                                   # 最高质量 速度会慢一点，不影响码率
    ])
    if bitrate > 0:
        cmds.extend([
            f'-b:v {bitrate}k',                         # 如果不设置，不知道 av1_nvenc 是怎么决定 bitrate 的，小文件输出反而变大了。
            # f'-minrate {bitrate}k',
            # f'-maxrate {int(1.2*bitrate)}k',            # 对于直播或者在线视频有用，避免码率峰值太高。【ai 推荐的这个太稳定了吧】
            # f'-bufsize {int(2.4*bitrate)}k',            # maxrate 可以持续的时间是 {bufsize/maxrate}s
        ])
    
    ### 音频流 只复制一条音频流
    if g_args.set_opus_bitrate > 0:
        cmds.extend([
                f'-map 0:a:0 -c:a libopus -b:a {g_args.set_opus_bitrate}k',
                f'-metadata:s:a:0 BPS-eng={g_args.set_opus_bitrate*1000}',
            ])
    else:
        if g_args.out_ext == '.webm':
            t_opus_bitrate = 128
            cmds.extend([
                f'-map 0:a:0 -c:a libopus -b:a {t_opus_bitrate}k',        # webm 强制使用 opus
                f'-metadata:s:a:0 BPS-eng={t_opus_bitrate*1000}',
            ])
        elif g_args.out_ext == '.mp4':
            cmds.extend([
                '-map 0:a:0 -c:a copy', # 按理来说应该用 aac
            ])
        else:
            cmds.extend([
                '-map 0:a:0 -c:a copy',
            ])

    ### mkv 复制 其他流 
    if g_args.out_ext == '.mkv':
        cmds.extend([
            '-map 0:s? -c:s copy',                      # 复制字幕
            '-map 0:t? -c:t copy',                      # 复制附件
            '-map 0:d? -c:d copy',                      # 复制数据
        ])
    else:
        # 忽略所有其他数据
        # cmds.extend([
        #     '-sn',
        #     '-dn',
        #     '-map -0:t',
        # ])
        # 其他格式以安全的方式复制字幕，其实就只有 mp4
        # if g_args.out_ext == Path(input_file).suffix:
        #     cmds.append('-map 0:s? -c:s copy')
        pass

    ## 输出文件的参数

    ### 在 mp4 开头放置 moovinfo. 据说可以加快播放开始速度。测试下来，本地播放和在共享服务器上播放没什么变化，都很快。
    ### 副作用：需要在编码完成后，再次移动所有数据，然后在开头插入 moov info，这样磁盘存在IO问题。就不开启了。（也没测试过）
    if g_args.out_ext == '.mp4' and g_args.faststart:
        cmds.extend([
            '-movflags faststart',                       # move moov info to head. [ctrl+c 中断后文件不完整不能播放了。]
            # '-movflags +faststart',                      # 在文件开头冗余一份 moov info
        ])

    ## 设置输出文件
    if g_args.dry_run > 0 and not g_args.dry_run_out:
        cmds.append('-f null -')
    else:
        cmds.append(output_file)

    ## call ffmpeg
    cmds = format_cmds(cmds)
    try:
        print(" ".join(cmds))
        print("")
        mode = 1
        if mode == 2: # 最简单的方案。把子进程的输出定向到当前进程
            try:
                process = subprocess.run(
                    cmds,
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
                logger.info(f"Ctrl+C KeyboardInterrupt")
                sys.exit(1)
        elif mode == 1: # 文本模式，需要自己处理进度条
            # 第一次尝试
            process:subprocess.Popen = None
            is_timeout = False
            # 获取视频总时长
            total_duration = get_video_duration(input_file)

            # 正则表达式匹配进度信息
            time_pattern = re.compile(r'time=(\d+:\d+:\d+\.\d+|N/A)') # time may be N/A

            process = subprocess.Popen(
                cmds,
                stderr=subprocess.STDOUT,
                stdout=subprocess.PIPE,
                universal_newlines=True, # 没法处理进度条的情况，\r 会被转换成 \n
                encoding='utf-8',
                errors='ignore',        # 忽略字符编码错误
                bufsize=1,
            )
            atexit.register(lambda: process.terminate()) # 避免意外情况
            try:
                start_time = time.time()  # 记录开始时间
                last_is_time_line = False
                sys.stdout.flush()
                for line in process.stdout:
                    line:str
                    line = line.strip('\n')
                    # 尝试匹配进度信息
                    time_match = time_pattern.search(line)
                    if time_match:
                        last_is_time_line = True
                        current_time = time_match.group(1)
                        current_seconds = time_to_seconds(current_time)
                        
                        # 计算百分比
                        percent = min(100, (current_seconds / total_duration) * 100)
                        
                        # 在原始进度信息前添加百分比
                        sys.stdout.write(f"\r[{percent:3.0f}%] {line}")
                        sys.stdout.flush()
                    else:
                        if last_is_time_line:
                            sys.stderr.write('\n')
                            last_is_time_line = False
                        if g_args.log_debug:
                            # 普通日志信息直接输出
                            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S ")
                            sys.stderr.write(current_time)
                            sys.stderr.write(line)
                            sys.stderr.write('\n')
                            sys.stderr.flush()
                    if g_args.dry_run > 0:
                        now = time.time()
                        if now - start_time > g_args.dry_run:
                            is_timeout = True
                            process.terminate()
                            break
                if last_is_time_line:
                    sys.stderr.write('\n')
            except KeyboardInterrupt:
                sys.stdout.write('\n')
                logger.info(f"Ctrl+C KeyboardInterrupt")
                sys.exit(1)
            process.wait()
            if not is_timeout and process.returncode != 0:
                raise subprocess.CalledProcessError(process.returncode, cmds)
        else:
            assert(False)
            pass

        logger.info(f"转码完成：{output_file}")
    except subprocess.CalledProcessError as e:
        logger.error(f"错误：转码失败 - 返回码 {e.returncode}")
        sys.exit(1)

def find_mp4_files(directory: str) -> List[str]:
    """递归查找目录下的所有MP4文件"""
    mp4_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.lower().endswith(g_support_input_exts):
                mp4_files.append(os.path.join(root, file))
    return mp4_files

def process_file(input_file: str, out_dir: str):
    """处理单个视频文件"""
    # 检测编码格式
    codec = get_video_codec(input_file)
    if not g_args.overwrite and (codec in g_hq_codec):
        logger.info(f"跳过：{input_file} 已是高级编码:{codec}")
        return False

    # 获取输出视频码率
    bitrate = determine_bitrate_for_file(input_file)

    # 生成输出路径
    filename_without_ext = Path(input_file).stem.upper()
    output_file = os.path.join(out_dir, f"{filename_without_ext}{g_args.out_ext}")
    if not g_args.overwrite and os.path.exists(output_file):
        logger.warning(f"跳过：{input_file}. output_file exsit {output_file}")
        return False

    # 执行转码
    logger.info(f"start_encode：{input_file}({codec}, to {bitrate}kbps). Press Ctrl+C to interrupt.")
    transcode_video(input_file, output_file, bitrate)
    return True

def main():
    setup_colored_logger()
    global g_args
    parser = argparse.ArgumentParser(description="AV1 批量转码工具")
    parser.add_argument("input_path", nargs='+', help="输入文件或目录路径")

    def add_option(*args, default:bool, action:str='nil',**kwargs):
        help = kwargs.get('help','')
        if default:
            kwargs['help'] = f"(default True, set False) {help}"
            parser.add_argument(*args, action='store_false', **kwargs)
        else:
            kwargs['help'] = f"(default False, set True) {help}"
            parser.add_argument(*args, action='store_true', **kwargs)

    parser.add_argument('-o', "--out-dir", default=g_args.out_dir, help="输出目录（默认当前目录）")
    add_option('-f', "--overwrite", default=g_args.overwrite, help="是否强制转码，无视：输入文件是高级编码，输出文件已经存在")
    parser.add_argument('-b', "--set-bitrate", type=int, default=g_args.set_bitrate, help=f"{determine_bitrate_for_file_help}(default {g_args.set_bitrate})")
    parser.add_argument('-ab', "--set-opus-bitrate", type=int, default=g_args.set_opus_bitrate, help=f"opus 的码率，如果设置了就会使用，webm文件会强制使用 (default {g_args.set_opus_bitrate})")
    parser.add_argument('-t', "--test-time", type=int, default=g_args.test_time, help=f"只编码文件的前几秒 (default {g_args.test_time})")
    parser.add_argument('-n', "--dry-run", type=float, default=g_args.dry_run, help="kill ffmpeg if timeout. output to null.")
    add_option('-no', "--dry-run-out", default=g_args.dry_run_out, help="output to file when dry-run")
    add_option('-d', "--log-debug", default=g_args.log_debug, help="是否输出 debug 信息")
    add_option('-fs', "--faststart", default=g_args.faststart, help="是否开启 moov faststart")
    parser.add_argument('-e', '--out-ext', type=str, choices=g_support_output_exts,default=g_args.out_ext, help=f'输出文件的封住格式，默认{g_args.out_ext}')
    args:GArgs = parser.parse_args()
    g_args = args

    # 获取待处理文件列表
    files:list[str]=[]
    for input_path in g_args.input_path:
        if os.path.isfile(input_path) and input_path.lower().endswith(g_support_input_exts):
            files.append(input_path)
        elif os.path.isdir(input_path):
            t_files = find_mp4_files(input_path)
            files.extend(t_files)
    if not files:
        logger.warning(f"错误：未找到1个需要处理的视频文件")
        sys.exit(0)

    # 创建输出目录（如果不存在）
    os.makedirs(g_args.out_dir, exist_ok=True)
    if not os.path.isdir(g_args.out_dir):
        logger.error(f"out_dir is not dir {args.out_dir}")
        sys.exit(1)

    # 批量处理
    total_cnt = len(files)
    logger.info(f"找到 {total_cnt} 个视频文件\n")
    cnt = 0
    for idx, file in enumerate(files):
        logger.info(f"process {idx+1}/{total_cnt}: {file}")
        ok = process_file(file, g_args.out_dir)
        cnt += int(ok)
    logger.info(f"全部处理完成. {cnt}/{total_cnt}")
    logger

if __name__ == "__main__":
    main()