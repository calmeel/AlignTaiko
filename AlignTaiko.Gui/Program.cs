using System.Globalization;

namespace AlignTaiko.Gui
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // İ’è‚©‚çŒ¾Œê‚ğ“Ç‚İ‚İi–³‚¯‚ê‚Î‰pŒêj
            var culture = AppConfig.LoadCultureNameOrDefault();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            Application.Run(new MainForm());
        }
    }
}