using System.Text;

namespace AlignTaiko.Core
{
    public static class OsuAligner
    {
        public static AlignResult AlignFile(string inputPath, string outputPath, AlignOptions? opt = null)
        {
            opt ??= new AlignOptions();

            if (!File.Exists(inputPath))
                return new AlignResult(false, 0, $"Input not found: {inputPath}");

            string[] lines;
            try
            {
                // osu! は基本 UTF-8 (BOM無し想定)
                lines = File.ReadAllLines(inputPath, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                return new AlignResult(false, 0, ex.Message);
            }

            int hitIdx = Array.IndexOf(lines, "[HitObjects]");
            if (hitIdx < 0)
                return new AlignResult(false, 0, "[HitObjects] not found.");

            int changed = 0;

            for (int i = hitIdx + 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int type))
                {
                    const int HitCircle = 1;

                    if ((type & HitCircle) != 0)
                    {
                        parts[0] = opt.TargetX.ToString();
                        parts[1] = opt.TargetY.ToString();
                        lines[i] = string.Join(",", parts);
                        changed++;
                    }
                }
            }

            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllLines(outputPath, lines, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                return new AlignResult(false, changed, ex.Message);
            }

            return new AlignResult(true, changed, null);
        }

        /// <summary>
        /// フォルダ一括： inputDir の *.osu を outputDir に同名保存
        /// </summary>
        public static int AlignFolder(string inputDir, string outputDir, AlignOptions? opt = null)
        {
            opt ??= new AlignOptions();

            if (!Directory.Exists(inputDir)) return 0;
            Directory.CreateDirectory(outputDir);

            int ok = 0;
            foreach (var inPath in Directory.EnumerateFiles(inputDir, "*.osu", SearchOption.TopDirectoryOnly))
            {
                var outPath = Path.Combine(outputDir, Path.GetFileName(inPath));
                var r = AlignFile(inPath, outPath, opt);
                if (r.Success) ok++;
            }
            return ok;
        }
    }
}
