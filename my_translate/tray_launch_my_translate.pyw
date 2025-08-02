import atexit
import logging
import signal
import threading
import subprocess
import sys
import os
import time
import webbrowser
import colorlog
from pystray import Icon, Menu, MenuItem
from PIL import Image

# 获取当前脚本所在的目录路径
script_dir = os.path.dirname(os.path.abspath(__file__))
# 切换工作目录
os.chdir(script_dir)


logger = colorlog.getLogger()
def setup_colored_logger():
    """配置彩色日志输出"""
    console_formatter = colorlog.ColoredFormatter(
        '%(log_color)som-log: %(asctime)s %(levelname)-8s %(name)s: %(message)s%(reset)s',
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
    console_handler = colorlog.StreamHandler(sys.stdout) # 不传参数，会输出到 sys.stderr 里
    console_handler.setFormatter(console_formatter)

    log_file =  f"{os.path.abspath(__file__)}.log"
    file_handler = logging.FileHandler(log_file, encoding='utf-8', mode='a') # 'a' 表示追加模式
    file_formatter = logging.Formatter(
        '%(asctime)s %(levelname)-8s %(name)s: %(message)s'
    )
    file_handler.setFormatter(file_formatter)
    
    logger.setLevel(logging.INFO)
    # logger.setLevel(logging.DEBUG)
    logger.addHandler(console_handler)
    logger.addHandler(file_handler)


# 定义日志文件的路径
log_file = f"{os.path.abspath(__file__)}.log"
log_server_file = f"{os.path.abspath(__file__)}.server.log" # 和上面共用的话，日志会混乱

setup_colored_logger()
# 清空日志
with open(log_file, 'w', encoding='utf-8') as f:
    f.write(f"--- 服务器启动日志: {time.ctime()} ---\n")

# 假设您的服务器脚本是 server.py，并且在当前目录下
SERVER_SCRIPT = "my_translate_web.py"
# 存储服务器进程的引用
server_process = None


# 用于控制主线程退出的事件
stop_event = threading.Event()

def IsServerIsRunning():
    if server_process and server_process.poll() is None:
        return True
    else:
        return False

def cleanup_server():
    global server_process
    if IsServerIsRunning():
        logger.info("尝试关闭服务器...")
        try:
            # 这儿折腾了一天，下面注释的写法对于 pyw 启动的程序没有用。另外 subprocess.Popen 时使用 CREATE_NO_WINDOW 会收不到消息。
            # 但是发现如果 web server 是非debug模式。terminate 可以正常终止掉web服务器。这些注释先保留吧，虽然已经乱七八糟了。
            # 发送终止信号（Windows 专用）
            # server_process.send_signal(signal.CTRL_C_EVENT)    # 实测，没用
            # server_process.send_signal(signal.CTRL_BREAK_EVENT)  # 比 CTRL_C_EVENT 更可靠，实测有用
            server_process.terminate()
            server_process.wait(timeout=10)
        # except subprocess.TimeoutExpired:
        except Exception as e:
            logger.warning("服务器未能正常终止，尝试强制终止。")
            server_process.kill()
        finally:
            server_process = None
        logger.info("服务器已关闭。")
def start_server(icon_instance):
    global server_process
    if not IsServerIsRunning():
        logger.info("尝试启动服务器...")

        # cmd_exe = sys.executable
        cmd_exe = "python"
        flags = 0
        # CREATE_NEW_PROCESS_GROUP 允许控制信号只发送到子进程，不然会发送到整个进程树
        # flags |= subprocess.CREATE_NEW_PROCESS_GROUP
        flags |= subprocess.CREATE_NO_WINDOW # 使用了这个就收不到消息了。
        server_process = subprocess.Popen(
            [cmd_exe, SERVER_SCRIPT
            #  , '-d' # Flask 的开发模式，没办法终止掉web服务器
             ],
            stdout=open(log_server_file, 'w', encoding='utf-8'),
            stderr=subprocess.STDOUT,
            creationflags=flags,
        )
        logger.info("服务器已启动。")
        update_tray_menu(icon_instance) # 更新托盘菜单以显示新状态
    else:
        logger.info("服务器已经在运行中。")

def stop_server(icon_instance):
    if IsServerIsRunning():
        cleanup_server()
        update_tray_menu(icon_instance) # 更新托盘菜单以显示新状态
    else:
        logger.info("服务器未运行。")

def open_launch_log_window(icon_instance):
    # 使用 PowerShell 的 Get-Content -Wait 来实时显示日志文件
    # -Wait 相当于 Unix/Linux 的 tail -f
    # start powershell -NoExit -Command ... 会打开一个新的PowerShell窗口并执行命令
    cmd_command = f'start powershell -NoExit -Command "Get-Content -Path "{log_file}" -Wait"'
    subprocess.Popen(cmd_command, shell=True)
    logger.info(f"已打开新的CMD窗口，显示 {log_file} 日志。")

def open_server_log_window(icon_instance):
    """打开新的CMD窗口，显示server日志文件。"""
    cmd_command = f'start powershell -NoExit -Command "Get-Content -Path "{log_server_file}" -Wait"'
    subprocess.Popen(cmd_command, shell=True)
    logger.info(f"已打开新的CMD窗口，显示 {log_server_file} 日志。")

def open_log_directory(icon_instance):
    """打开日志文件所在的目录。"""
    log_dir = os.path.dirname(log_file)
    subprocess.Popen(f'explorer "{log_dir}"', shell=True)
    logger.info(f"已打开日志目录: {log_dir}")

def open_url_in_webview(icon_instance):
    webbrowser.open('http://localhost:5000/')

def quit_application(icon_instance):
    stop_server(icon_instance)
    icon_instance.stop() # 这会停止 pystray icon 线程
    stop_event.set() # 设置事件，通知主线程退出
    logger.info("应用程序已退出。")
    # os._exit(0) # 通常不再需要，因为我们现在有事件来控制主线程退出


def create_menu():
    """根据服务器状态创建并返回菜单项列表。"""
    server_is_running = IsServerIsRunning()
    menu_items = [
        MenuItem("启动服务器", start_server, enabled=not server_is_running),
        MenuItem("关闭服务器", stop_server, enabled=server_is_running),
        Menu.SEPARATOR,
        MenuItem("打开测试URL", open_url_in_webview),
        MenuItem("打开日志窗口", open_launch_log_window),
        MenuItem("打开server日志窗口", open_server_log_window),
        MenuItem("打开日志目录", open_log_directory),
        Menu.SEPARATOR,
        MenuItem("退出", quit_application) # 传入icon参数
    ]
    return menu_items

last_menu_server_is_running = None
def update_tray_menu(icon_instance):
    """更新系统托盘菜单，以反映最新的服务器状态。"""
    global last_menu_server_is_running
    server_is_running = IsServerIsRunning()
    if server_is_running != last_menu_server_is_running:
        last_menu_server_is_running = server_is_running
        if server_is_running:
            logger.info("update_tray_menu: 服务器已启动。")
        else:
            logger.info("update_tray_menu: 服务器已关闭。")
        # 重新设置菜单，pystray 会自动刷新
        icon_instance.menu = create_menu()

if __name__ == "__main__":
    icon_path = os.path.join(script_dir, "tray_icon.ico")
    try:
        image = Image.open(icon_path)
    except Exception as e:
        logger.error(f"无法加载图标文件: {icon_path}")
        sys.exit(1)

    # 注意这里创建 Icon 时，菜单最初可以是空的，或者直接调用 create_menu()
    icon = Icon(
        "my_translate_app",
        image,
        "My翻译服务器",
        menu=create_menu() # 初始菜单
    )

    logger.info("系统托盘图标已启动。在右键菜单里启动翻译服务器。")
    # icon.visible = True # 不要设置，因为 Icon.run() 会自动显示图标，这儿设置了反而会导致显示不出来

    # 在单独的线程中运行系统托盘图标
    icon_thread = threading.Thread(target=icon.run)
    icon_thread.daemon = True
    icon_thread.start()

    atexit.register(lambda: cleanup_server) # 避免意外情况

    # 保持主线程活跃，直到 stop_event 被设置，或者被 Ctrl+C 打断
    try:
        # 使用一个短的超时时间，例如 1 秒
        while not stop_event.is_set():
            update_tray_menu(icon)
            stop_event.wait(1) # 每秒检查一次事件是否被设置
            # 如果从 wait() 返回，但事件没被设置，说明是超时了，可以继续循环或检查其他条件
            # 此时如果按 Ctrl+C，KeyboardInterrupt 就会在这里（在 wait() 返回之后）被捕获
            pass
    except KeyboardInterrupt:
        logger.info("捕获到 Ctrl+C，正在退出应用程序...")
        quit_application(icon) # 调用退出函数，它会设置 stop_event
    finally:
        logger.info("主程序即将退出。")
