import tkinter as tk
from tkinter import filedialog, messagebox
import os

def process_file():
    # 入力ファイル選択
    input_file = filedialog.askopenfilename(
        title="Select input file",
        filetypes=[("osu / txt", "*.osu *.txt"), ("All files", "*.*")]
    )
    if not input_file:
        return

    # 出力フォルダ選択
    output_dir = filedialog.askdirectory(
        title="Select output folder"
    )
    if not output_dir:
        return

    # 元のファイル名をそのまま使用
    output_file = os.path.join(
        output_dir,
        os.path.basename(input_file)
    )

    with open(input_file, "r", encoding="utf-8") as f:
        lines = f.read().splitlines()

    try:
        hit_idx = lines.index("[HitObjects]")
    except ValueError:
        messagebox.showerror("Error", "[HitObjects] section was not found.")
        return

    for i in range(hit_idx + 1, len(lines)):
        line = lines[i]
        if line == "":
            continue

        parts = line.split(",")
        if len(parts) >= 4 and parts[3] == "1":
            parts[0] = "256"
            parts[1] = "192"
            lines[i] = ",".join(parts)

    # 書き出し
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    messagebox.showinfo(
        "Done",
        f"Processing completed successfully.\n\nOutput file:\n{output_file}"
    )

# ---- GUI 本体 ----
root = tk.Tk()
root.title("AlignTaiko")
root.geometry("320x170")
root.resizable(False, False)

label = tk.Label(root, text="osu!taiko Alignment Tool", font=("Meiryo", 11))
label.pack(pady=15)

button = tk.Button(
    root,
    text="Select file and process",
    command=process_file,
    height=2
)
button.pack(pady=10)

root.mainloop()