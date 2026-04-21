using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;

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

                byte[] mergedBytes;
                using (var ms = new MemoryStream())
                {
                    using (var doc = new Document())
                    {
                        var copy = new PdfSmartCopy(doc, ms);
                        copy.CloseStream = false;
                        doc.Open();
                        foreach (string path in _pdfFiles)
                        {
                            var reader = new PdfReader(path);
                            try   { copy.AddDocument(reader); }
                            finally { reader.Close(); }
                        }
                    }
                    mergedBytes = ms.ToArray();
                }

                EmbedFonts(mergedBytes, outputFile);

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

        private void EmbedFonts(byte[] pdfBytes, string outputPath)
        {
            var fontFileLookup = BuildWindowsFontLookup();

            using (var reader = new PdfReader(pdfBytes))
            using (var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var stamper = new PdfStamper(reader, outStream))
            {
                int xrefSize = reader.XrefSize;
                for (int objNum = 1; objNum < xrefSize; objNum++)
                {
                    PdfObject obj = reader.GetPdfObject(objNum);
                    if (obj == null || !obj.IsDictionary()) continue;
                    var dict = (PdfDictionary)obj;

                    PdfName type = dict.GetAsName(PdfName.TYPE);
                    if (type == null || !PdfName.FONTDESCRIPTOR.Equals(type)) continue;

                    if (dict.Get(PdfName.FONTFILE)  != null) continue;
                    if (dict.Get(PdfName.FONTFILE2) != null) continue;
                    if (dict.Get(PdfName.FONTFILE3) != null) continue;

                    PdfName psNameObj = dict.GetAsName(PdfName.FONTNAME);
                    if (psNameObj == null) continue;
                    string psName = psNameObj.ToString().TrimStart('/');

                    if (psName.Length > 7 && psName[6] == '+' &&
                        Regex.IsMatch(psName.Substring(0, 6), "^[A-Z]{6}$"))
                        psName = psName.Substring(7);

                    string fontFilePath = ResolveFontFile(psName, fontFileLookup);
                    if (fontFilePath == null) continue;

                    // Skip TTC collections — need an index, handle plain TTF/OTF only
                    if (fontFilePath.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)) continue;

                    byte[] fontData;
                    try { fontData = File.ReadAllBytes(fontFilePath); }
                    catch { continue; }
                    if (fontData == null || fontData.Length == 0) continue;

                    // OpenType/CFF starts with "OTTO" magic bytes
                    bool isOpenType = fontData.Length >= 4 &&
                                      fontData[0] == 0x4F && fontData[1] == 0x54 &&
                                      fontData[2] == 0x54 && fontData[3] == 0x4F;

                    var fontStream = new PRStream(reader, fontData);
                    fontStream.Put(PdfName.LENGTH1, new PdfNumber(fontData.Length));
                    fontStream.SetCompressionLevel(9);

                    PdfName fontFileKey;
                    if (isOpenType)
                    {
                        fontStream.Put(PdfName.SUBTYPE, new PdfName("OpenType"));
                        fontFileKey = PdfName.FONTFILE3;
                    }
                    else
                    {
                        fontFileKey = PdfName.FONTFILE2;
                    }

                    PdfIndirectObject fontStreamRef = stamper.Writer.AddToBody(fontStream);
                    dict.Put(fontFileKey, fontStreamRef.IndirectReference);
                }
            }
        }

        private Dictionary<string, string> BuildWindowsFontLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            const string regKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";

            using (var key = Registry.LocalMachine.OpenSubKey(regKey))
            {
                if (key == null) return lookup;
                string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                foreach (string valueName in key.GetValueNames())
                {
                    string fontFile = key.GetValue(valueName) as string;
                    if (string.IsNullOrEmpty(fontFile)) continue;

                    string fullPath = Path.IsPathRooted(fontFile)
                        ? fontFile
                        : Path.Combine(fontsDir, fontFile);

                    if (!File.Exists(fullPath)) continue;

                    string bareName = Path.GetFileName(fullPath);
                    if (!lookup.ContainsKey(bareName))  lookup[bareName]  = fullPath;
                    if (!lookup.ContainsKey(valueName)) lookup[valueName] = fullPath;
                }
            }
            return lookup;
        }

        private string ResolveFontFile(string psName, Dictionary<string, string> lookup)
        {
            if (string.IsNullOrEmpty(psName)) return null;

            foreach (string ext in new[] { ".ttf", ".otf", ".ttc" })
            {
                if (lookup.TryGetValue(psName + ext, out string p)) return p;
            }

            string normalized = psName.Replace("-", " ").Replace(",", " ");
            foreach (string ext in new[] { ".ttf", ".otf", ".ttc" })
            {
                if (lookup.TryGetValue(normalized + ext, out string p)) return p;
            }

            foreach (var kvp in lookup)
            {
                string stem = Path.GetFileNameWithoutExtension(kvp.Key);
                if (stem.Length <= 3) continue;
                if (stem.IndexOf(psName, StringComparison.OrdinalIgnoreCase) >= 0) return kvp.Value;
                if (psName.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0) return kvp.Value;
            }

            return null;
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

        // ── PDF Field Extraction ──────────────────────────────────────────────

        // ── PDF Field Extraction via OCR ──────────────────────────────────────

        private void ExtractPdfFields(string pdfPath,
            out string vehicleNum, out string testNum, out string date)
        {
            vehicleNum = null; testNum = null; date = null;
            try
            {
                string tempTop = Path.Combine(Path.GetTempPath(), "pdf_ocr_top.png");
                string tempBot = Path.Combine(Path.GetTempPath(), "pdf_ocr_bot.png");

                System.Drawing.Bitmap fullBmp = null;
                try
                {
                    using (var doc = PdfiumViewer.PdfDocument.Load(pdfPath))
                        fullBmp = (System.Drawing.Bitmap)doc.Render(0, 1748, 2480, false);

                    int fw = fullBmp.Width, fh = fullBmp.Height;

                    // Right 55%, top 15% scaled 2x → vehicle + test number
                    // (RTL form: test/vehicle numbers are top-right; km/engine are top-left)
                    int topH = (int)(fh * 0.15);
                    int rightX = (int)(fw * 0.45);
                    var topRect = new System.Drawing.Rectangle(rightX, 0, fw - rightX, topH);
                    using (var crop = fullBmp.Clone(topRect, fullBmp.PixelFormat))
                    {
                        var scaled = new System.Drawing.Bitmap(crop.Width * 2, crop.Height * 2);
                        using (var g = System.Drawing.Graphics.FromImage(scaled))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(crop, 0, 0, scaled.Width, scaled.Height);
                        }
                        scaled.Save(tempTop, System.Drawing.Imaging.ImageFormat.Png);
                        scaled.Dispose();
                    }

                    // Bottom 30% scaled 2x → date area (date is in lower body paragraph)
                    var botRect = new System.Drawing.Rectangle(0, (int)(fh * 0.65), fw, (int)(fh * 0.30));
                    using (var crop = fullBmp.Clone(botRect, fullBmp.PixelFormat))
                    {
                        var scaled = new System.Drawing.Bitmap(crop.Width * 2, crop.Height * 2);
                        using (var g = System.Drawing.Graphics.FromImage(scaled))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(crop, 0, 0, scaled.Width, scaled.Height);
                        }
                        scaled.Save(tempBot, System.Drawing.Imaging.ImageFormat.Png);
                        scaled.Dispose();
                    }
                }
                finally { if (fullBmp != null) fullBmp.Dispose(); }

                // OCR top → numbers
                string topText = OcrImage(tempTop);
                // Join split numbers (e.g. "44 4444" → "444444")
                string topNorm = Regex.Replace(topText, @"\b(\d{2,4})\s+(\d{2,4})\b", "$1$2");

                // Detect and exclude phone numbers (Israeli prefix: 0x-xxxxxxx)
                var phoneNums = new System.Collections.Generic.HashSet<string>();
                foreach (Match pm in Regex.Matches(topNorm, @"\b0\d[\-\s]?(\d{6,8})\b"))
                    phoneNums.Add(pm.Groups[1].Value);

                var seen = new List<string>();
                foreach (Match m in Regex.Matches(topNorm, @"\b(\d{4,8})\b"))
                {
                    string v = m.Groups[1].Value;
                    if (Regex.IsMatch(v, @"^(19|20)\d{2}$")) continue; // skip years
                    if (phoneNums.Contains(v)) continue;                 // skip phone numbers
                    if (!seen.Contains(v)) seen.Add(v);
                }
                seen.Sort((a, b) => b.Length.CompareTo(a.Length));
                if (seen.Count >= 1) vehicleNum = seen[0];
                if (seen.Count >= 2) testNum    = seen[1];

                // OCR bottom → date (try standard format first)
                string botText = OcrImage(tempBot);
                // OCR often reads "/" as "," or " " — match all common separators
                Match dm = Regex.Match(botText,
                    @"\b(\d{2})\s*[/\.\-,]\s*(\d{2})\s*[/\.\-,]\s*(20\d{2})\b");
                if (dm.Success)
                    date = dm.Groups[1].Value + "-" + dm.Groups[2].Value + "-" + dm.Groups[3].Value;

            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "ocr_top.txt"), "EXCEPTION: " + ex, Encoding.UTF8);
            }
        }

        private string OcrImage(string imagePath)
        {
            string tempScript = Path.Combine(Path.GetTempPath(), "pdf_ocr.ps1");
            File.WriteAllText(tempScript, OcrPowerShellScript, Encoding.UTF8);

            var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe",
                string.Format("-NoProfile -ExecutionPolicy Bypass -File \"{0}\" \"{1}\"",
                    tempScript, imagePath));
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow  = true;

            var proc = System.Diagnostics.Process.Start(psi);
            string text = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return text ?? "";
        }

        private const string OcrPowerShellScript = @"
param($imagePath)
$null = [System.Reflection.Assembly]::LoadWithPartialName('System.Runtime.WindowsRuntime')
[Windows.Media.Ocr.OcrEngine,           Windows.Media.Ocr,        ContentType=WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType=WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile,            Windows.Storage,          ContentType=WindowsRuntime] | Out-Null
[Windows.Globalization.Language,         Windows.Globalization,    ContentType=WindowsRuntime] | Out-Null

$asTaskG = ([System.WindowsRuntimeSystemExtensions].GetMethods() |
    Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
        $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]

function Await($t, $r) {
    $task = $asTaskG.MakeGenericMethod($r).Invoke($null, @($t))
    $task.Wait(-1) | Out-Null; $task.Result
}

$file    = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($imagePath)) ([Windows.Storage.StorageFile])
$stream  = Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
$decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
$bitmap  = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if ($engine -eq $null) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage(
        [Windows.Globalization.Language]::new('en-US'))
}
$result = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
Write-Output $result.Text
";

    }
}
