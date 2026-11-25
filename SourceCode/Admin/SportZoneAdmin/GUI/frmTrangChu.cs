using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// Trang tổng quan: lời chào, 4 thẻ số liệu nhanh (bấm để mở chức năng), biểu đồ doanh thu 6 tháng gần nhất,
    /// danh sách sản phẩm sắp hết hàng và đơn hàng chờ xác nhận.
    /// </summary>
    public class frmTrangChu : Form
    {
        private readonly frmMain main;
        private readonly Label lblXinChao;
        private readonly Label lblMoTa;
        private readonly Label valDon;
        private readonly Label valSapHet;
        private readonly Label valDoanhThu;
        private readonly Label valKhach;
        private readonly Chart chart;
        private readonly DataGridView gridDon;
        private readonly DataGridView gridKho;

        public frmTrangChu(frmMain main)
        {
            this.main = main;
            UiHelper.SetupChild(this, "Trang chủ");
            BackColor = UiHelper.PageBack;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = UiHelper.PageBack };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));

            // ----- lời chào
            var greet = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.PageBack, Margin = new Padding(0) };
            lblXinChao = new Label { Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = UiHelper.TextColor };
            lblMoTa = new Label { Dock = DockStyle.Top, Height = 22, ForeColor = UiHelper.Muted };
            greet.Controls.Add(lblMoTa);
            greet.Controls.Add(lblXinChao);

            // ----- 4 thẻ số liệu
            var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = UiHelper.PageBack, Margin = new Padding(0, 0, 0, 14) };
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards.Controls.Add(StatCard("Đơn chờ xác nhận", Glyph.Cart, UiHelper.Orange, out valDon, "Xem đơn hàng  ›", "Quản lý đơn hàng"), 0, 0);
            cards.Controls.Add(StatCard("Sản phẩm sắp hết hàng", Glyph.Warning, UiHelper.Danger, out valSapHet, "Xem tồn kho  ›", "Quản lý tồn kho"), 1, 0);
            cards.Controls.Add(StatCard("Doanh thu tháng " + DateTime.Now.ToString("MM/yyyy"), Glyph.Money, UiHelper.Success, out valDoanhThu, "Xem thống kê  ›", "Thống kê doanh thu"), 2, 0);
            cards.Controls.Add(StatCard("Khách hàng hoạt động", Glyph.People, UiHelper.Blue, out valKhach, "Xem khách hàng  ›", "Tài khoản khách hàng"), 3, 0);

            // ----- biểu đồ + sắp hết hàng
            var middle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiHelper.PageBack, Margin = new Padding(0, 0, 0, 14) };
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            chart = MakeChart();
            gridKho = UiHelper.MakeGrid();
            CardPanel cardChart = UiHelper.MakeCard("Doanh thu 6 tháng gần nhất", chart, Glyph.Chart);
            cardChart.Margin = new Padding(0, 0, 14, 0);
            CardPanel cardKho = UiHelper.MakeCard("Sản phẩm sắp hết hàng  (nhấp đúp để nhập kho)", gridKho, Glyph.Box);
            cardKho.Margin = new Padding(0);
            middle.Controls.Add(cardChart, 0, 0);
            middle.Controls.Add(cardKho, 1, 0);

            // ----- đơn chờ xác nhận
            gridDon = UiHelper.MakeGrid();
            CardPanel cardDon = UiHelper.MakeCard("Đơn hàng chờ xác nhận  (nhấp đúp để mở Quản lý đơn hàng)", gridDon, Glyph.Cart);
            cardDon.Margin = new Padding(0);

            gridDon.CellDoubleClick += (s, e) => main.OpenMenu("Quản lý đơn hàng");
            gridKho.CellDoubleClick += (s, e) => main.OpenMenu("Nhập kho");
            gridKho.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var sp = gridKho.Rows[e.RowIndex].DataBoundItem as SanPhamDTO;
                if (sp != null && sp.SoLuongTon == 0 && gridKho.Columns[e.ColumnIndex].DataPropertyName == "SoLuongTon")
                    e.CellStyle.ForeColor = UiHelper.Danger;
            };

            layout.Controls.Add(greet, 0, 0);
            layout.Controls.Add(cards, 0, 1);
            layout.Controls.Add(middle, 0, 2);
            layout.Controls.Add(cardDon, 0, 3);
            Controls.Add(layout);
        }

        private CardPanel StatCard(string title, string glyph, Color color, out Label value, string footer, string menu)
        {
            CardPanel card = UiHelper.MakeStatCard(title, glyph, color, out value, footer);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, menu == "Tài khoản khách hàng" ? 0 : 14, 0);
            UiHelper.ClickAll(card, (s, e) => main.OpenMenu(menu));
            return card;
        }

        private static Chart MakeChart()
        {
            var c = new Chart { BackColor = Color.White, Margin = new Padding(0) };
            var area = new ChartArea("main") { BackColor = Color.White };
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LineColor = UiHelper.Line;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.LabelStyle.Font = UiHelper.SmallFont;
            area.AxisX.LabelStyle.ForeColor = UiHelper.Muted;
            area.AxisX.Interval = 1;
            area.AxisY.MajorGrid.LineColor = UiHelper.Line;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LabelStyle.Font = UiHelper.SmallFont;
            area.AxisY.LabelStyle.ForeColor = UiHelper.Muted;
            area.AxisY.LabelStyle.Format = "#,##0,,.#tr";
            c.ChartAreas.Add(area);
            var s = new Series("Doanh thu")
            {
                ChartType = SeriesChartType.Column,
                Color = UiHelper.Primary,
                ToolTip = "#VALX: #VALY{#,##0} đ",
                IsValueShownAsLabel = false
            };
            s["PointWidth"] = "0.55";
            c.Series.Add(s);
            return c;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string ten = Session.CurrentAdmin != null ? Session.CurrentAdmin.HoTen : "";
            int h = DateTime.Now.Hour;
            string buoi = h < 11 ? "Chào buổi sáng" : h < 14 ? "Chào buổi trưa" : h < 18 ? "Chào buổi chiều" : "Chào buổi tối";
            lblXinChao.Text = buoi + ", " + ten;
            lblMoTa.Text = "Tình hình cửa hàng SportZone hôm nay, " + DateTime.Now.ToString("dd/MM/yyyy") + ".";

            UiHelper.Try(this, () =>
            {
                TongQuanDTO tq = new ThongKeBLL().TongQuan();
                valDon.Text = tq.DonChoXacNhan.ToString("N0");
                valSapHet.Text = tq.SanPhamSapHet.ToString("N0");
                valDoanhThu.Text = tq.DoanhThuThangNay.ToString("N0") + " đ";
                valKhach.Text = tq.SoKhachHang.ToString("N0");

                DateTime dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                List<DoanhThuDTO> ds = new ThongKeBLL().DoanhThuTheoKy(dauThang.AddMonths(-5), DateTime.Today, KieuThongKe.Thang);
                Series s = chart.Series[0];
                s.Points.Clear();
                foreach (DoanhThuDTO d in ds)
                    s.Points.AddXY(d.TuNgay.ToString("MM/yyyy"), d.DoanhThu);
                if (ds.Count > 0) s.Points[s.Points.Count - 1].Color = UiHelper.Accent;   // tháng hiện tại

                UiHelper.Bind(gridDon, new DonHangBLL().GetList("", TrangThaiDonHang.ChoXacNhan, null, null),
                    "MaDH|Mã đơn||50", "NgayDat|Ngày đặt|dd/MM/yyyy HH:mm|110", "TenKH|Khách hàng||150",
                    "TenPTTT|Thanh toán||120", "TongTien|Tổng tiền|N0|90", "TrangThai|Trạng thái||100");
                UiHelper.Bind(gridKho, new KhoBLL().GetTonKho("", true),
                    "TenSP|Sản phẩm||170", "SoLuongTon|Tồn|N0|50", "MucTonToiThieu|Tối thiểu|N0|60", "TinhTrangKho|Tình trạng||90");
            });
        }
    }
}
