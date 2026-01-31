using System;
using System.Collections.Generic;
using System.IO;

namespace AlignTaiko.Gui.Services
{
    internal sealed class BatchPreviewService
    {
        /// <summary>
        /// seedOsu と同じフォルダ直下にある .osu を列挙し、
        /// diff名（Version:）の一覧を返す。
        /// 下位フォルダには降りない。
        /// </summary>
        public IReadOnlyList<string> GetDiffNames(string seedOsuPath)
        {
            if (string.IsNullOrWhiteSpace(seedOsuPath))
                return Array.Empty<string>();

            if (!File.Exists(seedOsuPath))
                return Array.Empty<string>();

            var dir = Path.GetDirectoryName(seedOsuPath);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return Array.Empty<string>();

            var result = new List<string>();

            foreach (var file in Directory.EnumerateFiles(dir, "*.osu", SearchOption.TopDirectoryOnly))
            {
                var diff = TryReadDiffName(file);
                result.Add(diff ?? Path.GetFileNameWithoutExtension(file));
            }

            return result;
        }

        /// <summary>
        /// .osu ファイルから Version:（diff名）を読む。
        /// </summary>
        private static string? TryReadDiffName(string osuPath)
        {
            try
            {
                foreach (var line in File.ReadLines(osuPath))
                {
                    if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                        return line.Substring("Version:".Length).Trim();
                }
            }
            catch
            {
                // 読めなくても落とさない
            }
            return null;
        }
    }
}
