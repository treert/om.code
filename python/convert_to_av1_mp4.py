import argparse
import logging
import os
from pathlib import Path
import subprocess
import sys
from typing import IO, List

import colorlog

"""
代码基本是AI生成的。对话反馈调试花了大概4个小时，还是不熟练呀。
"""

# 配置日志系统
# logging.basicConfig(
#     level=logging.DEBUG,  # 设置日志级别
#     format='%(asctime)s - %(levelname)s - %(message)s'
# )

def setup_colored_logging():
    """配置彩色日志输出"""
    formatter = colorlog.ColoredFormatter(
        '%(log_color)s%(asctime)s %(levelname)-8s %(message)s%(reset)s',
        datefmt='%Y-%m-%d %H:%M:%S',
        log_colors={
            'DEBUG': 'cyan',
            'INFO': 'green',
            'WARNING': 'yellow',
            'ERROR': 'red',
            'CRITICAL': 'red,bg_white',
        },
        reset=True,
        style='%'
    )
    
    handler = colorlog.StreamHandler()
    handler.setFormatter(formatter)
    
    logger = colorlog.getLogger()
    logger.setLevel(logging.INFO)
    # logger.setLevel(logging.DEBUG)
    logger.addHandler(handler)

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
        logging.debug(f"get_video_height {cmd_out}")
        height = int(cmd_out)
        return height
    except subprocess.CalledProcessError as e:
        logging.error(f"错误：无法获取视频高度 - {e}")
        sys.exit(1)

def determine_bitrate(height):
    """根据视频高度确定目标码率（单位：kbps）"""
    if height <= 480:
        return 1000   # 480p 或更低
    elif height <= 720:
        return 2000   # 720p
    elif height <= 1080:
        return 3000   # 1080p
    elif height <= 1440:
        return 5000   # 2K
    else:
        return 8000   # 4K 或更高


def print_pipe(stream:IO[bytes]):
    """支持进度条"""
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
            sys.stdout.buffer.write(buffer)
            sys.stdout.buffer.write(byte)
            sys.stdout.flush()
            buffer.clear()  # 清空 buffer
        else:
            buffer.extend(byte)  # 累积字节
    pass

def transcode_video(input_file, output_file, bitrate):
    """使用 ffmpeg 进行转码（显示实时输出）"""
    cmd = [
        'ffmpeg',
        '-i', input_file,
        '-c:a', 'copy',
        '-c:v', 'av1_nvenc',
        '-rc', 'vbr', # 默认就是这样。其实没有必要
        '-b:v', f'{bitrate}k',
        '-preset', 'p7',
        '-y',
        output_file
    ]
    try:
        # 实时显示 ffmpeg 输出
        print(" ".join(cmd))
        print("")
        process = subprocess.Popen(
            cmd,
            stderr=subprocess.STDOUT,
            stdout=subprocess.PIPE,
            universal_newlines=False,  # 关闭文本模式 保留 \r（回车符）或其它控制字符（如进度条）
            bufsize=0,
        )

        try:
            print_pipe(process.stdout)
        except KeyboardInterrupt:
            logging.info(f"\nCtrl+C KeyboardInterrupt")
            process.terminate()
        
        # process = subprocess.Popen(
        #     cmd,
        #     stderr=subprocess.STDOUT,
        #     stdout=subprocess.PIPE,
        #     universal_newlines=True, # 没法处理进度条的情况，\r 会被转换成 \n
        # )
        # for line in process.stdout:
        #     print(line.strip())
        #     pass
        
        process.wait()
        if process.returncode != 0:
            raise subprocess.CalledProcessError(process.returncode, cmd)
        
        logging.info(f"\n转码完成：{output_file}")
    except subprocess.CalledProcessError as e:
        logging.error(f"\n错误：转码失败 - 返回码 {e.returncode}")
        sys.exit(1)

def find_mp4_files(directory: str) -> List[str]:
    """递归查找目录下的所有MP4文件"""
    mp4_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.lower().endswith(('.mp4','.mkv','.avi','.wmv','.mov')):
                mp4_files.append(os.path.join(root, file))
    return mp4_files

def process_file(input_file: str, out_dir: str):
    """处理单个视频文件"""
    # 检测编码格式
    codec = get_video_codec(input_file)
    if codec == 'av1':
        logging.info(f"跳过：{input_file} 已是 AV1 编码")
        return

    # 获取视频参数
    height = get_video_height(input_file)
    bitrate = determine_bitrate(height)

    # 生成输出路径
    filename_without_ext = Path(input_file).stem
    output_file = os.path.join(out_dir, f"{filename_without_ext}.mp4")
    # output_file = os.path.join(out_dir, f"{filename_without_ext}.mkv")

    # 执行转码
    logging.info(f"\n开始转码：{input_file}(视频高度: {height}p, 目标码率: {bitrate}kbps). Press Ctrl+C to interrupt.")
    transcode_video(input_file, output_file, bitrate)

def main():
    parser = argparse.ArgumentParser(description="AV1 批量转码工具")
    parser.add_argument("input_path", help="输入文件或目录路径")
    parser.add_argument("--out_dir", help="输出目录（默认当前目录）", default=".")
    args = parser.parse_args()

    # 检查输入路径是否存在
    if not os.path.exists(args.input_path):
        logging.error(f"错误：路径不存在 - {args.input_path}")
        sys.exit(1)

    # 获取待处理文件列表
    if os.path.isfile(args.input_path):
        files = [args.input_path]
    else:
        files = find_mp4_files(args.input_path)
        if not files:
            logging.error(f"错误：目录中未找到视频文件 - {args.input_path}")
            sys.exit(1)

    # 创建输出目录（如果不存在）
    os.makedirs(args.out_dir, exist_ok=True)
    if not os.path.isdir(args.out_dir):
        logging.error(f"out_dir is not dir {args.out_dir}")
        sys.exit(1)

    # 批量处理
    logging.info(f"找到 {len(files)} 个MP4文件\n")
    for file in files:
        process_file(file, args.out_dir)
        print("")
    logging.info("全部处理完成")

if __name__ == "__main__":
    main()