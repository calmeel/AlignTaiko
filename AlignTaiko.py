import tkinter as tk
from tkinter import filedialog, messagebox
import os
import json

# =========================
# デザイン設定（ダークブルー）
# =========================
WINDOW_W, WINDOW_H = 360, 240  # 言語スイッチ分 少しだけ高さUP

BG_COLOR = "#0e1b2a"
LABEL_BG = "#132a44"
BTN_BG = "#1c3f66"
BTN_ACTIVE_BG = "#255a99"
FG_COLOR = "white"

FONT_TITLE = ("メイリオ", 13, "bold")
FONT_UI = ("メイリオ", 11, "bold")
FONT_BTN = ("メイリオ", 12, "bold")

TARGET_X = "256"
TARGET_Y = "192"

# =========================
# 言語辞書
# =========================
LANG_JP = {
    "app_title": "AlignTaiko",
    "mode": "処理モード",
    "single": "単体ファイル",
    "batch": "フォルダ一括処理",
    "run": "実行",
    "select_output_title": "出力フォルダ選択",
    "select_output_msg": "出力フォルダを選択してください\n\n変更後のファイルが保存されます",
    "select_input_title": "入力フォルダ選択",
    "select_input_msg": "入力フォルダを選択してください\n\n.osu ファイルが入っているフォルダ",
    "confirm_title": "確認",
    "confirm_single": "HitObjects を中央 (256, 192) に揃えます。\n\n実行しますか？",
    "confirm_batch": "フォルダ内のすべての .osu ファイルを処理します。\n\n実行しますか？",
    "done_title": "完了",
    "done_single": "処理が完了しました。",
    "done_batch": "{} 件のファイルを処理しました。",
    "error_title": "エラー",
    "error_hitobjects": "[HitObjects] が見つかりません。",
    "select_osu_title": "osu! ファイルを選択",
}
LANG_EN = {
    "app_title": "AlignTaiko",
    "mode": "Processing mode",
    "single": "Single file",
    "batch": "Batch (folder)",
    "run": "Run",
    "select_output_title": "Select output folder",
    "select_output_msg": "Select OUTPUT folder\n\nModified file(s) will be saved here",
    "select_input_title": "Select input folder",
    "select_input_msg": "Select INPUT folder\n\nFolder containing .osu files",
    "confirm_title": "Confirm",
    "confirm_single": "Align HitObjects to center (256, 192).\n\nProceed?",
    "confirm_batch": "Process ALL .osu files in the folder.\n\nProceed?",
    "done_title": "Done",
    "done_single": "Process completed.",
    "done_batch": "Processed {} file(s).",
    "error_title": "Error",
    "error_hitobjects": "[HitObjects] not found.",
    "select_osu_title": "Select osu file",
}

# =========================
# 設定ファイル（言語保存）
# =========================
def get_config_path() -> str:
    appdata = os.getenv("APPDATA")
    base = appdata if appdata else os.path.expanduser("~")
    cfg_dir = os.path.join(base, "AlignTaiko")
    os.makedirs(cfg_dir, exist_ok=True)
    return os.path.join(cfg_dir, "config.json")

CONFIG_PATH = get_config_path()

def load_lang_setting() -> str:
    try:
        with open(CONFIG_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
        lang = data.get("lang", "JP")
        return "EN" if lang == "EN" else "JP"
    except Exception:
        return "JP"

def save_lang_setting(lang: str) -> None:
    try:
        with open(CONFIG_PATH, "w", encoding="utf-8") as f:
            json.dump({"lang": lang}, f, ensure_ascii=False, indent=2)
    except Exception:
        # 保存できなくてもアプリが落ちないようにする
        pass

# =========================
# 翻訳関数
# =========================
current_lang = None  # tk.StringVar は GUI 作成後に作る

def T(key: str) -> str:
    return (LANG_JP if current_lang.get() == "JP" else LANG_EN)[key]

# =========================
# 処理ロジック
# =========================
def process_osu_file(input_path, output_path):
    with open(input_path, "r", encoding="utf-8") as f:
        lines = f.read().splitlines()

    try:
        hit_idx = lines.index("[HitObjects]")
    except ValueError:
        return False

    for i in range(hit_idx + 1, len(lines)):
        line = lines[i]
        if not line.strip():
            continue

        parts = line.split(",")
        if len(parts) >= 4 and parts[3] == "1":
            parts[0] = TARGET_X
            parts[1] = TARGET_Y
            lines[i] = ",".join(parts)

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    return True

# =========================
# 単体処理
# =========================
def run_single():
    file_path = filedialog.askopenfilename(
        title=T("select_osu_title"),
        filetypes=[("osu files", "*.osu")]
    )
    if not file_path:
        return

    messagebox.showinfo(T("select_output_title"), T("select_output_msg"))
    out_dir = filedialog.askdirectory(title=T("select_output_title"))
    if not out_dir:
        return

    if not messagebox.askyesno(T("confirm_title"), T("confirm_single")):
        return

    out_path = os.path.join(out_dir, os.path.basename(file_path))

    if process_osu_file(file_path, out_path):
        messagebox.showinfo(T("done_title"), T("done_single"))
    else:
        messagebox.showerror(T("error_title"), T("error_hitobjects"))

# =========================
# フォルダ一括処理
# =========================
def run_batch():
    messagebox.showinfo(T("select_input_title"), T("select_input_msg"))
    in_dir = filedialog.askdirectory(title=T("select_input_title"))
    if not in_dir:
        return

    messagebox.showinfo(T("select_output_title"), T("select_output_msg"))
    out_dir = filedialog.askdirectory(title=T("select_output_title"))
    if not out_dir:
        return

    if not messagebox.askyesno(T("confirm_title"), T("confirm_batch")):
        return

    count = 0
    for fn in os.listdir(in_dir):
        if not fn.lower().endswith(".osu"):
            continue
        in_path = os.path.join(in_dir, fn)
        out_path = os.path.join(out_dir, fn)
        if process_osu_file(in_path, out_path):
            count += 1

    messagebox.showinfo(T("done_title"), T("done_batch").format(count))

# =========================
# GUI
# =========================
root = tk.Tk()
root.geometry(f"{WINDOW_W}x{WINDOW_H}")
root.resizable(False, False)
root.configure(bg=BG_COLOR)

# 言語：保存値をロードして反映
current_lang = tk.StringVar(value=load_lang_setting())

def refresh_text():
    root.title(T("app_title"))
    label_mode.config(text=T("mode"))
    radio_single.config(text=T("single"))
    radio_batch.config(text=T("batch"))
    run_button.config(text=T("run"))

def on_lang_change():
    save_lang_setting(current_lang.get())  # ★ここで保存
    refresh_text()

# 右上：言語スイッチ
topbar = tk.Frame(root, bg=BG_COLOR)
topbar.pack(fill="x", padx=10, pady=(8, 0))

lang_frame = tk.Frame(topbar, bg=BG_COLOR)
lang_frame.pack(side="right")

tk.Radiobutton(
    lang_frame, text="JP", value="JP",
    variable=current_lang, command=on_lang_change,
    font=FONT_UI, fg=FG_COLOR, bg=BG_COLOR,
    activebackground=BG_COLOR, activeforeground=FG_COLOR,
    selectcolor=LABEL_BG
).pack(side="left", padx=(0, 6))

tk.Radiobutton(
    lang_frame, text="EN", value="EN",
    variable=current_lang, command=on_lang_change,
    font=FONT_UI, fg=FG_COLOR, bg=BG_COLOR,
    activebackground=BG_COLOR, activeforeground=FG_COLOR,
    selectcolor=LABEL_BG
).pack(side="left")

# モード選択
mode = tk.StringVar(value="single")

label_mode = tk.Label(
    root,
    text="",
    font=FONT_TITLE,
    fg=FG_COLOR,
    bg=BG_COLOR
)
label_mode.pack(pady=(10, 6))

radio_frame = tk.Frame(root, bg=BG_COLOR)
radio_frame.pack()

radio_single = tk.Radiobutton(
    radio_frame,
    text="",
    variable=mode,
    value="single",
    font=FONT_UI,
    fg=FG_COLOR,
    bg=BG_COLOR,
    activebackground=BG_COLOR,
    activeforeground=FG_COLOR,
    selectcolor=LABEL_BG
)
radio_single.pack(anchor="w", padx=30)

radio_batch = tk.Radiobutton(
    radio_frame,
    text="",
    variable=mode,
    value="batch",
    font=FONT_UI,
    fg=FG_COLOR,
    bg=BG_COLOR,
    activebackground=BG_COLOR,
    activeforeground=FG_COLOR,
    selectcolor=LABEL_BG
)
radio_batch.pack(anchor="w", padx=30)

def run():
    if mode.get() == "single":
        run_single()
    else:
        run_batch()

run_button = tk.Button(
    root,
    text="",
    font=FONT_BTN,
    fg=FG_COLOR,
    bg=BTN_BG,
    activebackground=BTN_ACTIVE_BG,
    activeforeground=FG_COLOR,
    relief="flat",
    height=2,
    width=18,
    command=run
)
run_button.pack(pady=18)

# 初期表示を反映
refresh_text()

root.mainloop()
