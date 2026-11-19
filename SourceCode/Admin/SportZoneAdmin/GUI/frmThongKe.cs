using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// UC 1.2.6 - Thống kê doanh thu: xem doanh thu theo thời gian (biểu đồ), báo cáo chi tiết theo tiêu chí,
    /// xuất báo cáo ra Excel / PDF / CSV. Doanh thu tính trên các đơn hàng "Đã giao hàng".
    /// </summary>
    public class frmThongKe : Form
    {
        private readonly ThongKeBLL bll = new ThongKeBLL();

        // tab 1
        private readonly ComboBox cboKieu;
        private readonly DateTimePicker dtFrom1;
        private readonly DateTimePicker dtTo1;
        private readonly Chart chart;
        private readonly DataGridView grid1;
        private readonly Label lblTong;
        private readonly Label lblSoDon;
        private readonly Label lblGiam;
        private string subtitle1 = "";
        // tab 2
        private readonly ComboBox cboTieuChi;
        private readonly DateTimePicker dtFrom2;
        private readonly DateTimePicker dtTo2;
        private readonly DataGridView grid2;
        private readonly Label lblSummary2;
        private string subtitle2 = "";

        private static readonly string[] TenKieu = { "Theo ngày", "Theo tuần", "Theo tháng", "Theo quý", "Theo năm" };
        private static readonly string[] TenTieuChi = { "Theo sản phẩm", "Theo danh mục", "Theo khách hàng", "Theo phương thức thanh toán" };

        public frmThongKe()
        {
            UiHelper.SetupChild(this, "Thống kê doanh thu");
            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };

            // ================= TAB 1: DOANH THU THEO THỜI GIAN
            var tab1 = new TabPage("Doanh thu theo thời gian") { BackColor = Color.White };
            FlowLayoutPanel tb1 = UiHelper.MakeToolbar();
            cboKieu = UiHelper.AddTool(tb1, "Kiểu:", UiHelper.MakeCombo(120));
            cboKieu.Items.AddRange(TenKieu);
            cboKieu.SelectedIndex = (int)KieuThongKe.Thang;
            DateTime dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtFrom1 = UiHelper.AddTool(tb1, "Từ ngày:", UiHelper.MakeDate(dauThang.AddMonths(-5)));
            dtTo1 = UiHelper.AddTool(tb1, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            Button btnXem1 = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("Xem thống kê", UiHelper.Primary, 130));
            Button btnExcel1 = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("Xuất Excel", UiHelper.Blue, 110));
            Button btnPdf1 = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("Xuất PDF", UiHelper.Danger, 100));
            btnXem1.Click += (s, e) => XemDoanhThu();
            btnExcel1.Click += (s, e) => Export(grid1, "Báo cáo doanh thu theo thời gian", subtitle1, "excel");
            btnPdf1.Click += (s, e) => Export(grid1, "Báo cáo doanh thu theo thời gian", subtitle1, "pdf");

            var cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(6, 6, 6, 0), BackColor = Color.White };
            lblTong = AddCard(cards, "Tổng doanh thu (thực thu)", UiHelper.Primary);
            lblSoDon = AddCard(cards, "Số đơn hoàn tất", UiHelper.Blue);
            lblGiam = AddCard(cards, "Tổng tiền giảm giá", UiHelper.Orange);

            chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            var area = new ChartArea("main");
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.LabelStyle.Format = "#,##0";
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8.5F);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8.5F);
            chart.ChartAreas.Add(area);
            var series = new Series("Doanh thu")
            {
                ChartType = SeriesChartType.Column,
                Color = UiHelper.Primary,
                ToolTip = "#VALX: #VALY{#,##0} đ"
            };
            chart.Series.Add(series);
            chart.Titles.Add(new Title("Biểu đồ doanh thu", Docking.Top, new Font("Segoe UI", 11F, FontStyle.Bold), Color.FromArgb(33, 37, 41)));

            grid1 = UiHelper.MakeGrid();
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterWidth = 6 };
            split.Panel1.Controls.Add(chart);
            split.Panel2.Controls.Add(grid1);
            tab1.Controls.Add(split);
            tab1.Controls.Add(cards);
            tab1.Controls.Add(tb1);
            split.BringToFront();
            split.SizeChanged += (s, e) =>
            {
                int desired = (int)(split.Height * 0.58);
                if (desired > split.Panel1MinSize && desired < split.Height - split.Panel2MinSize && Math.Abs(split.SplitterDistance - desired) > 40)
                    split.SplitterDistance = desired;
            };

            // ================= TAB 2: BÁO CÁO DOANH THU CHI TIẾT
            var tab2 = new TabPage("Báo cáo doanh thu chi tiết") { BackColor = Color.White };
            FlowLayoutPanel tb2 = UiHelper.MakeToolbar();
            cboTieuChi = UiHelper.AddTool(tb2, "Tiêu chí:", UiHelper.MakeCombo(220));
            cboTieuChi.Items.AddRange(TenTieuChi);
            cboTieuChi.SelectedIndex = 0;
            dtFrom2 = UiHelper.AddTool(tb2, "Từ ngày:", UiHelper.MakeDate(new DateTime(DateTime.Today.Year, 1, 1)));
            dtTo2 = UiHelper.AddTool(tb2, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            Button btnXem2 = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Xem báo cáo", UiHelper.Primary, 120));
            Button btnExcel2 = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Xuất Excel", UiHelper.Blue, 110));
            Button btnPdf2 = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Xuất PDF", UiHelper.Danger, 100));
            Button btnCsv2 = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("CSV", UiHelper.Gray, 70));
            btnXem2.Click += (s, e) => XemBaoCao();
            btnExcel2.Click += (s, e) => Export(grid2, "Báo cáo doanh thu " + cboTieuChi.Text.ToLower(), subtitle2, "excel");
            btnPdf2.Click += (s, e) => Export(grid2, "Báo cáo doanh thu " + cboTieuChi.Text.ToLower(), subtitle2, "pdf");
            btnCsv2.Click += (s, e) => Export(grid2, "Báo cáo doanh thu " + cboTieuChi.Text.ToLower(), subtitle2, "csv");

            lblSummary2 = new Label { Dock = DockStyle.Bottom, Height = 34, Font = UiHelper.BoldFont, ForeColor = UiHelper.Primary, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            grid2 = UiHelper.MakeGrid();
            tab2.Controls.Add(grid2);
            tab2.Controls.Add(lblSummary2);
            tab2.Controls.Add(tb2);
            grid2.BringToFront();

            tabs.TabPages.Add(tab1);
            tabs.TabPages.Add(tab2);
            tabs.SelectedIndexChanged += (s, e) => { if (tabs.SelectedTab == tab2 && grid2.DataSource == null) XemBaoCao(); };
            Controls.Add(tabs);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            XemDoanhThu();
        }

        private static Label AddCard(FlowLayoutPanel parent, string title, Color color)
        {
            var p = new Panel { Width = 270, Height = 78, BackColor = color, Margin = new Padding(0, 0, 12, 0) };
            var value = new Label { Dock = DockStyle.Fill, ForeColor = Color.White, Font = UiHelper.BigNumberFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Text = "0" };
            var caption = new Label { Dock = DockStyle.Top, Height = 26, ForeColor = Color.White, Text = title, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(12, 0, 0, 0) };
            p.Controls.Add(value);
            p.Controls.Add(caption);
            parent.Controls.Add(p);
            return value;
        }

        // ------------------------------------------------------------------ doanh thu theo thời gian
        private void XemDoanhThu()
        {
            var kieu = (KieuThongKe)cboKieu.SelectedIndex;
            List<DoanhThuDTO> data = null;
            if (!UiHelper.Try(this, () => data = bll.DoanhThuTheoKy(dtFrom1.Value, dtTo1.Value, kieu))) return;

            UiHelper.Bind(grid1, data,
                "Ky|Kỳ||100", "SoDon|Số đơn hoàn tất|N0|70", "DoanhThuGop|Doanh thu gộp|N0|90",
                "TienGiam|Giảm giá|N0|80", "DoanhThu|Doanh thu thực thu|N0|100");

            Series s = chart.Series[0];
            s.Points.Clear();
            foreach (DoanhThuDTO d in data) s.Points.AddXY(d.Ky, (double)d.DoanhThu);
            ChartArea area = chart.ChartAreas[0];
            area.AxisX.Interval = data.Count <= 31 ? 1 : 0;
            area.AxisX.LabelStyle.Angle = data.Count > 8 ? -45 : 0;
            chart.Titles[0].Text = "Biểu đồ doanh thu " + TenKieu[(int)kieu].ToLower() + " (" +
                                   dtFrom1.Value.ToString("dd/MM/yyyy") + " - " + dtTo1.Value.ToString("dd/MM/yyyy") + ")";

            decimal tong = data.Sum(x => x.DoanhThu);
            lblTong.Text = tong.ToString("N0") + " đ";
            lblSoDon.Text = data.Sum(x => x.SoDon).ToString("N0");
            lblGiam.Text = data.Sum(x => x.TienGiam).ToString("N0") + " đ";
            subtitle1 = TenKieu[(int)kieu] + ", từ " + dtFrom1.Value.ToString("dd/MM/yyyy") + " đến " + dtTo1.Value.ToString("dd/MM/yyyy") +
                        ". Tổng thực thu: " + tong.ToString("N0") + " đ; số đơn: " + lblSoDon.Text;
            if (tong == 0) UiHelper.Info(this, "Không có doanh thu phát sinh trong khoảng thời gian đã chọn.");
        }

        // ------------------------------------------------------------------ báo cáo chi tiết
        private void XemBaoCao()
        {
            var tieuChi = (TieuChiBaoCao)cboTieuChi.SelectedIndex;
            List<BaoCaoDoanhThuDTO> data = null;
            if (!UiHelper.Try(this, () => data = bll.BaoCao(dtFrom2.Value, dtTo2.Value, tieuChi))) return;

            string tenNhom;
            switch (tieuChi)
            {
                case TieuChiBaoCao.SanPham: tenNhom = "Sản phẩm"; break;
                case TieuChiBaoCao.DanhMuc: tenNhom = "Danh mục"; break;
                case TieuChiBaoCao.KhachHang: tenNhom = "Khách hàng"; break;
                default: tenNhom = "Phương thức thanh toán"; break;
            }
            UiHelper.Bind(grid2, data,
                "Nhom|" + tenNhom + "||200", "SoDon|Số đơn hoàn thành|N0|70", "SoLuongBan|Số lượng bán|N0|70",
                "DoanhThuGop|Doanh thu gộp|N0|90", "ChietKhau|Chiết khấu|N0|80", "ThucThu|Doanh thu thực tế|N0|100");

            decimal thucThu = data.Sum(x => x.ThucThu);
            lblSummary2.Text = "Tổng doanh thu thực tế: " + thucThu.ToString("N0") + " đ   |   Chiết khấu: " +
                               data.Sum(x => x.ChietKhau).ToString("N0") + " đ   |   Số lượng bán: " + data.Sum(x => x.SoLuongBan).ToString("N0");
            subtitle2 = cboTieuChi.Text + ", từ " + dtFrom2.Value.ToString("dd/MM/yyyy") + " đến " + dtTo2.Value.ToString("dd/MM/yyyy") +
                        ". Tổng thực thu: " + thucThu.ToString("N0") + " đ";
            if (data.Count == 0) UiHelper.Info(this, "Không có dữ liệu doanh thu trong khoảng thời gian đã chọn.");
        }

        // ------------------------------------------------------------------ xuất file
        private void Export(DataGridView grid, string title, string subtitle, string format)
        {
            if (grid.Rows.Count == 0)
            {
                UiHelper.Warn(this, "Báo cáo trống, không thể xuất file. Hãy bấm Xem trước.");
                return;
            }
            string ext = format == "excel" ? "xlsx" : format;
            string filter = format == "excel" ? "Excel (*.xlsx)|*.xlsx" : (format == "pdf" ? "PDF (*.pdf)|*.pdf" : "CSV (*.csv)|*.csv");
            using (var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = "BaoCaoDoanhThu_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + "." + ext,
                Title = "Lưu báo cáo"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string path = dlg.FileName;
                Cursor = Cursors.WaitCursor;
                bool ok = UiHelper.Try(this, () =>
                {
                    if (format == "excel") ReportExporter.ExportExcel(path, title, subtitle, grid);
                    else if (format == "pdf") ReportExporter.ExportPdf(path, title, subtitle, grid);
                    else ReportExporter.ExportCsv(path, title, subtitle, grid);
                });
                Cursor = Cursors.Default;
                if (!ok) return;
                if (UiHelper.Confirm(this, "Đã xuất báo cáo:\n" + path + "\n\nMở file ngay?"))
                {
                    try
                    {
                        if (File.Exists(path)) System.Diagnostics.Process.Start(path);
                    }
                    catch (Exception ex)
                    {
                        UiHelper.Warn(this, "Không mở được file: " + ex.Message);
                    }
                }
            }
        }
    }
}
