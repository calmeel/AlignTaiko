using System.IO;
using AlignTaiko.Core;

namespace AlignTaiko.Gui.Services
{
    internal static class OverwriteHelper
    {
        /// <summary>
        /// AlignFile を temp に書き出してから元ファイルを安全に置換する。
        /// </summary>
        public static AlignResult AlignFileOverwriteSafe(string path, AlignOptions opt)
        {
            var dir = Path.GetDirectoryName(path) ?? "";
            var tmp = Path.Combine(dir, Path.GetFileName(path) + ".aln_tmp");

            var r = OsuAligner.AlignFile(path, tmp, opt);
            if (!r.Success)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return r;
            }

            File.Replace(tmp, path, destinationBackupFileName: null);
            return r;
        }
    }
}
