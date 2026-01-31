using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlignTaiko.Gui
{
    internal static class UiTheme
    {
        // ---- Palette (your picks) ----
        public static readonly Color BgMain = Color.FromArgb(23, 23, 28);   // #17171C
        public static readonly Color BgPanel = Color.FromArgb(35, 34, 42);   // #23222A
        public static readonly Color BgInput = Color.FromArgb(46, 46, 56);   // #2E2E38
        public static readonly Color Border = Color.FromArgb(58, 57, 70);   // #3A3946

        public static readonly Color Accent = Color.FromArgb(61, 57, 172);  // #3D39AC
        public static readonly Color AccentSoft = Color.FromArgb(167, 166, 217); // #A7A6D9

        public static readonly Color TextMain = Color.Gainsboro;
        public static readonly Color TextMuted = Color.FromArgb(160, 160, 160);

        // Optional: log colors (tweak as you like)
        public static readonly Color LogOk = AccentSoft;
        public static readonly Color LogWarn = Color.FromArgb(214, 185, 92); // muted yellow
        public static readonly Color LogErr = Color.FromArgb(217, 106, 106); // muted red

        public static readonly Color BgReadOnly = Color.FromArgb(30, 30, 36); // BgPanel より暗い

        /// <summary>
        /// Apply the dark-blue theme to the given form & common controls.
        /// Call once after controls are created (after InitializeComponent / after BuildUi()).
        /// </summary>
        public static void Apply(Form form)
        {
            if (form == null) return;

            form.BackColor = BgMain;
            form.ForeColor = TextMain;

            // Recursively theme controls
            ApplyToControlTree(form);

            // Improve rendering a bit on Windows
            form.DoubleBuffered(true);
        }

        private static void ApplyToControlTree(Control root)
        {
            foreach (Control c in root.Controls)
            {
                ApplyToControl(c);

                // recurse
                if (c.HasChildren)
                    ApplyToControlTree(c);
            }
        }

        private static void ApplyToControl(Control c)
        {
            switch (c)
            {
                case TableLayoutPanel tlp:
                    tlp.BackColor = BgMain;
                    break;

                case Panel p:
                    // If this is your drop panel, you can override later by name/tag.
                    p.BackColor = BgPanel;
                    break;

                case GroupBox gb:
                    gb.ForeColor = TextMain;
                    gb.BackColor = BgMain;
                    break;

                case Label lbl:
                    lbl.ForeColor = TextMain;
                    // keep transparent to show parent's back color
                    lbl.BackColor = Color.Transparent;
                    break;

                case RadioButton rb:
                    rb.ForeColor = TextMain;
                    rb.BackColor = Color.Transparent;
                    break;

                case CheckBox cb:
                    cb.ForeColor = TextMain;
                    cb.BackColor = Color.Transparent;
                    break;

                case ComboBox combo:
                    // Note: WinForms ComboBox color support depends on style.
                    combo.BackColor = BgInput;
                    combo.ForeColor = TextMain;
                    break;

                case TextBox tb:
                    if (tb.ReadOnly)
                    {
                        tb.BackColor = BgPanel;
                        tb.ForeColor = TextMuted;
                    }
                    else
                    {
                        tb.BackColor = BgInput;
                        tb.ForeColor = TextMain;
                    }
                    break;

                case RichTextBox rtb:
                    rtb.BackColor = BgInput;
                    rtb.ForeColor = TextMain;
                    rtb.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListBox lb:
                    lb.BackColor = BgInput;
                    lb.ForeColor = TextMain;
                    break;

                case Button btn:
                    StyleButton(btn);
                    break;
            }
        }

        /// <summary>
        /// Base styling for buttons. Call SetPrimaryButton() for Run.
        /// </summary>
        public static void StyleButton(Button btn)
        {
            // Default button = secondary
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Border;

            btn.BackColor = BgPanel;
            btn.ForeColor = AccentSoft;

            // Hover/pressed subtle feedback
            btn.FlatAppearance.MouseOverBackColor = BgInput;
            btn.FlatAppearance.MouseDownBackColor = BgInput;
        }

        /// <summary>
        /// Primary CTA button styling (Run / Start).
        /// </summary>
        public static void SetPrimaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Accent;

            btn.BackColor = Accent;
            btn.ForeColor = Color.White;

            btn.FlatAppearance.MouseOverBackColor = Accent;
            btn.FlatAppearance.MouseDownBackColor = Accent;
        }

        /// <summary>
        /// Recommended styling for the drop area.
        /// </summary>
        public static void StyleDropArea(Panel pnlDrop, Label lblDropHint)
        {
            if (pnlDrop != null)
            {
                pnlDrop.BackColor = BgInput;
                pnlDrop.BorderStyle = BorderStyle.FixedSingle;
            }

            if (lblDropHint != null)
            {
                lblDropHint.ForeColor = TextMuted;
                lblDropHint.BackColor = Color.Transparent;
                lblDropHint.Dock = DockStyle.Fill;
                lblDropHint.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        // ---- Helpers ----
        private static void DoubleBuffered(this Control control, bool enable)
        {
            // Enable double buffer on some WinForms controls via reflection
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(control, enable, null);
        }
    }
}
