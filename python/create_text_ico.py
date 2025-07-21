import argparse
from PIL import Image, ImageDraw, ImageFont

def create_text_ico(text, output_path="icon.ico", size=(64, 64), font_size=40, bg_color="white", text_color="black"):
    # 创建空白图像
    img = Image.new("RGB", size, bg_color)
    draw = ImageDraw.Draw(img)
    
    # 加载字体（默认使用系统字体，或指定路径）
    try:
        font = ImageFont.truetype("simhei.ttf", font_size)
    except:
        font = ImageFont.load_default()  # 备用默认字体

    # 获取文本框大小
    # 从 Pillow 9.2.0 开始，可以使用 getbbox()
    try:
        bbox = draw.textbbox((0, 0), text, font=font)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]
    except AttributeError:
        # For older Pillow versions
        text_width, text_height = draw.textsize(text, font=font)

    # 计算文字位置（居中）
    position = ((size[0] - text_width) // 2, (size[1] - text_height) // 2)
    
    # 绘制文字
    draw.text(position, text, fill=text_color, font=font)
    
    # 保存为 ICO 文件（支持多尺寸）
    img.save(output_path, format="ICO", sizes=[(size[0], size[1])])

    print(f"ICO 文件 '{output_path}' 已成功创建。")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="创建文本图标")
    parser.add_argument("text", help="输入文件或目录路径")
    parser.add_argument("-o", "--output", help="输出文件路径", default="icon.ico")
    parser.add_argument("-s", "--size", help="图标尺寸", default=64, type=int)
    parser.add_argument("-f", "--font_size", help="字体大小", default=0, type=int)
    parser.add_argument("-b", "--bg_color", help="背景颜色", default="white")
    parser.add_argument("-t", "--text_color", help="文字颜色", default="black")

    args = parser.parse_args()

    if args.font_size == 0:
        cnt = len(args.text)
        if cnt == 1:
            args.font_size = args.size * 56 // 64
        else:
            args.font_size = args.size // cnt

    create_text_ico(args.text, 
                    output_path=args.output, 
                    size=(args.size, args.size), 
                    font_size=args.font_size,
                    bg_color=args.bg_color, 
                    text_color=args.text_color
                    )
