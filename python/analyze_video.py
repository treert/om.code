import argparse
import logging
import os
from pathlib import Path
import sys
import cv2
import numpy as np
import matplotlib.pyplot as plt

from skimage.metrics import structural_similarity as ssim

'''
用途优先，不能判断视频的质量，不过有点意思的样子。可以输出频谱图。
'''


# logging.basicConfig(
#     level=logging.INFO,
#     handlers=[logging.StreamHandler()],
#     format='%(asctime)s - %(levelname)s - %(message)s'
# )

# 1. 创建记录器
logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)  # 设置记录器级别

# 2. 创建处理器（这里使用控制台输出）
handler = logging.StreamHandler()
handler.setLevel(logging.DEBUG)  # 设置处理器级别

# 3. 创建格式器
formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
handler.setFormatter(formatter)

# 4. 将处理器添加到记录器
logger.addHandler(handler)

# deepseek 给的方法
def high_freq_energy_ratio(image:cv2.typing.MatLike):
    # 转换为灰度图
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    
    # 计算二维FFT
    dft = np.fft.fft2(gray)
    dft_shift = np.fft.fftshift(dft)
    magnitude = np.abs(dft_shift)  # 获取幅度谱（不要取对数）

    # 创建高频掩模（外围25%区域）
    h, w = magnitude.shape
    mask = np.ones((h, w), np.uint8)
    cv2.circle(mask, (w//2, h//2), int(min(h,w)*0.25), 0, -1)  # 中心区域置0

    # 计算能量（幅度平方）
    total_energy = np.sum(magnitude**2)
    high_freq_energy = np.sum((magnitude**2) * mask)  # 高频区域能量

    # 计算高频能量占比
    high_freq_ratio = high_freq_energy / (total_energy + 1e-10)  # 避免除零
    
    return high_freq_ratio

def analyze_video_spectrum(video_path, start_frame=0, to_frame=1<<55, num_frames=10, output_dir="output_frames"):
    """
    分析视频频谱特征
    :param video_path: 输入视频路径
    :param output_dir: 频谱图保存目录
    """
    # 创建输出目录
    import os
    os.makedirs(output_dir, exist_ok=True)

    base_name = Path(video_path).stem
    logger.info(f"start analyze {Path(video_path).name}")
    
    # 打开视频文件
    cap = cv2.VideoCapture(video_path)
    fps = cap.get(cv2.CAP_PROP_FPS)
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    
    logger.info(f"视频信息: {fps:.2f} FPS, 总帧数: {total_frames}, 准备分析帧: ss={start_frame} num={num_frames}")
    
    # 每10帧分析1次（避免冗余）
    sample_interval = 10
    frame_count = -1
    analyze_cnt = 0

    if start_frame > 0:
        cap.set(cv2.CAP_PROP_POS_FRAMES, start_frame)  # 跳到 start_frame 帧附近
        frame_count = start_frame - 1
    
    while cap.isOpened():
        frame_count += 1
        if analyze_cnt >= num_frames:
            break
        if frame_count > to_frame:
            break

        need_analyse = frame_count >= start_frame and (frame_count - start_frame) % sample_interval == 0
        if not need_analyse:
            ret = cap.grab()  # 只解码不渲染
            if not ret:
                break
            continue
        ret, frame = cap.read()
        if not ret:
            break

        analyze_cnt += 1
        # 转换为灰度图
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        
        # 计算二维FFT
        dft = np.fft.fft2(gray)
        dft_shift = np.fft.fftshift(dft)
        magnitude = 20 * np.log(np.abs(dft_shift) + 1e-10)  # 避免log(0)
        
        # 计算高频能量占比（外围25%区域）
        h, w = magnitude.shape
        mask = np.zeros((h, w), np.uint8)
        cv2.circle(mask, (w//2, h//2), int(h*0.25), 1, -1)
        high_freq_energy = np.mean(magnitude * mask)
        
        # 保存频谱分析结果
        plt.figure(figsize=(12, 6))
        
        plt.subplot(121)
        plt.imshow(gray, cmap='gray')
        plt.title(f"Original Frame {frame_count}")
        
        plt.subplot(122)
        plt.imshow(magnitude, cmap='gray')
        plt.title(f"Spectrum (HF Energy: {high_freq_energy:.2f} dB)")
        
        output_path = os.path.join(output_dir, f"{base_name}-frame_{frame_count}_spectrum.png")
        plt.savefig(output_path)
        plt.close()
        
        logger.info(f"Frame {frame_count}: High Freq Energy = {high_freq_energy:.2f} dB")
    
    cap.release()
    logger.info(f"分析完成！结果保存至: {output_dir}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="分析视频")
    parser.add_argument("input_path", help="输入文件")
    parser.add_argument('-o',"--output_dir", default="output_frames", help="输出的目录")
    parser.add_argument('-ss', '--start_frame', type=int, default=0)
    parser.add_argument('-to', '--to_frame', type=int, default=1<<55)
    parser.add_argument('-nf', '--num_frames', type=int, default=10)
    args = parser.parse_args()
    if not os.path.isfile(args.input_path):
        logger.error(f"miss file: {args.input_path}")
        sys.exit(0)
    
    analyze_video_spectrum(args.input_path, 
                           start_frame=args.start_frame, 
                           to_frame=args.to_frame, 
                           num_frames=args.num_frames,
                           output_dir=args.output_dir
                           )