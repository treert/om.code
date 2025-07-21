import atexit
import threading
import subprocess
import sys
import os
import time
from pystray import Icon, Menu, MenuItem
from PIL import Image

# 获取当前脚本所在的目录路径
script_dir = os.path.dirname(os.path.abspath(__file__))
# 切换工作目录
os.chdir(script_dir)

# 假设您的服务器脚本是 server.py，并且在当前目录下
SERVER_SCRIPT = "my_translate_web.py"
# 存储服务器进程的引用
server_process = None

# 定义日志文件的路径
LOG_FILE = os.path.join(script_dir ,f"{SERVER_SCRIPT}.log")

# 用于控制主线程退出的事件
stop_event = threading.Event()

# 用于表示服务器状态的全局变量
server_is_running = False

def cleanup():
    if server_process is not None:
        server_process.terminate()

atexit.register(lambda: cleanup) # 避免意外情况

def start_server():
    global server_process, server_is_running
    if server_process is None or server_process.poll() is not None:
        print("尝试启动服务器...")
        with open(LOG_FILE, 'w', encoding='utf-8') as f:
            f.write(f"--- 服务器启动日志: {time.ctime()} ---\n")

        # cmd_exe = sys.executable
        cmd_exe = "python"

        server_process = subprocess.Popen(
            [cmd_exe, SERVER_SCRIPT, '-d'],
            stdout=open(LOG_FILE, 'a', encoding='utf-8', buffering=1),
            # stderr=open(LOG_FILE, 'a', encoding='utf-8'),
            stderr=subprocess.STDOUT,
            encoding='utf-8',
            errors='ignore',        # 忽略字符编码错误``
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        server_is_running = True
        print(f"服务器已启动，输出将写入 {LOG_FILE}")
        update_tray_menu() # 更新托盘菜单以显示新状态
    else:
        print("服务器已经在运行中。")

def stop_server():
    global server_process, server_is_running
    if server_process:
        print("尝试关闭服务器...")
        server_process.terminate()
        try:
            server_process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            print("服务器未在规定时间内关闭，尝试强制终止。")
            server_process.kill()
        server_process = None
        server_is_running = False
        print("服务器已关闭。")
        update_tray_menu() # 更新托盘菜单以显示新状态
    else:
        print("服务器未运行。")

def open_cmd_window():
    # 使用 PowerShell 的 Get-Content -Wait 来实时显示日志文件
    # -Wait 相当于 Unix/Linux 的 tail -f
    # start powershell -NoExit -Command ... 会打开一个新的PowerShell窗口并执行命令
    cmd_command = f'start powershell -NoExit -Command "Get-Content -Path "{LOG_FILE}" -Wait"'
    subprocess.Popen(cmd_command, shell=True)
    print(f"已打开新的CMD窗口，显示 {LOG_FILE} 日志。")

def open_log_directory(icon):
    """打开日志文件所在的目录。"""
    log_dir = os.path.dirname(LOG_FILE)
    subprocess.Popen(f'explorer "{log_dir}"', shell=True)
    print(f"已打开日志目录: {log_dir}")

def quit_application(icon):
    stop_server()
    icon.stop() # 这会停止 pystray icon 线程
    stop_event.set() # 设置事件，通知主线程退出
    print("应用程序已退出。")
    # os._exit(0) # 通常不再需要，因为我们现在有事件来控制主线程退出


# 全局图标实例，需要在更新菜单时访问
my_icon = None

def create_menu():
    """根据服务器状态创建并返回菜单项列表。"""
    menu_items = [
        # # 第一项显示服务器状态，并禁用点击
        # MenuItem(f"状态: {server_is_running and "运行中" or "未运行"}", None, enabled=False),
        # Menu.SEPARATOR,
        MenuItem("启动服务器", start_server, enabled=not server_is_running),
        MenuItem("关闭服务器", stop_server, enabled=server_is_running),
        MenuItem("打开日志窗口", open_cmd_window),
        MenuItem("打开日志目录", open_log_directory),
        Menu.SEPARATOR,
        MenuItem("退出", lambda icon: quit_application(icon)) # 传入icon参数
    ]
    return menu_items

def update_tray_menu():
    """更新系统托盘菜单，以反映最新的服务器状态。"""
    if my_icon:
        # 重新设置菜单，pystray 会自动刷新
        my_icon.menu = create_menu()

def setup_icon(icon_instance):
    """初始化图标，并设置初始菜单。"""
    global my_icon
    my_icon = icon_instance # 保存图标实例
    my_icon.visible = True

    print("系统托盘图标已启动。在右键菜单里启动翻译服务器。")
    # 初始不启动服务器
    # start_server()
    # 初始设置菜单（start_server里会调用一次update_tray_menu）

if __name__ == "__main__":
    icon_path = os.path.join(script_dir, "tray_icon.ico")
    try:
        image = Image.open(icon_path)
    except Exception as e:
        print(f"无法加载图标文件: {icon_path}")
        sys.exit(1)

    # 注意这里创建 Icon 时，菜单最初可以是空的，或者直接调用 create_menu()
    icon = Icon(
        "my_server_app",
        image,
        "本地服务器",
        menu=create_menu() # 初始菜单
    )

    

    # 在单独的线程中运行系统托盘图标
    icon_thread = threading.Thread(target=icon.run)
    icon_thread.daemon = True
    icon_thread.start()

    # 将图标实例传递给 setup_icon
    setup_icon(icon)

    # 保持主线程活跃，直到 stop_event 被设置，或者被 Ctrl+C 打断
    try:
        # 使用一个短的超时时间，例如 1 秒
        while not stop_event.is_set():
            stop_event.wait(1) # 每秒检查一次事件是否被设置
            # 如果从 wait() 返回，但事件没被设置，说明是超时了，可以继续循环或检查其他条件
            # 此时如果按 Ctrl+C，KeyboardInterrupt 就会在这里（在 wait() 返回之后）被捕获
            pass
    except KeyboardInterrupt:
        print("捕获到 Ctrl+C，正在退出应用程序...")
        quit_application(icon) # 调用退出函数，它会设置 stop_event
    finally:
        print("主程序即将退出。")
