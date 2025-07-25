import logging
import os
import argparse
import subprocess
from pathlib import Path
import sys

import colorlog

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
    
    logger.setLevel(logging.DEBUG)
    logger.addHandler(console)

setup_colored_logger()

def extract_subtitles(input_paths, output_dir=None, sub_idx=0):
    """
    从视频文件中提取字幕
    
    参数:
        input_paths (list[str]): 视频文件路径或目录
        output_dir (str): 输出目录，默认为视频所在目录
        sub_idx (int): 要提取的字幕流索引，默认为0
    """
    # 获取待处理文件列表
    video_files:list[str]=[]
    for input_path in input_paths:
        if os.path.isdir(input_path):
            # 如果是目录，递归处理所有视频文件
            for root, _, files in os.walk(input_path):
                for file in files:
                    if is_video_file(file):
                        video_path = os.path.join(root, file)
                        video_files.append(video_path)
        else:
            # 处理单个视频文件
            if is_video_file(input_path):
                video_files.append(input_path)
    
    if not video_files:
        logger.warning(f"错误：未找到1个需要处理的视频文件")
        sys.exit(0)

    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
        if not os.path.isdir(output_dir):
            logger.error(f"out_dir is not dir {output_dir}")
            sys.exit(1)
    
    # 批量处理
    total_cnt = len(video_files)
    logger.info(f"找到 {total_cnt} 个视频文件\n")
    cnt = 0
    for idx, file in enumerate(video_files):
        logger.info(f"process {idx+1}/{total_cnt}: {file}")
        ok = process_video(file, output_dir, sub_idx)
        cnt += int(not not ok)
    logger.info(f"全部处理完成. {cnt}/{total_cnt}")

def is_video_file(filename):
    """检查文件是否是支持的视频格式"""
    video_extensions = ['.mp4', '.mkv', '.webm', '.avi', '.mov', '.flv']
    return os.path.splitext(filename)[1].lower() in video_extensions

def get_subtitle_streams(video_path):
    """获取视频中的所有字幕流信息"""
    try:
        cmd = [
            'ffprobe',
            '-v', 'error',
            '-select_streams', 's',
            '-show_entries', 'stream=index,codec_name:stream_tags=language,title',
            '-of', 'csv=p=0',
            video_path
        ]
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        
        streams = []
        for line in result.stdout.splitlines():
            parts = line.split(',')
            if len(parts) >= 2:
                stream_info = {
                    'index': parts[0],
                    'codec': parts[1],
                    'language': parts[2] if len(parts) > 2 else 'und',
                    'title': parts[3] if len(parts) > 3 else ''
                }
                streams.append(stream_info)
        
        return streams
    except subprocess.CalledProcessError as e:
        logger.error(f"获取 {video_path} 的字幕流信息失败: {e.stderr}")
        return []
    except Exception as e:
        logger.error(f"获取 {video_path} 的字幕流信息时发生未知错误: {str(e)}")
        return []

def process_video(video_path, output_dir, idx):
    """处理单个视频文件"""
    video_path = os.path.abspath(video_path)
    video_dir, video_filename = os.path.split(video_path)
    video_name, video_ext = os.path.splitext(video_filename)
    
    logger.info(f"处理视频文件: {video_path}")
    
    # 获取所有字幕流信息
    subtitle_streams = get_subtitle_streams(video_path)
    
    if not subtitle_streams:
        logger.warning("警告: 该视频没有找到任何字幕流")
        return
    
    # 打印字幕流信息
    logger.debug(f"找到 {len(subtitle_streams)} 个字幕流:")
    for i, stream in enumerate(subtitle_streams):
        logger.debug(f"  [{i}] 索引: {stream['index']}, 格式: {stream['codec']}, "
              f"语言: {stream['language']}, 标题: {stream['title']}")
    
    # 检查请求的字幕流索引是否有效
    if idx < 0 or idx >= len(subtitle_streams):
        logger.error(f"错误: 无效的字幕流索引 {idx}, 有效范围是 0-{len(subtitle_streams)-1}")
        return
    
    # 设置输出目录
    if output_dir is None:
        output_dir = video_dir
    
    # 确定输出文件扩展名
    codec_name = subtitle_streams[idx]['codec']
    ext_map = {
        'subrip': '.srt',
        'ass': '.ass',
        'webvtt': '.vtt',
        'mov_text': '.srt',
        'hdmv_pgs_subtitle': '.sup',
        'dvd_subtitle': '.sub'
    }
    language = subtitle_streams[idx]['language']
    output_ext = ext_map.get(codec_name, '.srt')
    
    # 构建输出文件名
    output_filename = f"{video_name}.{language}{output_ext}"
    output_path = os.path.join(output_dir, output_filename)
    
    # 使用ffmpeg提取字幕
    try:
        cmd = [
            'ffmpeg',
            '-v', 'error',
            '-i', video_path,
            '-map', f'0:s:{idx}',
            '-c', 'copy',
            output_path
        ]
        logger.info(' '.join(cmd))
        subprocess.run(cmd, check=True)
        
        logger.info(f"成功提取字幕到: {output_path}")
        return True
        
    except subprocess.CalledProcessError as e:
        logger.error(f"提取字幕时出错: {e.stderr}")
    except Exception as e:
        logger.error(f"提取字幕时发生未知错误: {str(e)}")

def main():
    parser = argparse.ArgumentParser(description='从视频文件中提取字幕')
    parser.add_argument('input', nargs='+', help='视频文件路径或目录')
    parser.add_argument('--idx', type=int, default=0, 
                       help='要提取的字幕流索引 (默认: 0)')
    parser.add_argument('--output', help='输出目录 (默认: 视频所在目录)')
    
    args = parser.parse_args()
    extract_subtitles(args.input, output_dir=args.output, sub_idx=args.idx)

if __name__ == '__main__':
    main()