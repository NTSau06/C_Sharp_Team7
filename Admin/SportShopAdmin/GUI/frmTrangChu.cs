using System;
using System.Drawing;
using System.Windows.Forms;
using SportShopAdmin.BLL;
using SportShopAdmin.DTO;

namespace SportShopAdmin.GUI
{
    /// <summary>Trang tổng quan: số liệu nhanh, đơn chờ xác nhận, sản phẩm sắp hết hàng</summary>
    public class frmTrangChu : Form
    {
        private readonly frmMain main;
        private readonly FlowLayoutPanel cards;
        private readonly DataGridView gridDon;
        private readonly DataGridView gridKho;

        public frmTrangChu(frmMain main)
        {
            this.main = main;
            UiHelper.SetupChild(this, "Trang chủ");
            BackColor = UiHelper.PageBack;

            cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(0, 4, 0, 4), BackColor = UiHelper.PageBack };

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiHelper.PageBack };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            gridDon = UiHelper.MakeGrid();
            gridKho = UiHelper.MakeGrid();
            gridKho.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var sp = gridKho.Rows[e.RowIndex].DataBoundItem as SanPhamDTO;
                if (sp != null && sp.SoLuongTon == 0) e.CellStyle.ForeColor = UiHelper.Danger;
            };
            table.Controls.Add(Box("Đơn hàng chờ xác nhận (nhấp đúp để mở Quản lý đơn hàng)", gridDon), 0, 0);
            table.Controls.Add(Box("Sản phẩm sắp hết hàng (tồn ≤ mức tối thiểu)", gridKho), 1, 0);
            gridDon.CellDoubleClick += (s, e) => main.OpenMenu(4);
            gridKho.CellDoubleClick += (s, e) => main.OpenMenu(3);

            Controls.Add(table);
            Controls.Add(cards);
            table.BringToFront();
        }

        private static Control Box(string title, DataGridView grid)
        {
            var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Font = UiHelper.BoldFont, ForeColor = UiHelper.Primary, BackColor = Color.White, Padding = new Padding(8) };
            grid.Font = UiHelper.BaseFont;
            group.Controls.Add(grid);
            return group;
        }

        private void AddCard(string title, string value, Color color, int menuIndex)
        {
            var p = new Panel { Width = 270, Height = 112, BackColor = color, Margin = new Padding(0, 0, 14, 0), Cursor = Cursors.Hand };
            var lblValue = new Label { Text = value, Dock = DockStyle.Fill, ForeColor = Color.White, Font = UiHelper.BigNumberFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
            var lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 38, ForeColor = Color.White, Font = UiHelper.BoldFont, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(14, 0, 0, 0) };
            p.Controls.Add(lblValue);
            p.Controls.Add(lblTitle);
            EventHandler open = (s, e) => main.OpenMenu(menuIndex);
            p.Click += open;
            lblValue.Click += open;
            lblTitle.Click += open;
            cards.Controls.Add(p);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UiHelper.Try(this, () =>
            {
                TongQuanDTO tq = new ThongKeBLL().TongQuan();
                AddCard("Đơn chờ xác nhận", tq.DonChoXacNhan.ToString("N0"), UiHelper.Orange, 4);
                AddCard("Sản phẩm sắp hết hàng", tq.SanPhamSapHet.ToString("N0"), UiHelper.Danger, 3);
                AddCard("Doanh thu tháng " + DateTime.Now.ToString("MM/yyyy"), tq.DoanhThuThangNay.ToString("N0") + " đ", UiHelper.Primary, 7);
                AddCard("Khách hàng hoạt động", tq.SoKhachHang.ToString("N0"), UiHelper.Blue, 5);

                UiHelper.Bind(gridDon, new DonHangBLL().GetList("", TrangThaiDonHang.ChoXacNhan, null, null),
                    "MaDH|Mã đơn||50", "NgayDat|Ngày đặt|dd/MM/yyyy HH:mm|110", "TenKH|Khách hàng||130", "TongTien|Tổng tiền|N0|90");
                UiHelper.Bind(gridKho, new KhoBLL().GetTonKho("", true),
                    "TenSP|Sản phẩm||170", "SoLuongTon|Tồn|N0|50", "MucTonToiThieu|Tối thiểu|N0|60");
            });
        }
    }
}
