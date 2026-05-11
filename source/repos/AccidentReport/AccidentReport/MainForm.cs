using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace AccidentReport
{
    public partial class MainForm : XtraForm
    {
        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color Navy = Color.FromArgb(26, 58, 107);
        private static readonly Color NavyLight = Color.FromArgb(225, 233, 245);
        private static readonly Color Blue = Color.FromArgb(41, 128, 185);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color RedLight = Color.FromArgb(253, 232, 232);
        private static readonly Color Amber = Color.FromArgb(211, 84, 0);
        private static readonly Color AmberLight = Color.FromArgb(254, 243, 226);
        private static readonly Color Green = Color.FromArgb(39, 174, 96);
        private static readonly Color GreenLight = Color.FromArgb(230, 244, 234);
        private static readonly Color Surface = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color Border = Color.FromArgb(218, 222, 230);
        private static readonly Color TextPrimary = Color.FromArgb(30, 35, 45);
        private static readonly Color TextMuted = Color.FromArgb(110, 118, 130);

        // ── Lookup data ───────────────────────────────────────────────────────
        private string[] Stations = Array.Empty<string>();
        private string[] AccidentTypes = Array.Empty<string>();
        private string[] Routes = Array.Empty<string>();
        private string[] VehiclesInvolvedList = Array.Empty<string>();
        private string[] Provinces = Array.Empty<string>();
        private string[] Districts = Array.Empty<string>();
        private string[] DaysOfTheWeek = new[] { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };

        // ── Form-tab controls ─────────────────────────────────────────────────
        private ComboBoxEdit cboStation, cboType, cboRoute, cboVehicles, cboProvince, cboDistrict, cboDays;
        private TextEdit txtARNumber, txtLocation;
        private DateEdit dtAccidentDate;
        private TimeEdit tdAccidentTime;
        private LabelControl lblDayValue, lblTotalCasualties;
        private SpinEdit spnFD, spnFP, spnFPD, spnFC;
        private SpinEdit spnSD, spnSP, spnSPD, spnSC;
        private SpinEdit spnSLD, spnSLP, spnSLPD, spnSLC;

        // ── Reports-tab controls ──────────────────────────────────────────────
        private DateEdit dtFrom, dtTo, dtReportMonth;
        private RichTextBox rtbReport;
        private ComboBoxEdit cboFilterStation, cboFilterType;
        private GridControl grid, gridMonthlyReport;
        private GridView gridView, gridViewMonthlyReport;
        private ChartControl chartByType, chartBySeverity, chartByStation, chartByMonth;
        private ChartControl chartMonthlyCrashes, chartMonthlyFatalities;
        private LabelControl lblKpiAccidents, lblKpiFatal, lblKpiSerious, lblKpiSlight;
        private SimpleButton btnExportReport, btnSaveReport, btnGenerateReport;
        private XtraTabControl tabs;
        private Panel scrollFormTab;
        private Panel scrollReportsTab;

        private MonthlyAccidentData currentMonthlyData;

        // ─────────────────────────────────────────────────────────────────────
        public MainForm()
        {
            Text = "Vehicle Accident Reporter";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 780);
            BackColor = Surface;

            try
            {
                DatabaseHelper.EnsureTable();
                Stations = DatabaseHelper.GetLookup("Stations", "Name").ToArray();
                AccidentTypes = DatabaseHelper.GetLookup("AccidentTypes", "Name").ToArray();
                Routes = DatabaseHelper.GetLookup("Routes", "Name").ToArray();
                VehiclesInvolvedList = DatabaseHelper.GetLookup("VehiclesInvolved", "Name").ToArray();
                Provinces = DatabaseHelper.GetLookup("Provinces", "ProvinceName").ToArray();
                Districts = DatabaseHelper.GetLookup("DistrictMunicipalities", "DistrictMunicipalityName").ToArray();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    "Could not connect to SQL Server.\n\nError: " + ex.Message,
                    "Database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            BuildUI();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  SHELL
        // ═════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Navy };
            topBar.Controls.Add(new LabelControl
            {
                Text = "Vehicle Accident Reporter",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Location = new Point(24, 15),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(400, 28),
                BackColor = Color.Transparent
            });

            tabs = new XtraTabControl { Dock = DockStyle.Fill };
            tabs.AppearancePage.Header.BackColor = Color.White;
            tabs.AppearancePage.Header.ForeColor = TextMuted;
            tabs.AppearancePage.Header.Font = new Font("Segoe UI", 10);
            tabs.AppearancePage.HeaderActive.BackColor = Color.White;
            tabs.AppearancePage.HeaderActive.ForeColor = Navy;
            tabs.AppearancePage.HeaderActive.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            tabs.AppearancePage.PageClient.BackColor = Surface;

            var tabForm = new XtraTabPage { Text = "   New Report   " };
            var tabReports = new XtraTabPage { Text = "   Reports & Analytics   " };

            // ── BUG 4 FIX ────────────────────────────────────────────────────
            // BuildMonthlyReportTab was defined but never called, so the Monthly
            // Report tab was never added to the XtraTabControl.
            // Now we create the page, build its content, and add it.
            var tabMonthly = new XtraTabPage { Text = "   Monthly Report   " };

            tabs.TabPages.Add(tabForm);
            tabs.TabPages.Add(tabReports);
            tabs.TabPages.Add(tabMonthly); // BUG 4 FIX: tab now added

            BuildFormTab(tabForm);
            BuildReportsTab(tabReports);
            BuildMonthlyReportTab(tabMonthly); // BUG 4 FIX: method now invoked
            // ─────────────────────────────────────────────────────────────────

            Controls.Add(tabs);
            Controls.Add(topBar);

            this.Shown += (s, e) =>
            {
                scrollFormTab?.PerformLayout();
                scrollReportsTab?.PerformLayout();
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        //  FORM TAB
        // ═════════════════════════════════════════════════════════════════════
        private void BuildFormTab(XtraTabPage page)
        {
            scrollFormTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Surface };
            var content = new Panel { AutoSize = true, BackColor = Surface };

            scrollFormTab.Resize += (s, e) =>
            {
                content.Width = Math.Min(720, scrollFormTab.ClientSize.Width - 40);
                content.Location = new Point(
                    Math.Max(20, (scrollFormTab.ClientSize.Width - content.Width) / 2), 24);
            };

            int cardW = 720;
            int fieldH = 42;
            int gap = 14;
            int cardX = 0;
            int curY = 0;

            // ── Card 1: Incident Details ──────────────────────────────────────
            var c1 = MakeCard(cardX, curY, cardW, 0, "Incident Details", "\ue668");
            int fy = 64;


            // Province
            AddFieldLabel(c1, "Province", 24, fy);
            cboProvince = MakeCombo(c1, 24, fy + 26, cardW - 48, Provinces);
            fy += 26 + fieldH + gap; // BUG 1 FIX: advance fy before next field

            // District
            AddFieldLabel(c1, "District", 24, fy);
            cboDistrict = MakeCombo(c1, 24, fy + 26, cardW - 48, Districts);
            fy += 26 + fieldH + gap; 

            // SAPS Station
            AddFieldLabel(c1, "SAPS Station", 24, fy);
            cboStation = MakeCombo(c1, 24, fy + 26, cardW - 48, Stations);
            fy += 26 + fieldH + gap;

            // AR Number
            AddFieldLabel(c1, "AR Number", 24, fy);
            txtARNumber = MakeText(c1, 24, fy + 26, cardW - 48, "e.g. 2024/001234");
            fy += 26 + fieldH + gap;

            
            AddFieldLabel(c1, "Date", 24, fy);
            dtAccidentDate = MakeDate(c1, 24, fy + 26, 280);
            fy += 26 + fieldH + gap;

            AddFieldLabel(c1, "Day", 24, fy);
            cboDays = MakeCombo(c1, 24, fy + 26, 280, DaysOfTheWeek);
            fy += 26 + fieldH + gap;

            // Time
            AddFieldLabel(c1, "Time (e.g. 14:30)", 24, fy);
            tdAccidentTime = MakeTime(c1, 24, fy + 26, 280);
            fy += 26 + fieldH + gap;

            // Route
            AddFieldLabel(c1, "Route", 24, fy);
            cboRoute = MakeCombo(c1, 24, fy + 26, cardW - 48, Routes);
            fy += 26 + fieldH + gap;

            // Location
            AddFieldLabel(c1, "Location Description", 24, fy);
            txtLocation = MakeText(c1, 24, fy + 26, cardW - 48);
            fy += 26 + fieldH + gap;

            // Accident Type
            AddFieldLabel(c1, "Accident Type", 24, fy);
            cboType = MakeCombo(c1, 24, fy + 26, cardW - 48, AccidentTypes);
            fy += 26 + fieldH + gap;

            // Vehicles Involved (editable combo — user can type "OTHER …")
            AddFieldLabel(c1, "Vehicles Involved", 24, fy);
            cboVehicles = new ComboBoxEdit
            {
                Location = new Point(24, fy + 26),
                Size = new Size(cardW - 48, fieldH),
                Properties = { TextEditStyle = TextEditStyles.Standard, AutoComplete = true }
            };
            cboVehicles.Properties.Items.AddRange(VehiclesInvolvedList);
            cboVehicles.Properties.Items.Add("OTHER (Please specify)");
            StyleEdit(cboVehicles, fieldH);
            c1.Controls.Add(cboVehicles);
            fy += 26 + fieldH + 8;

            // Abbreviation legend
            c1.Controls.Add(new LabelControl
            {
                Text = "SED = Sedan  ·  LDV = Light Delivery Vehicle  ·  P/D = Pedestrian  ·  ART = Articulated Truck  ·  M/C = Motorcycle",
                Location = new Point(24, fy),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(cardW - 48, 18)
            });
            fy += 28;

            SetCardHeight(c1, fy);
            curY += fy + 16;

            // ── Card 2: Casualties ────────────────────────────────────────────
            var c2 = MakeCard(cardX, curY, cardW, 0, "Casualties", "\ue3f4");
            int casH = BuildCasualtiesSection(c2, cardW);
            SetCardHeight(c2, casH);
            curY += casH + 16;

            // ── Action buttons ────────────────────────────────────────────────
            int btnW = (cardW - 24) / 3;
            var btnSave = MakeBigButton("Save to Database", Navy, new Point(cardX, curY), btnW, 48);
            var btnPdf = MakeBigButton("Export to PDF", Blue, new Point(cardX + btnW + 12, curY), btnW, 48);
            var btnClear = MakeBigButton("Clear Form", Color.FromArgb(140, 148, 160), new Point(cardX + (btnW + 12) * 2, curY), btnW, 48);

            btnSave.Click += BtnSave_Click;
            btnPdf.Click += BtnPdf_Click;
            btnClear.Click += (s, e) => ClearForm();


            curY += 48 + 32;
            content.Height = curY;
            content.Controls.AddRange(new Control[] { c1, c2, btnSave, btnPdf, btnClear });
            scrollFormTab.Controls.Add(content);
            scrollFormTab.PerformLayout();
            page.Controls.Add(scrollFormTab);
        }

      //Monthly report Tab
        private void BuildMonthlyReportTab(XtraTabPage page)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Surface };

            // ── Filter / control bar ──────────────────────────────────────────
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = CardBg,
                Padding = new Padding(20)
            };
            MakeCardBorder(filterPanel);

            var lblMonth = new LabelControl
            {
                Text = "Select Month",
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 10),
                Size = new Size(100, 26),
                ForeColor = TextPrimary
            };

            dtReportMonth = new DateEdit
            {
                Location = new Point(20, 50),
                Size = new Size(250, 38),
                DateTime = DateTime.Today.AddMonths(-1),
                Properties =
        {
            Mask =
            {
                EditMask = "MMMM yyyy",
                MaskType = DevExpress.XtraEditors.Mask.MaskType.DateTime
            },
            CalendarTimeEditing = DevExpress.Utils.DefaultBoolean.False
        }
            };
            StyleEdit(dtReportMonth, 38);

            var btnLoadData = new SimpleButton
            {
                Text = "Load Data",
                Location = new Point(290, 50),
                Size = new Size(120, 38),
                Appearance = { BackColor = Navy, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) }
            };
            btnLoadData.Click += BtnLoadMonthlyData_Click;

            btnGenerateReport = new SimpleButton
            {
                Text = "Generate Report",
                Location = new Point(430, 50),
                Size = new Size(140, 38),
                Appearance = { BackColor = Blue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                Enabled = false
            };
            btnGenerateReport.Click += BtnGenerateMonthlyReport_Click;

            btnSaveReport = new SimpleButton
            {
                Text = "Save Report",
                Location = new Point(580, 50),
                Size = new Size(120, 38),
                Appearance = { BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                Enabled = false
            };
            btnSaveReport.Click += BtnSaveMonthlyReport_Click;

            filterPanel.Controls.AddRange(new Control[]
            {
        lblMonth, dtReportMonth, btnLoadData, btnGenerateReport, btnSaveReport
            });

            // ── Content area ──────────────────────────────────────────────────
            // SplitContainer throws InvalidOperationException when SplitterDistance
            // is set before the control has a real width (common inside XtraTabPages
            // with DockStyle.Fill children).  We replace it with two plain Panels
            // positioned manually via a Resize handler — identical visual result,
            // zero runtime exceptions.
            var contentArea = new Panel { Dock = DockStyle.Fill, BackColor = Surface };

            // ── Left panel: scrollable text report ───────────────────────────
            var leftPanel = new Panel
            {
                Location = new Point(0, 0),
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            rtbReport = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 10)
            };
            leftPanel.Controls.Add(rtbReport);

            // ── Right panel: charts on top, summary grid below ────────────────
            var rightPanel = new Panel
            {
                Location = new Point(0, 0),
                BackColor = Surface
            };

            var chartPanel = new Panel
            {
                Height = 250,
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                BackColor = Surface
            };
            chartMonthlyCrashes = new ChartControl { Size = new Size(400, 220), Location = new Point(10, 15) };
            chartMonthlyFatalities = new ChartControl { Size = new Size(400, 220), Location = new Point(420, 15) };
            chartPanel.Controls.AddRange(new Control[] { chartMonthlyCrashes, chartMonthlyFatalities });

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            gridMonthlyReport = new GridControl { Dock = DockStyle.Fill };
            gridViewMonthlyReport = new GridView(gridMonthlyReport)
            {
                OptionsView = { ShowGroupPanel = false, ColumnAutoWidth = true },
                OptionsBehavior = { Editable = false }
            };
            gridViewMonthlyReport.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            gridMonthlyReport.MainView = gridViewMonthlyReport;
            gridPanel.Controls.Add(gridMonthlyReport);

            // Add Fill child before Top child so WinForms dock order is correct
            rightPanel.Controls.Add(gridPanel);
            rightPanel.Controls.Add(chartPanel);

            contentArea.Controls.Add(leftPanel);
            contentArea.Controls.Add(rightPanel);

            // ── Layout: keep both panels filling contentArea side by side ─────
            // Called on every resize so the split tracks the window width.
            const int gap = 4; // thin visual separator between the two halves
            void LayoutPanels()
            {
                int w = contentArea.ClientSize.Width;
                int h = contentArea.ClientSize.Height;
                if (w <= 0 || h <= 0) return;

                int leftW = Math.Max(300, (w - gap) / 2);
                int rightW = Math.Max(300, w - leftW - gap);

                leftPanel.SetBounds(0, 0, leftW, h);
                rightPanel.SetBounds(leftW + gap, 0, rightW, h);
            }

            contentArea.Resize += (s, e) => LayoutPanels();

            // Also run once when the tab becomes visible for the first time
            // so panels are sized correctly before the user interacts with them.
            page.VisibleChanged += (s, e) =>
            {
                if (page.Visible) LayoutPanels();
            };

            // Add Fill child before Top child
            panel.Controls.Add(contentArea);
            panel.Controls.Add(filterPanel);
            page.Controls.Add(panel);
        }

       //Casualties Section
        private int BuildCasualtiesSection(Panel card, int cardW)
        {
            int colW = (cardW - 48 - 120 - 36) / 4;
            int c0 = 24, c1 = 144,
                c2 = c1 + colW + 12,
                c3 = c2 + colW + 12,
                c4 = c3 + colW + 12;
            int rh = 52, headerY = 58, firstRowY = 94;

            foreach (var (txt, cx) in new (string, int)[]
                { ("Drivers", c1), ("Passengers", c2), ("Pedestrians", c3), ("Cyclists", c4) })
            {
                card.Controls.Add(new LabelControl
                {
                    Text = txt,
                    Location = new Point(cx, headerY),
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Size = new Size(colW, 18),
                    Appearance = { TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
                });
            }

            int r1 = firstRowY,
                r2 = r1 + rh + 10,
                r3 = r2 + rh + 10;

            CasRow(card, "FATAL", Red, c0, r1, c1, c2, c3, c4, colW, out spnFD, out spnFP, out spnFPD, out spnFC);
            CasRow(card, "SERIOUS", Amber, c0, r2, c1, c2, c3, c4, colW, out spnSD, out spnSP, out spnSPD, out spnSC);
            CasRow(card, "SLIGHT", Green, c0, r3, c1, c2, c3, c4, colW, out spnSLD, out spnSLP, out spnSLPD, out spnSLC);

            int divY = r3 + rh + 16;
            card.Controls.Add(new Panel
            { Location = new Point(24, divY), Size = new Size(cardW - 48, 1), BackColor = Border });

            card.Controls.Add(new LabelControl
            {
                Text = "Total casualties",
                Location = new Point(24, divY + 14),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 10),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(200, 26)
            });

            lblTotalCasualties = new LabelControl
            {
                Text = "0",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Navy,
                Location = new Point(cardW - 100, divY + 8),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(76, 36),
                Appearance = { TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Far } }
            };
            card.Controls.Add(lblTotalCasualties);

            foreach (var s in new[]
                { spnFD,spnFP,spnFPD,spnFC, spnSD,spnSP,spnSPD,spnSC, spnSLD,spnSLP,spnSLPD,spnSLC })
                s.EditValueChanged += (_, __) => RecalcTotal();

            return divY + 60;
        }

        private void CasRow(Panel card, string severity, Color col,
            int lx, int y, int c1, int c2, int c3, int c4, int colW,
            out SpinEdit s1, out SpinEdit s2, out SpinEdit s3, out SpinEdit s4)
        {
            var pill = new Panel
            {
                Location = new Point(lx, y + 4),
                Size = new Size(112, 28),
                BackColor = Blend(col, 30)
            };
            pill.Controls.Add(new LabelControl
            {
                Text = severity,
                ForeColor = DarkenColor(col),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Location = new Point(0, 5),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(112, 20),
                BackColor = Color.Transparent,
                Appearance = { TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
            });
            RoundControl(pill, 14);
            card.Controls.Add(pill);

            s1 = MakeSpin(c1, y, colW); s2 = MakeSpin(c2, y, colW);
            s3 = MakeSpin(c3, y, colW); s4 = MakeSpin(c4, y, colW);
            card.Controls.AddRange(new Control[] { s1, s2, s3, s4 });
        }

        private void RecalcTotal()
        {
            if (lblTotalCasualties == null) return;
            int t = (int)(spnFD.Value + spnFP.Value + spnFPD.Value + spnFC.Value
                        + spnSD.Value + spnSP.Value + spnSPD.Value + spnSC.Value
                        + spnSLD.Value + spnSLP.Value + spnSLPD.Value + spnSLC.Value);
            lblTotalCasualties.Text = t.ToString();
            lblTotalCasualties.ForeColor = t == 0 ? TextMuted : t > 5 ? Red : Navy;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  REPORTS TAB
        // ═════════════════════════════════════════════════════════════════════
        private void BuildReportsTab(XtraTabPage page)
        {
            scrollReportsTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Surface };

            // ── Filter card ───────────────────────────────────────────────────
            var filterCard = MakeCard(16, 16, 1060, 86, "Filter", "\ue8b6");
            filterCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            AddFieldLabel(filterCard, "From", 24, 42);
            dtFrom = MakeDate(filterCard, 24, 64, 160);
            dtFrom.DateTime = DateTime.Today.AddMonths(-6);

            AddFieldLabel(filterCard, "To", 200, 42);
            dtTo = MakeDate(filterCard, 200, 64, 160);
            dtTo.DateTime = DateTime.Today;

            AddFieldLabel(filterCard, "Station", 376, 42);
            cboFilterStation = new ComboBoxEdit { Location = new Point(376, 64), Size = new Size(190, 38) };
            cboFilterStation.Properties.Items.Add("(All Stations)");
            cboFilterStation.Properties.Items.AddRange(Stations);
            cboFilterStation.SelectedIndex = 0;
            StyleEdit(cboFilterStation, 38);
            filterCard.Controls.Add(cboFilterStation);

            AddFieldLabel(filterCard, "Type", 582, 42);
            cboFilterType = new ComboBoxEdit { Location = new Point(582, 64), Size = new Size(190, 38) };
            cboFilterType.Properties.Items.Add("(All Types)");
            cboFilterType.Properties.Items.AddRange(AccidentTypes);
            cboFilterType.SelectedIndex = 0;
            StyleEdit(cboFilterType, 38);
            filterCard.Controls.Add(cboFilterType);

            var btnLoad = MakeSmallBtn("Load Data", Navy, new Point(792, 58));
            var btnExport = MakeSmallBtn("Export PDF", Blue, new Point(900, 58));
            var btnImport = MakeSmallBtn("Import Excel", Color.FromArgb(130, 60, 180), new Point(1008, 58));
            btnLoad.Click += (s, e) => LoadReportData();
            btnExport.Click += BtnExportReport_Click;
            btnImport.Click += BtnImportExcel_Click;
            filterCard.Controls.AddRange(new Control[] { btnLoad, btnExport, btnImport });
            SetCardHeight(filterCard, 108);

            // ── KPI row ───────────────────────────────────────────────────────
            var kpiPanel = new Panel
            {
                Location = new Point(16, 136),
                Size = new Size(1060, 94),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            int kw = 252, kg = 12;
            lblKpiAccidents = MakeKpiCard(kpiPanel, "TOTAL ACCIDENTS", "—", Navy, 0, kw);
            lblKpiFatal = MakeKpiCard(kpiPanel, "FATAL CASUALTIES", "—", Red, kw + kg, kw);
            lblKpiSerious = MakeKpiCard(kpiPanel, "SERIOUS INJURIES", "—", Amber, (kw + kg) * 2, kw);
            lblKpiSlight = MakeKpiCard(kpiPanel, "SLIGHT INJURIES", "—", Green, (kw + kg) * 3, kw);

            // ── Chart cards ───────────────────────────────────────────────────
            var chartCard1 = MakeCard(16, 242, 516, 310, "Accidents by Type", "\ue3ec");
            var chartCard2 = MakeCard(548, 242, 528, 310, "Casualty Severity Split", "\ue3ec");
            var chartCard3 = MakeCard(16, 568, 516, 310, "Top 10 Stations", "\ue3ec");
            var chartCard4 = MakeCard(548, 568, 528, 310, "Monthly Trend", "\ue3ec");

            foreach (var c in new[] { chartCard1, chartCard2, chartCard3, chartCard4 })
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            chartByType = EmbedBarChart(chartCard1, 12, 52, 492, 246);
            chartBySeverity = EmbedPieChart(chartCard2, 12, 52, 504, 246);
            chartByStation = EmbedBarChart(chartCard3, 12, 52, 492, 246);
            chartByMonth = EmbedLineChart(chartCard4, 12, 52, 504, 246);

            // ── Detailed records grid ─────────────────────────────────────────
            var gridCard = MakeCard(16, 894, 1060, 360, "Detailed Records", "\ue8ef");
            gridCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            grid = new GridControl { Location = new Point(12, 52), Size = new Size(1036, 294) };
            gridView = new GridView(grid)
            {
                OptionsView = { ShowGroupPanel = false, ColumnAutoWidth = true },
                OptionsBehavior = { Editable = false }
            };
            gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            gridView.Appearance.HeaderPanel.BackColor = Surface;
            gridView.Appearance.HeaderPanel.ForeColor = TextMuted;
            gridView.OptionsView.EnableAppearanceEvenRow = true;
            gridView.Appearance.EvenRow.BackColor = Color.FromArgb(249, 250, 252);
            gridView.Appearance.Row.Font = new Font("Segoe UI", 9);
            gridView.Appearance.Row.ForeColor = TextPrimary;
            gridView.RowStyle += GridView_RowStyle;
            grid.MainView = gridView;
            gridCard.Controls.Add(grid);

            scrollReportsTab.Controls.AddRange(new Control[]
            {
                filterCard, kpiPanel,
                chartCard1, chartCard2, chartCard3, chartCard4,
                gridCard
            });

            scrollReportsTab.Resize += (s, e) => LayoutReportsTab(
                scrollReportsTab, filterCard, kpiPanel,
                chartCard1, chartCard2, chartCard3, chartCard4, gridCard);

            scrollReportsTab.PerformLayout();
            page.Controls.Add(scrollReportsTab);
        }

        private void LayoutReportsTab(Panel scroll, Panel filterCard, Panel kpiPanel,
            Panel cc1, Panel cc2, Panel cc3, Panel cc4, Panel gridCard)
        {
            if (scroll.ClientSize.Width <= 0) return;
            int w = Math.Max(scroll.ClientSize.Width - 32, 600);
            int half = (w - 12) / 2;
            int kw = (w - 36) / 4;
            int kg = 12;

            filterCard.Size = new Size(w, filterCard.Height);
            kpiPanel.Size = new Size(w, 94);

            for (int i = 0; i < kpiPanel.Controls.Count; i++)
            {
                var kpi = kpiPanel.Controls[i];
                kpi.Size = new Size(kw, 88);
                kpi.Location = new Point(i * (kw + kg), 0);
            }

            cc1.Location = new Point(16, 242); cc1.Size = new Size(half, cc1.Height);
            cc2.Location = new Point(16 + half + 12, 242); cc2.Size = new Size(half, cc2.Height);
            cc3.Location = new Point(16, 568); cc3.Size = new Size(half, cc3.Height);
            cc4.Location = new Point(16 + half + 12, 568); cc4.Size = new Size(half, cc4.Height);

            gridCard.Location = new Point(16, 894);
            gridCard.Size = new Size(w, gridCard.Height);
            if (grid != null) grid.Size = new Size(w - 24, grid.Height);
        }

        private void GridView_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            try
            {
                var fatal = gridView.GetRowCellValue(e.RowHandle, "Fatal");
                if (fatal != null && Convert.ToInt32(fatal) > 0)
                {
                    e.Appearance.BackColor = RedLight;
                    e.Appearance.ForeColor = Color.FromArgb(120, 20, 20);
                    e.HighPriority = true;
                }
            }
            catch { }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  DATA
        // ═════════════════════════════════════════════════════════════════════
        private void LoadReportData()
        {
            try
            {
                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (dtFrom.DateTime != DateTime.MinValue)
                    fromDate = dtFrom.DateTime;

                if (dtTo.DateTime != DateTime.MinValue)
                    toDate = dtTo.DateTime;

                string station = cboFilterStation.SelectedIndex <= 0 ? null : cboFilterStation.Text;
                string type = cboFilterType.SelectedIndex <= 0 ? null : cboFilterType.Text;
                string province = cboProvince?.SelectedIndex <= 0 ? null : cboProvince.Text;
                string district = cboDistrict?.SelectedIndex <= 0 ? null : cboDistrict.Text;
                string day = cboDays?.SelectedIndex <= 0 ? null : cboDays.Text;

                var list = DatabaseHelper.GetReports(fromDate, toDate, station, type, province, district).ToList();

               
                grid.DataSource = list.Select(r => new
                {
                    r.Id,
                    Station = r.SAPSStation,
                    r.ARNumber,
                    Date = r.AccidentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    Day = r.DayOfWeek,
                    Time = r.AccidentTime.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture),
                    r.Route,
                    r.Location,
                    Type = r.AccidentType,
                    Province = r.ProvinceName,
                    District = r.DistrictMunicipalityName,
                    Fatal = r.TotalFatal,
                    Serious = r.TotalSerious,
                    Slight = r.TotalSlight,
                    Total = r.GrandTotal,
                    Vehicles = r.VehiclesInvolved
                }).ToList();

                
                int tf = list.Sum(r => r.TotalFatal);
                int ts = list.Sum(r => r.TotalSerious);
                int tsl = list.Sum(r => r.TotalSlight);

                lblKpiAccidents.Text = list.Count.ToString("N0");
                lblKpiFatal.Text = tf.ToString("N0");
                lblKpiSerious.Text = ts.ToString("N0");
                lblKpiSlight.Text = tsl.ToString("N0");

              
            }
            catch (Exception ex)
            {
                ShowErr("Error loading data", ex);
            }
        }


        private static ChartControl EmbedBarChart(Panel card, int x, int y, int w, int h)
        {
            var chart = BaseChart(card, x, y, w, h);
            var s = new Series("Data", ViewType.Bar);
            s.Label.Visible = false;
            chart.Series.Add(s);
            chart.Legend.Visible = false;
            StyleXY((XYDiagram)chart.Diagram);
            return chart;
        }

        private static ChartControl EmbedPieChart(Panel card, int x, int y, int w, int h)
        {
            var chart = BaseChart(card, x, y, w, h);
            var s = new Series("Data", ViewType.Pie);
            var v = (PieSeriesView)s.View;
            v.ExplodeMode = PieExplodeMode.MinValue;
            v.ExplodedDistancePercentage = 5;
            s.Label.TextPattern = "{A}\n{VP:P0}";
            s.Label.Font = new Font("Segoe UI", 7);
            chart.Series.Add(s);
            chart.Legend.Visible = true;
            chart.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Center;
            chart.Legend.AlignmentVertical = LegendAlignmentVertical.BottomOutside;
            chart.Legend.Font = new Font("Segoe UI", 8);
            chart.Legend.BackColor = Color.Transparent;
            chart.Legend.Border.Visible = false;
            return chart;
        }

        private static ChartControl EmbedLineChart(Panel card, int x, int y, int w, int h)
        {
            var chart = BaseChart(card, x, y, w, h);
            var s = new Series("Trend", ViewType.Line);
            s.Label.Visible = false;
            var v = (LineSeriesView)s.View;
            v.LineStyle.Thickness = 2;
            v.Color = Navy;
            v.MarkerVisibility = DevExpress.Utils.DefaultBoolean.True;
            v.LineMarkerOptions.Size = 7;
            v.LineMarkerOptions.Kind = MarkerKind.Circle;
            chart.Series.Add(s);
            chart.Legend.Visible = false;
            StyleXY((XYDiagram)chart.Diagram);
            return chart;
        }

        private static ChartControl BaseChart(Panel card, int x, int y, int w, int h)
        {
            var chart = new ChartControl
            { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.White };
            card.Controls.Add(chart);
            return chart;
        }

        private static void StyleXY(XYDiagram d)
        {
            d.AxisX.Label.Angle = -35;
            d.AxisX.Label.Font = new Font("Segoe UI", 7);
            d.AxisX.GridLines.Visible = false;
            d.AxisX.Color = Border;
            d.AxisY.Label.Font = new Font("Segoe UI", 7);
            d.AxisY.Color = Border;
            d.AxisY.GridLines.Color = Color.FromArgb(235, 237, 240);
            d.AxisY.Title.Visibility = DevExpress.Utils.DefaultBoolean.True;
            d.AxisY.Title.Text = "Count";
            d.AxisY.Title.Font = new Font("Segoe UI", 7, FontStyle.Bold);
            d.EnableAxisXScrolling = false;
            d.EnableAxisYScrolling = false;
        }

        private static void RefreshBarChart(ChartControl chart, string[] labels, double[] values, Color color)
        {
            chart.Series.Clear();
            var s = new Series("Data", ViewType.Bar);
            s.Label.Visible = false;
            var v = (BarSeriesView)s.View;
            v.Color = color;
            v.BarWidth = 0.55;
            v.FillStyle.FillMode = DevExpress.XtraCharts.FillMode.Solid;
            for (int i = 0; i < labels.Length; i++)
                s.Points.Add(new SeriesPoint(labels[i], values[i]));
            chart.Series.Add(s);
            chart.Legend.Visible = false;
            if (chart.Diagram is XYDiagram d)
            {
                d.AxisX.Label.Angle = -35;
                d.AxisX.Label.Font = new Font("Segoe UI", 7);
                d.AxisY.WholeRange.AlwaysShowZeroLevel = true;
            }
        }

        private static void RefreshPieChart(ChartControl chart, string[] labels, double[] values, Color[] colors)
        {
            chart.Series.Clear();
            var s = new Series("Casualties", ViewType.Pie);
            var v = (PieSeriesView)s.View;
            v.ExplodeMode = PieExplodeMode.MinValue;
            v.ExplodedDistancePercentage = 5;
            s.Label.TextPattern = "{A}\n{VP:P0}";
            s.Label.Font = new Font("Segoe UI", 7);
            for (int i = 0; i < labels.Length; i++)
                s.Points.Add(new SeriesPoint(labels[i], values[i]) { Color = colors[i] });
            chart.Series.Add(s);
            chart.Legend.Visible = true;
            chart.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Center;
            chart.Legend.AlignmentVertical = LegendAlignmentVertical.BottomOutside;
            chart.Legend.Font = new Font("Segoe UI", 8);
            chart.Legend.BackColor = Color.Transparent;
            chart.Legend.Border.Visible = false;
        }

        private static void RefreshLineChart(ChartControl chart, string[] labels, double[] values)
        {
            chart.Series.Clear();
            var s = new Series("Trend", ViewType.Line);
            s.Label.Visible = false;
            var v = (LineSeriesView)s.View;
            v.LineStyle.Thickness = 2;
            v.Color = Navy;
            v.MarkerVisibility = DevExpress.Utils.DefaultBoolean.True;
            v.LineMarkerOptions.Size = 7;
            v.LineMarkerOptions.Kind = MarkerKind.Circle;
            for (int i = 0; i < labels.Length; i++)
                s.Points.Add(new SeriesPoint(labels[i], values[i]));
            chart.Series.Add(s);
            chart.Legend.Visible = false;
            if (chart.Diagram is XYDiagram d)
            {
                d.AxisX.Label.Angle = -35;
                d.AxisX.Label.Font = new Font("Segoe UI", 7);
                d.AxisY.WholeRange.AlwaysShowZeroLevel = true;
            }
        }

        
        private LabelControl MakeKpiCard(Panel parent, string title, string value,
            Color accent, int x, int w)
        {
            var card = new Panel { Location = new Point(x, 0), Size = new Size(w, 88), BackColor = CardBg };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(new Rectangle(0, 0, card.Width, card.Height), 12);
                g.FillPath(new SolidBrush(CardBg), path);
                g.DrawPath(new Pen(Border, 0.5f), path);
                g.FillRectangle(new SolidBrush(accent), 0, 0, 5, card.Height);
            };
            card.Region = RoundRegion(card.Size, 12);

            card.Controls.Add(new LabelControl
            {
                Text = title,
                Location = new Point(16, 10),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(w - 20, 14)
            });
            var valLbl = new LabelControl
            {
                Text = value,
                Location = new Point(16, 26),
                ForeColor = accent,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(w - 20, 36)
            };
            card.Controls.Add(valLbl);
            parent.Controls.Add(card);
            return valLbl;
        }

       
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(out string msg))
            { XtraMessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                var r = BuildReport();
                DatabaseHelper.InsertReport(r);
                XtraMessageBox.Show($"Saved.\nAR: {r.ARNumber}  Station: {r.SAPSStation}",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (XtraMessageBox.Show("Export to PDF now?", "PDF",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    DoPdfExport(r);
            }
            catch (Exception ex) { ShowErr("Save failed", ex); }
        }

        private void BtnPdf_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(out string msg))
            { XtraMessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            DoPdfExport(BuildReport());
        }

        private void BtnExportReport_Click(object sender, EventArgs e)
        {
            if (grid.DataSource == null)
            { XtraMessageBox.Show("Load data first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var pd = new PrintDocument { DocumentName = "Accident Summary Report" };
            pd.PrintPage += (s, ev) =>
            {
                ev.Graphics.DrawString("VEHICLE ACCIDENT SUMMARY REPORT",
                    new Font("Segoe UI", 16, FontStyle.Bold), new SolidBrush(Navy), 40, 40);
                ev.Graphics.DrawString($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}",
                    new Font("Segoe UI", 9), new SolidBrush(TextMuted), 40, 70);
                ev.HasMorePages = false;
            };
            using var dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK) pd.Print();
        }

        private void BtnImportExcel_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Title = "Select Excel File", Filter = "Excel Files|*.xlsx;*.xls" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                var records = ExcelImporter.Parse(dlg.FileName);
                var (ins, skip) = DatabaseHelper.BulkImport(records);
                XtraMessageBox.Show($"Import done!\nInserted: {ins}\nSkipped:  {skip}",
                    "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReportData();
            }
            catch (Exception ex) { ShowErr("Import failed", ex); }
            finally { Cursor = Cursors.Default; }
        }

        private void DoPdfExport(AccidentReportModel r)
        {
            using var save = new SaveFileDialog
            {
                Title = "Save PDF",
                Filter = "PDF Files|*.pdf",
                FileName = $"AR_{r.ARNumber}_{r.SAPSStation}_{DateTime.Today:yyyyMMdd}.pdf"
            };
            if (save.ShowDialog() != DialogResult.OK) return;
            using var pd = new PrintDocument { DocumentName = "Vehicle Accident Report" };
            bool printed = false;
            pd.PrintPage += (s, ev) =>
            {
                if (printed) return;
                printed = true;
                RenderReportPage(ev.Graphics, r, ev.PageBounds);
                ev.HasMorePages = false;
            };
            string pdfPrinter = null;
            foreach (string p in PrinterSettings.InstalledPrinters)
                if (p.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0) { pdfPrinter = p; break; }
            if (pdfPrinter != null)
            {
                pd.PrinterSettings.PrinterName = pdfPrinter;
                pd.PrinterSettings.PrintFileName = save.FileName;
                pd.PrinterSettings.PrintToFile = true;
                pd.Print();
                XtraMessageBox.Show("PDF saved to:\n" + save.FileName, "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                using var dlg2 = new PrintDialog { Document = pd };
                XtraMessageBox.Show("Select 'Microsoft Print to PDF' in the next dialog.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (dlg2.ShowDialog() == DialogResult.OK) pd.Print();
            }
        }

        private static void RenderReportPage(Graphics g, AccidentReportModel r, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float x = 40, y = 40, w = bounds.Width - 80;
            g.FillRectangle(new SolidBrush(Navy), x, y, w, 56);
            g.DrawString("VEHICLE ACCIDENT REPORT",
                new Font("Segoe UI", 16, FontStyle.Bold), Brushes.White,
                new RectangleF(x + 14, y + 14, w - 20, 32));
            y += 68;

            void F(string lbl, string val, float fx, float fy, float fw)
            {
                g.DrawString(lbl, new Font("Segoe UI", 7, FontStyle.Bold), new SolidBrush(TextMuted), fx, fy);
                g.DrawString(val, new Font("Segoe UI", 9), new SolidBrush(TextPrimary), fx, fy + 12);
                g.DrawLine(new Pen(Border), fx, fy + 28, fx + fw, fy + 28);
            }
            float half = w / 2f - 10;
            F("SAPS STATION", r.SAPSStation, x, y, half);
            F("AR NUMBER", r.ARNumber, x + half + 20, y, half);
            y += 40;
            F("DATE", r.AccidentDate.ToString("dd MMMM yyyy"), x, y, half);
            F("DAY", r.DayOfWeek, x + half + 20, y, half / 2);
            F("TIME", r.AccidentTime.ToString("HH:mm"), x + half + 20 + half / 2 + 20, y, half / 2);
            y += 40;
            F("ROUTE", r.Route, x, y, half);
            F("ACCIDENT TYPE", r.AccidentType, x + half + 20, y, half);
            y += 40;
            F("LOCATION", r.Location, x, y, w); y += 40;
            F("VEHICLES INVOLVED", r.VehiclesInvolved, x, y, w); y += 40;
            F("PROVINCE", r.ProvinceName, x, y, half);
            F("DISTRICT", r.DistrictMunicipalityName, x + half + 20, y, half);
            y += 50;

            g.DrawString("CASUALTIES", new Font("Segoe UI", 10, FontStyle.Bold),
                new SolidBrush(Navy), x, y);
            y += 20;

            float c0 = x, c1 = x + 130, c2 = x + 240, c3 = x + 360, c4 = x + 480, c5 = x + 590;
            int rh = 24;
            g.FillRectangle(new SolidBrush(Navy), x, y, w, rh);
            foreach (var (txt, cx) in new (string, float)[]
                { ("", c0+4), ("DRIVERS",c1+4), ("PASSENGERS",c2+4),
                  ("PEDESTRIANS",c3+4), ("CYCLISTS",c4+4), ("TOTAL",c5+4) })
                g.DrawString(txt, new Font("Segoe UI", 7, FontStyle.Bold), Brushes.White, cx, y + 5);
            y += rh;

            void Row(string lbl, Color col, int d, int p, int pd2, int cy, int tot)
            {
                g.FillRectangle(new SolidBrush(Blend(col, 30)), x, y, w, rh);
                g.DrawString(lbl, new Font("Segoe UI", 8, FontStyle.Bold),
                    new SolidBrush(DarkenColor(col)), c0 + 4, y + 5);
                foreach (var (v2, cx) in new (int, float)[]
                    { (d,c1),(p,c2),(pd2,c3),(cy,c4),(tot,c5) })
                    g.DrawString(v2.ToString(), new Font("Segoe UI", 9),
                        new SolidBrush(TextPrimary), cx + 4, y + 5);
                g.DrawLine(new Pen(Border), x, y + rh, x + w, y + rh);
                y += rh;
            }
            Row("FATAL", Red, r.FatalDrivers, r.FatalPassengers, r.FatalPedestrians, r.FatalCyclists, r.TotalFatal);
            Row("SERIOUS", Amber, r.SeriousDrivers, r.SeriousPassengers, r.SeriousPedestrians, r.SeriousCyclists, r.TotalSerious);
            Row("SLIGHT", Green, r.SlightDrivers, r.SlightPassengers, r.SlightPedestrians, r.SlightCyclists, r.TotalSlight);
            y += 12;
            g.DrawString($"GRAND TOTAL: {r.GrandTotal} casualt{(r.GrandTotal == 1 ? "y" : "ies")}",
                new Font("Segoe UI", 9, FontStyle.Bold), new SolidBrush(Navy), x, y);

            float fy2 = bounds.Height - 50;
            g.DrawLine(new Pen(Border), x, fy2, x + w, fy2);
            g.DrawString($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}   Ref: AR-{r.ARNumber}-{r.SAPSStation}",
                new Font("Segoe UI", 7), new SolidBrush(TextMuted), x, fy2 + 6);
        }

        
        private bool ValidateForm(out string msg)
        {
            if (cboStation.SelectedIndex < 0 && string.IsNullOrWhiteSpace(cboStation.Text))
            { msg = "Please select a SAPS Station."; return false; }
            if (string.IsNullOrWhiteSpace(txtARNumber.Text))
            { msg = "AR Number is required."; return false; }
            if (cboType.SelectedIndex < 0 && string.IsNullOrWhiteSpace(cboType.Text))
            { msg = "Please select an Accident Type."; return false; }
            if (string.IsNullOrWhiteSpace(cboVehicles.Text))
            { msg = "Please select or enter Vehicles Involved."; return false; }
            msg = null;
            return true;
        }

        private AccidentReportModel BuildReport() => new AccidentReportModel
        {
            SAPSStation = cboStation.Text.Trim(),
            ARNumber = txtARNumber.Text.Trim(),
            AccidentDate = dtAccidentDate.DateTime,
            DayOfWeek = cboDays.Text.Trim(),
            AccidentTime = tdAccidentTime.Time,
            Route = cboRoute.Text.Trim(),
            Location = txtLocation.Text.Trim(),
            AccidentType = cboType.Text.Trim(),

            
            ProvinceName = cboProvince.Text.Trim(),
            DistrictMunicipalityName = cboDistrict.Text.Trim(),
           

            FatalDrivers = (int)spnFD.Value,
            FatalPassengers = (int)spnFP.Value,
            FatalPedestrians = (int)spnFPD.Value,
            FatalCyclists = (int)spnFC.Value,
            SeriousDrivers = (int)spnSD.Value,
            SeriousPassengers = (int)spnSP.Value,
            SeriousPedestrians = (int)spnSPD.Value,
            SeriousCyclists = (int)spnSC.Value,
            SlightDrivers = (int)spnSLD.Value,
            SlightPassengers = (int)spnSLP.Value,
            SlightPedestrians = (int)spnSLPD.Value,
            SlightCyclists = (int)spnSLC.Value,
            VehiclesInvolved = cboVehicles.Text.Trim(),
            CreatedAt = DateTime.Now
        };

        // ═════════════════════════════════════════════════════════════════════
        //  MONTHLY REPORT HANDLERS
        // ═════════════════════════════════════════════════════════════════════
        private void BtnLoadMonthlyData_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var year = dtReportMonth.DateTime.Year;
                var month = dtReportMonth.DateTime.Month;
                var start = new DateTime(year, month, 1);
                var end = start.AddMonths(1).AddDays(-1);

                
                var reports = DatabaseHelper.GetReports(start, end, null, null, null, null).ToList();
                

                if (!reports.Any())
                {
                    XtraMessageBox.Show(
                        $"No accident data found for {dtReportMonth.DateTime:MMMM yyyy}",
                        "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                currentMonthlyData = new MonthlyAccidentData(reports);

                btnGenerateReport.Enabled = true;
                btnSaveReport.Enabled = true;
                

                XtraMessageBox.Show(
                    $"Loaded {reports.Count} accident records for {dtReportMonth.DateTime:MMMM yyyy}\n\n" +
                    $"Crashes: {currentMonthlyData.CurrentYearCrashes:N0}\n" +
                    $"Fatalities: {currentMonthlyData.CurrentYearFatalities:N0}\n" +
                    $"Serious Injuries: {currentMonthlyData.CurrentYearSerious:N0}\n" +
                    $"Slight Injuries: {currentMonthlyData.CurrentYearSlight:N0}",
                    "Data Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        
        private void BtnGenerateMonthlyReport_Click(object sender, EventArgs e)
        {
            if (currentMonthlyData == null)
            {
                XtraMessageBox.Show("Please load data first using 'Load Data' button.",
                    "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Cursor = Cursors.WaitCursor;
            try
            {
                rtbReport.Text = GenerateMonthlyReportText(currentMonthlyData, dtReportMonth.DateTime);
                UpdateMonthlyComparisonChart();
                UpdateMonthlySummaryGrid();
                XtraMessageBox.Show("Monthly report generated successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void UpdateMonthlyComparisonChart()
        {
            chartMonthlyCrashes.Series.Clear();
            var sc = new Series("Crashes", ViewType.Bar);
            sc.Points.Add(new SeriesPoint("Previous Year", currentMonthlyData.PreviousYearCrashes));
            sc.Points.Add(new SeriesPoint("Current Year", currentMonthlyData.CurrentYearCrashes));
            ((BarSeriesView)sc.View).Color = Navy;
            sc.Label.Visible = true;
            sc.Label.TextPattern = "{V}";
            chartMonthlyCrashes.Series.Add(sc);
            chartMonthlyCrashes.Legend.Visible = true;

            chartMonthlyFatalities.Series.Clear();
            var sf = new Series("Fatalities", ViewType.Bar);
            sf.Points.Add(new SeriesPoint("Previous Year", currentMonthlyData.PreviousYearFatalities));
            sf.Points.Add(new SeriesPoint("Current Year", currentMonthlyData.CurrentYearFatalities));
            ((BarSeriesView)sf.View).Color = Red;
            sf.Label.Visible = true;
            sf.Label.TextPattern = "{V}";
            chartMonthlyFatalities.Series.Add(sf);
            chartMonthlyFatalities.Legend.Visible = true;
        }

        private void UpdateMonthlySummaryGrid()
        {
            var summaryData = new[]
            {
                new { Category    = "CRASHES",
                      PreviousYear= currentMonthlyData.PreviousYearCrashes,
                      CurrentYear = currentMonthlyData.CurrentYearCrashes,
                      Variation   = $"{currentMonthlyData.CrashesVariation:+0.00;-0.00}%" },
                new { Category    = "FATALITIES",
                      PreviousYear= currentMonthlyData.PreviousYearFatalities,
                      CurrentYear = currentMonthlyData.CurrentYearFatalities,
                      Variation   = $"{currentMonthlyData.FatalitiesVariation:+0.00;-0.00}%" },
                new { Category    = "SERIOUS INJURIES",
                      PreviousYear= currentMonthlyData.PreviousYearSerious,
                      CurrentYear = currentMonthlyData.CurrentYearSerious,
                      Variation   = $"{currentMonthlyData.SeriousVariation:+0.00;-0.00}%" },
                new { Category    = "SLIGHT INJURIES",
                      PreviousYear= currentMonthlyData.PreviousYearSlight,
                      CurrentYear = currentMonthlyData.CurrentYearSlight,
                      Variation   = $"{currentMonthlyData.SlightVariation:+0.00;-0.00}%" }
            };
            gridMonthlyReport.DataSource = summaryData;

            gridViewMonthlyReport.Columns["Category"].Caption = "Metric";
            gridViewMonthlyReport.Columns["PreviousYear"].Caption = "Previous Year";
            gridViewMonthlyReport.Columns["CurrentYear"].Caption = "Current Year";
            gridViewMonthlyReport.Columns["Variation"].Caption = "Variation";

            gridViewMonthlyReport.Columns["PreviousYear"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridViewMonthlyReport.Columns["PreviousYear"].DisplayFormat.FormatString = "N0";
            gridViewMonthlyReport.Columns["CurrentYear"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridViewMonthlyReport.Columns["CurrentYear"].DisplayFormat.FormatString = "N0";
        }

        
        private void BtnSaveMonthlyReport_Click(object sender, EventArgs e)
        {
            if (currentMonthlyData == null)
            {
                XtraMessageBox.Show("No data to save. Please load data first.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                DatabaseHelper.SaveMonthlyReport(currentMonthlyData, dtReportMonth.DateTime, rtbReport.Text);
                XtraMessageBox.Show("Monthly report saved to database successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error saving report: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateMonthlyReportText(MonthlyAccidentData data, DateTime reportDate)
        {
            var sb = new System.Text.StringBuilder();
            int daysInMonth = DateTime.DaysInMonth(reportDate.Year, reportDate.Month);

            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"MONTHLY ACCIDENT REPORT: {reportDate:MMMM yyyy}");
            sb.AppendLine($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            sb.AppendLine("EXECUTIVE SUMMARY");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"During {reportDate:MMMM yyyy}, {data.CurrentYearCrashes:N0} road crashes took place, ");
            sb.AppendLine($"resulting in {data.CurrentYearFatalities:N0} fatalities, {data.CurrentYearSerious:N0} serious injuries ");
            sb.AppendLine($"and {data.CurrentYearSlight:N0} slight injuries.");
            sb.AppendLine();

            sb.AppendLine("PROVINCIAL CRASHES: MONTH TO MONTH COMPARISON");
            sb.AppendLine($"{reportDate:MMMM yyyy} VS {reportDate.AddYears(-1):MMMM yyyy}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Metric",-20} {"Previous Year",-15} {"Current Year",-15} {"Variation",-15}");
            sb.AppendLine(new string('-', 65));
            sb.AppendLine($"{"CRASHES",-20} {data.PreviousYearCrashes,-15:N0} {data.CurrentYearCrashes,-15:N0} {data.CrashesVariation:+0.00;-0.00}%");
            sb.AppendLine($"{"FATALITIES",-20} {data.PreviousYearFatalities,-15:N0} {data.CurrentYearFatalities,-15:N0} {data.FatalitiesVariation:+0.00;-0.00}%");
            sb.AppendLine($"{"SERIOUS INJURIES",-20} {data.PreviousYearSerious,-15:N0} {data.CurrentYearSerious,-15:N0} {data.SeriousVariation:+0.00;-0.00}%");
            sb.AppendLine($"{"SLIGHT INJURIES",-20} {data.PreviousYearSlight,-15:N0} {data.CurrentYearSlight,-15:N0} {data.SlightVariation:+0.00;-0.00}%");
            sb.AppendLine();

            sb.AppendLine("AVERAGE PER DAY");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"CRASHES",-20} {"FATALITIES",-15} {"SERIOUS INJURIES",-20} {"SLIGHT INJURIES",-20}");
            sb.AppendLine(new string('-', 75));
            sb.AppendLine($"{data.CurrentYearCrashes / (double)daysInMonth,-20:F1}" +
                          $"{data.CurrentYearFatalities / (double)daysInMonth,-15:F1}" +
                          $"{data.CurrentYearSerious / (double)daysInMonth,-20:F1}" +
                          $"{data.CurrentYearSlight / (double)daysInMonth,-20:F1}");
            sb.AppendLine();

            // ── BUG 8 FIX ────────────────────────────────────────────────────
            // The parameter type was declared as VictimCounts, which does not
            // exist.  The correct type is VictimData.
            void VictimSection(string heading, VictimData vc)
            // ─────────────────────────────────────────────────────────────────
            {
                sb.AppendLine(heading);
                sb.AppendLine(new string('-', 80));
                sb.AppendLine($"{"Category",-15} {"Count",-10}");
                sb.AppendLine(new string('-', 25));
                sb.AppendLine($"{"DRIVERS",-15} {vc.Drivers,-10}");
                sb.AppendLine($"{"PASSENGERS",-15} {vc.Passengers,-10}");
                sb.AppendLine($"{"PEDESTRIANS",-15} {vc.Pedestrians,-10}");
                sb.AppendLine($"{"CYCLISTS",-15} {vc.Cyclists,-10}");
                sb.AppendLine($"{"TOTAL",-15} {vc.Total,-10}");
                sb.AppendLine();
            }

            VictimSection("CATEGORIES OF VICTIMS - FATALITIES", data.FatalVictims);
            VictimSection("CATEGORIES OF VICTIMS - SERIOUS INJURIES", data.SeriousVictims);
            VictimSection("CATEGORIES OF VICTIMS - SLIGHT INJURIES", data.SlightVictims);

            sb.AppendLine("PROVINCIAL PREVALENT TIMES");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Time Period",-20} {"Crashes",-15} {"Fatalities",-15}");
            sb.AppendLine(new string('-', 50));
            foreach (var time in data.TimeAnalysis.OrderBy(t => t.Key))
                sb.AppendLine($"{time.Key,-20} {time.Value.Crashes,-15:N0} {time.Value.Fatalities,-15:N0}");
            sb.AppendLine();

            var orderedDays = new[] { "M", "TU", "W", "TH", "FR", "SA", "SU" };
            var dayNames = new System.Collections.Generic.Dictionary<string, string>
            {
                { "M",  "MONDAYS"    }, { "TU", "TUESDAYS"  }, { "W",  "WEDNESDAYS" },
                { "TH", "THURSDAYS"  }, { "FR", "FRIDAYS"   }, { "SA", "SATURDAYS"  },
                { "SU", "SUNDAYS"    }
            };
            sb.AppendLine("PROVINCIAL DAYS OF THE WEEK");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Day",-12} {"Crashes",-15} {"Fatalities",-15}");
            sb.AppendLine(new string('-', 42));
            foreach (var day in orderedDays)
                if (data.DayAnalysis.ContainsKey(day))
                    sb.AppendLine($"{dayNames[day],-12} {data.DayAnalysis[day].Crashes,-15:N0} {data.DayAnalysis[day].Fatalities,-15:N0}");
            sb.AppendLine();

            sb.AppendLine("CONCLUSION");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"• Crashes {(data.CrashesVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.CrashesVariation):F2}%");
            sb.AppendLine($"• Fatalities {(data.FatalitiesVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.FatalitiesVariation):F2}%");
            sb.AppendLine($"• Serious injuries {(data.SeriousVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.SeriousVariation):F2}%");
            sb.AppendLine($"• Slight injuries {(data.SlightVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.SlightVariation):F2}%");
            sb.AppendLine();

            sb.AppendLine("RECOMMENDATIONS");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("• Increase deployment of traffic officers over weekends");
            sb.AppendLine("• Focus on high-risk time periods (14:00 – 06:00)");
            sb.AppendLine("• Target high-risk vehicle categories (Sedans, LDVs, Taxis)");
            sb.AppendLine();

            sb.AppendLine(new string('=', 80));
            sb.AppendLine("END OF REPORT");
            sb.AppendLine(new string('=', 80));
            return sb.ToString();
        }

      
        private void ClearForm()
        {
            cboStation.SelectedIndex = cboType.SelectedIndex =
            cboRoute.SelectedIndex = cboVehicles.SelectedIndex =
            cboProvince.SelectedIndex = cboDistrict.SelectedIndex = -1;

            cboStation.Text = cboType.Text = cboRoute.Text =
            cboVehicles.Text = cboProvince.Text = cboDistrict.Text = string.Empty;

            dtAccidentDate.DateTime = DateTime.Today;
            txtARNumber.Text = string.Empty;
            txtLocation.Text = string.Empty;

            foreach (var s in new[]
                { spnFD,spnFP,spnFPD,spnFC, spnSD,spnSP,spnSPD,spnSC, spnSLD,spnSLP,spnSLPD,spnSLC })
                s.Value = 0;
            RecalcTotal();
        }

        
        private static Panel MakeCard(int x, int y, int w, int h, string title, string _icon)
        {
            var card = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = CardBg };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                g.FillPath(new SolidBrush(CardBg), path);
                g.DrawPath(new Pen(Border, 0.8f), path);
            };
            card.Region = RoundRegion(card.Size, 14);
            card.Controls.Add(new LabelControl
            {
                Text = title,
                Location = new Point(24, 16),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(w - 48, 22)
            });
            var accent = new Panel { Location = new Point(24, 44), Size = new Size(40, 3), BackColor = Navy };
            RoundControl(accent, 2);
            card.Controls.Add(accent);
            return card;
        }

        private static void SetCardHeight(Panel card, int h)
        {
            card.Height = h;
            card.Region = RoundRegion(card.Size, 14);
        }

        private static ComboBoxEdit MakeCombo(Panel parent, int x, int y, int w, string[] items)
        {
            var c = new ComboBoxEdit { Location = new Point(x, y), Size = new Size(w, 42) };
            c.Properties.Items.AddRange(items);
            c.Properties.TextEditStyle = TextEditStyles.Standard;
            StyleEdit(c, 42);
            parent.Controls.Add(c);
            return c;
        }

        private static TextEdit MakeText(Panel parent, int x, int y, int w, string placeholder = "")
        {
            var t = new TextEdit { Location = new Point(x, y), Size = new Size(w, 42) };
            if (!string.IsNullOrEmpty(placeholder)) t.Properties.NullText = placeholder;
            StyleEdit(t, 42);
            parent.Controls.Add(t);
            return t;
        }

        private static DateEdit MakeDate(Panel parent, int x, int y, int w)
        {
            var d = new DateEdit { Location = new Point(x, y), Size = new Size(w, 42) };
            StyleEdit(d, 42);
            parent.Controls.Add(d);
            return d;
        }

        private static TimeEdit MakeTime(Panel parent, int x, int y, int w)
        {
            var t = new TimeEdit { Location = new Point(x, y), Size = new Size(w, 42) };
            StyleEdit(t, 42);
            parent.Controls.Add(t);
            return t;
        }

        private static SpinEdit MakeSpin(int x, int y, int w) => new SpinEdit
        {
            Location = new Point(x, y),
            Size = new Size(w, 42),
            Properties = { MinValue = 0, MaxValue = 999, IsFloatValue = false }
        };

        private static void AddFieldLabel(Panel p, string txt, int x, int y)
        {
            p.Controls.Add(new LabelControl
            {
                Text = txt,
                Location = new Point(x, y),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Size = new Size(300, 20)
            });
        }

        private static void StyleEdit(BaseEdit edit, int height)
        {
            edit.Height = height;
            edit.Font = new Font("Segoe UI", 10);
            edit.Properties.Appearance.BorderColor = Border;
            edit.Properties.AppearanceFocused.BorderColor = Navy;
            edit.Properties.Appearance.Font = new Font("Segoe UI", 10);
        }

        private static SimpleButton MakeBigButton(string txt, Color col, Point loc, int w, int h) =>
            new SimpleButton
            {
                Text = txt,
                Location = loc,
                Size = new Size(w, h),
                Appearance = { BackColor = col, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) }
            };

        private static SimpleButton MakeSmallBtn(string txt, Color col, Point loc) =>
            new SimpleButton
            {
                Text = txt,
                Location = loc,
                Size = new Size(96, 38),
                Appearance = { BackColor = col, ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold) }
            };

        private void MakeCardBorder(Control control)
        {
            control.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
            };
        }

        // ── Rounded graphics helpers ──────────────────────────────────────────
        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private static Region RoundRegion(Size size, int radius) =>
            new Region(RoundRect(new Rectangle(0, 0, size.Width, size.Height), radius));

        private static void RoundControl(Control c, int radius) =>
            c.Region = RoundRegion(c.Size, radius);

        // ── Colour helpers ────────────────────────────────────────────────────
        private static Color Blend(Color c, int alpha) => Color.FromArgb(alpha, c);

        private static Color DarkenColor(Color c) =>
            Color.FromArgb(Math.Max(0, c.R - 55), Math.Max(0, c.G - 55), Math.Max(0, c.B - 55));

        private static string DayCode(DateTime d) => d.DayOfWeek switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            _ => "Sunday"
        };

        private void InitializeComponent() { }

        private static void ShowErr(string title, Exception ex) =>
            XtraMessageBox.Show(title + ":\n\n" + ex.Message, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}