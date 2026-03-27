using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfMerger
{
    public partial class MainForm : Form
    {
        private readonly List<string> _pdfFiles = new List<string>();
        private const int MaxFiles = 3;
        private string _lastFolder = null;

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            UpdateUI();
        }

        // ── Settings ──────────────────────────────────────────────────────────

        private void LoadSettings()
        {
            string saved = Properties.Settings.Default.OutputFolder;
            if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
                lblFolder.Text = saved;
            else
                lblFolder.Text = "לא נבחרה תיקייה";
        }

        private string OutputFolder
        {
            get { return lblFolder.Text == "לא נבחרה תיקייה" ? null : lblFolder.Text; }
        }

        private void btnChangeFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "בחר תיקייה לשמירת קובץ PDF הממוזג";
                if (OutputFolder != null && Directory.Exists(OutputFolder))
                    dlg.SelectedPath = OutputFolder;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    lblFolder.Text = dlg.SelectedPath;
                    Properties.Settings.Default.OutputFolder = dlg.SelectedPath;
                    Properties.Settings.Default.Save();
                    SetStatus("", Color.Empty);
                }
            }
            UpdateMergeButton();
        }

        // ── Browse (click) ────────────────────────────────────────────────────

        private void panelDrop_Click(object sender, EventArgs e)
        {
            if (_pdfFiles.Count >= MaxFiles) return;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "בחר קבצי PDF";
                dlg.Filter = "קבצי PDF|*.pdf";
                dlg.Multiselect = true;
                if (_lastFolder != null && Directory.Exists(_lastFolder))
                    dlg.InitialDirectory = _lastFolder;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                _lastFolder = Path.GetDirectoryName(dlg.FileNames[0]);

                foreach (string file in dlg.FileNames)
                {
                    if (_pdfFiles.Contains(file)) continue;
                    if (_pdfFiles.Count >= MaxFiles) break;
                    _pdfFiles.Add(file);
                }

                if (_pdfFiles.Count >= MaxFiles)
                    SetStatus("הגעת לגבול של 3 קבצים.", Color.FromArgb(180, 100, 0));
                else
                    SetStatus("", Color.Empty);

                UpdateUI();
            }
        }

        // ── Drag & Drop ───────────────────────────────────────────────────────

        private void panelDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool allPdf = Array.TrueForAll(files,
                    f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                bool hasRoom = (_pdfFiles.Count + files.Length) <= MaxFiles;

                e.Effect = (allPdf && hasRoom)
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void panelDrop_DragOver(object sender, DragEventArgs e)
        {
            panelDrop_DragEnter(sender, e);
        }

        private void panelDrop_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int added = 0;

            foreach (string file in files)
            {
                if (!file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                if (_pdfFiles.Contains(file)) continue;
                if (_pdfFiles.Count >= MaxFiles) break;
                _pdfFiles.Add(file);
                added++;
            }

            if (added > 0)
                SetStatus("", Color.Empty);

            if (_pdfFiles.Count >= MaxFiles)
                SetStatus("הגעת לגבול של 3 קבצים.", Color.FromArgb(180, 100, 0));

            UpdateUI();
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void UpdateUI()
        {
            pdfListPanel.Controls.Clear();
            lblDropHint.Visible = (_pdfFiles.Count == 0);
            pdfListPanel.Visible = (_pdfFiles.Count > 0);

            foreach (string path in _pdfFiles)
            {
                var row = CreateFileRow(path);
                pdfListPanel.Controls.Add(row);
            }

            bool full = (_pdfFiles.Count >= MaxFiles);
            panelDrop.Cursor  = full ? Cursors.Default : Cursors.Hand;
            pdfListPanel.Cursor = full ? Cursors.Default : Cursors.Hand;

            UpdateMergeButton();
        }

        private Panel CreateFileRow(string filePath)
        {
            var panel = new Panel
            {
                Size = new Size(430, 52),
                BackColor = Color.White,
                Tag = filePath
            };

            // PDF icon label
            var icon = new Label
            {
                Text = "PDF",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 60, 60),
                Size = new Size(34, 34),
                Location = new Point(8, 9),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Filename
            var name = new Label
            {
                Text = Path.GetFileName(filePath),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(40, 40, 60),
                AutoSize = false,
                Size = new Size(330, 20),
                Location = new Point(50, 8),
                AutoEllipsis = true
            };

            // File size
            long bytes = new FileInfo(filePath).Length;
            string sizeText;
            if (bytes < 1024 * 1024)
                sizeText = string.Format("{0:F1} KB", bytes / 1024.0);
            else
                sizeText = string.Format("{0:F1} MB", bytes / (1024.0 * 1024));

            var size = new Label
            {
                Text = sizeText,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(330, 16),
                Location = new Point(50, 28)
            };

            // Remove button
            string capturedPath = filePath;
            var btn = new Button
            {
                Text = "X",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 60, 60),
                BackColor = Color.White,
                Size = new Size(28, 28),
                Location = new Point(394, 12),
                Tag = filePath,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, ev) =>
            {
                _pdfFiles.Remove(capturedPath);
                SetStatus("", Color.Empty);
                UpdateUI();
            };

            // Separator line
            var line = new Panel
            {
                BackColor = Color.FromArgb(230, 230, 240),
                Size = new Size(430, 1),
                Location = new Point(0, 51)
            };

            panel.Controls.AddRange(new Control[] { icon, name, size, btn, line });
            return panel;
        }

        private void UpdateMergeButton()
        {
            bool canMerge = _pdfFiles.Count >= 2 && OutputFolder != null;
            btnMerge.Enabled = canMerge;
            btnMerge.BackColor = canMerge
                ? Color.FromArgb(60, 120, 220)
                : Color.FromArgb(180, 190, 210);
        }

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color == Color.Empty ? Color.FromArgb(60, 160, 80) : color;
        }

        // ── Merge ─────────────────────────────────────────────────────────────

        private void btnMerge_Click(object sender, EventArgs e)
        {
            if (_pdfFiles.Count < 2 || OutputFolder == null) return;

            string initVehicle, initTest, initDate;
            ExtractPdfFields(_pdfFiles[0], out initVehicle, out initTest, out initDate);

            string vehicleNum, testNum, inspDate;
            if (!ShowFieldsDialog(initVehicle, initTest, initDate,
                    out vehicleNum, out testNum, out inspDate))
                return;
            string outputFile = Path.Combine(OutputFolder,
                string.Format("{0}_{1}_{2}.pdf", vehicleNum, testNum, inspDate));

            try
            {
                btnMerge.Enabled = false;
                btnMerge.Text = "ממזג...";

                using (var output = new PdfDocument())
                {
                    foreach (string path in _pdfFiles)
                    {
                        using (var input = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                        {
                            for (int i = 0; i < input.PageCount; i++)
                                output.AddPage(input.Pages[i]);
                        }
                    }
                    output.Save(outputFile);
                }

                _pdfFiles.Clear();
                UpdateUI();
                SetStatus("נשמר: " + Path.GetFileName(outputFile), Color.FromArgb(30, 140, 60));

                string msg = "הקובץ נשמר בהצלחה!\n" + outputFile + "\n\nלפתוח את התיקייה?";
                if (MessageBox.Show(msg, "בוצע", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe",
                        "/select,\"" + outputFile + "\"");
                }
            }
            catch (Exception ex)
            {
                SetStatus("שגיאה: " + ex.Message, Color.FromArgb(200, 50, 50));
                MessageBox.Show("שגיאה במיזוג:\n" + ex.Message, "שגיאה",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnMerge.Text = "מזג PDF'ים";
                UpdateMergeButton();
            }
        }

        private bool ShowFieldsDialog(string initVehicle, string initTest, string initDate,
            out string vehicleNum, out string testNum, out string date)
        {
            vehicleNum = null; testNum = null; date = null;

            var dlg = new Form();
            dlg.Text = "פרטי הבדיקה";
            dlg.RightToLeft = RightToLeft.Yes;
            dlg.RightToLeftLayout = true;
            dlg.Width = 340;
            dlg.Height = 210;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            int y = 18;
            Func<string, TextBox> addRow = label =>
            {
                var lbl = new Label { Text = label, Left = 180, Top = y + 3, Width = 120, TextAlign = ContentAlignment.MiddleRight };
                var txt = new TextBox { Left = 16, Top = y, Width = 160, RightToLeft = RightToLeft.No };
                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txt);
                y += 38;
                return txt;
            };

            var txtVehicle = addRow("מספר רכב:");
            txtVehicle.Text = initVehicle ?? "";
            var txtTest    = addRow("מס' בדיקה:");
            txtTest.Text   = initTest ?? "";
            var txtDate    = addRow("תאריך (dd-MM-yyyy):");
            txtDate.Text   = initDate ?? DateTime.Now.ToString("dd-MM-yyyy");

            var btnOk = new Button { Text = "אישור", Left = 220, Top = y, Width = 80, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "ביטול", Left = 130, Top = y, Width = 80, DialogResult = DialogResult.Cancel };
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            if (string.IsNullOrWhiteSpace(txtVehicle.Text) || string.IsNullOrWhiteSpace(txtTest.Text))
            {
                MessageBox.Show("יש למלא מספר רכב ומס' בדיקה.", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            vehicleNum = txtVehicle.Text.Trim();
            testNum    = txtTest.Text.Trim();
            date       = string.IsNullOrWhiteSpace(txtDate.Text) ? DateTime.Now.ToString("dd-MM-yyyy") : txtDate.Text.Trim();
            return true;
        }

        // ── PDF Field Extraction ───────────────────────────────────────────────

        private void ExtractPdfFields(string pdfPath,
            out string vehicleNum, out string testNum, out string date)
        {
            vehicleNum = null; testNum = null; date = null;
            try
            {
                using (var reader = new iTextSharp.text.pdf.PdfReader(pdfPath))
                {
                    var pageRect = reader.GetPageSizeWithRotation(1);
                    float w = pageRect.Width;
                    float h = pageRect.Height;

                    // Strategy A: region-based LocationTextExtractionStrategy
                    var topRight = new iTextSharp.text.Rectangle(w * 0.55f, h * 0.75f, w, h);
                    string topText = ExtractRegion(reader, 1, topRight);

                    var bottomRect = new iTextSharp.text.Rectangle(0, 0, w, h * 0.15f);
                    string botText = ExtractRegion(reader, 1, bottomRect);

                    Match mDate = Regex.Match(botText + " " + topText,
                        @"\b(\d{2}[/\.]\d{2}[/\.]\d{4})\b");
                    if (mDate.Success)
                        date = mDate.Groups[1].Value.Replace(".", "/").Replace("/", "-");

                    ParseVehicleAndTest(topText, out vehicleNum, out testNum);

                    // Strategy B: raw content stream fallback
                    if (vehicleNum == null || testNum == null || date == null)
                    {
                        string raw = DecodeRawContent(reader.GetPageContent(1));
                        if (date == null)
                        {
                            Match m2 = Regex.Match(raw, @"\b(\d{2}[/\.]\d{2}[/\.]\d{4})\b");
                            if (m2.Success)
                                date = m2.Groups[1].Value.Replace(".", "/").Replace("/", "-");
                        }
                        if (vehicleNum == null || testNum == null)
                        {
                            string v2, t2;
                            ParseVehicleAndTest(raw, out v2, out t2);
                            if (vehicleNum == null) vehicleNum = v2;
                            if (testNum    == null) testNum    = t2;
                        }
                    }
                }
            }
            catch { /* fields stay null → dialog shows empty */ }
        }

        private string ExtractRegion(iTextSharp.text.pdf.PdfReader reader,
            int page, iTextSharp.text.Rectangle region)
        {
            try
            {
                var filter = new iTextSharp.text.pdf.parser.RegionTextRenderFilter(region);
                var strategy = new iTextSharp.text.pdf.parser.FilteredTextRenderListener(
                    new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy(), filter);
                return iTextSharp.text.pdf.parser.PdfTextExtractor
                    .GetTextFromPage(reader, page, strategy) ?? "";
            }
            catch { return ""; }
        }

        private void ParseVehicleAndTest(string text,
            out string vehicleNum, out string testNum)
        {
            vehicleNum = null; testNum = null;
            var seen = new System.Collections.Generic.List<string>();
            foreach (Match m in Regex.Matches(text, @"\b(\d{4,8})\b"))
                if (!seen.Contains(m.Groups[1].Value))
                    seen.Add(m.Groups[1].Value);

            // Remove year-like numbers (19xx / 20xx)
            seen.RemoveAll(n => Regex.IsMatch(n, @"^(19|20)\d{2}$"));

            // Longest = vehicle number, next = test number
            seen.Sort((a, b) => b.Length.CompareTo(a.Length));
            if (seen.Count >= 1) vehicleNum = seen[0];
            if (seen.Count >= 2) testNum    = seen[1];
        }

        private string DecodeRawContent(byte[] raw)
        {
            if (raw == null) return "";
            string ascii = System.Text.Encoding.ASCII.GetString(raw);
            var sb = new StringBuilder();
            // Literal strings: (content)
            foreach (Match m in Regex.Matches(ascii, @"\(([^\)]{1,80})\)"))
                sb.Append(m.Groups[1].Value).Append(' ');
            // Hex strings: <HEXHEX>
            foreach (Match m in Regex.Matches(ascii, @"<([0-9A-Fa-f]{2,160})>"))
            {
                string hex = m.Groups[1].Value;
                if (hex.Length % 2 != 0) continue;
                var bytes = new byte[hex.Length / 2];
                bool ok = true;
                for (int i = 0; i < bytes.Length; i++)
                {
                    try { bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16); }
                    catch { ok = false; break; }
                }
                if (ok) sb.Append(System.Text.Encoding.ASCII.GetString(bytes)).Append(' ');
            }
            return sb.ToString();
        }
    }
}
