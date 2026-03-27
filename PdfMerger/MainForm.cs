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

            string vehicleNum, testNum, inspDate;
            ExtractPdfFields(_pdfFiles[0], out vehicleNum, out testNum, out inspDate);
            string vehiclePart = vehicleNum ?? Path.GetFileNameWithoutExtension(_pdfFiles[0]);
            string testPart    = testNum    ?? Properties.Settings.Default.TestCounter.ToString();
            string datePart    = inspDate   ?? DateTime.Now.ToString("dd-MM-yyyy");
            string outputFile  = Path.Combine(OutputFolder,
                string.Format("{0}_{1}_{2}.pdf", vehiclePart, testPart, datePart));

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

        private void ExtractPdfFields(string pdfPath, out string vehicleNum, out string testNum, out string date)
        {
            vehicleNum = null;
            testNum    = null;
            date       = null;
            try
            {
                using (var reader = new iTextSharp.text.pdf.PdfReader(pdfPath))
                {
                    var sb = new StringBuilder();
                    for (int p = 1; p <= reader.NumberOfPages; p++)
                        sb.AppendLine(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, p));
                    string text = sb.ToString();

                    // DEBUG - הצג את הטקסט שנחלץ
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pdf_debug.txt"),
                        text, System.Text.Encoding.UTF8);
                    MessageBox.Show("טקסט שנחלץ נשמר ב:\n" + System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pdf_debug.txt"),
                        "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Match m = Regex.Match(text, @"מספר\s+רכב[:\s]+(\d{5,10})");
                    if (m.Success) vehicleNum = m.Groups[1].Value;

                    m = Regex.Match(text, @"מס['\u2019]?\s*בדיקה[:\s]+(\d{4,8})");
                    if (m.Success) testNum = m.Groups[1].Value;

                    m = Regex.Match(text, @"בתאריך[:\s]+(\d{2}/\d{2}/\d{4})");
                    if (m.Success) date = m.Groups[1].Value.Replace("/", "-");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בחילוץ:\n" + ex.Message, "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
