using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SportShopAdmin.BLL;
using SportShopAdmin.DTO;
using SportShopAdmin.Utils;

namespace SportShopAdmin.GUI
{
    /// <summary>
    /// UC 1.2.4 - Quản lý đơn hàng: xem/lọc danh sách, xem chi tiết, xác nhận (trừ tồn kho),
    /// hủy đơn (hoàn lượt mã giảm giá), cập nhật trạng thái giao hàng (hoàn kho khi giao thất bại)
    /// </summary>
    public class frmDonHang : Form
    {
        private readonly DonHangBLL bll = new DonHangBLL();
        private readonly DataGridView grid;
        private readonly TextBox txtSearch;
        private readonly ComboBox cboTrangThai;
        private readonly DateTimePicker dtFrom;
        private readonly DateTimePicker dtTo;
        private readonly Label lblCount;
        private readonly Button btnXacNhan;
        private readonly Button btnHuy;
        private readonly Button btnGiaoHang;

        public frmDonHang()
        {
            UiHelper.SetupChild(this, "Quản lý đơn hàng");

            FlowLayoutPanel filter = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(filter, "Mã đơn / tên KH / SĐT:", new TextBox { Width = 200 });
            cboTrangThai = UiHelper.AddTool(filter, "Trạng thái:", UiHelper.MakeCombo(150));
            cboTrangThai.Items.Add("Tất cả");
            cboTrangThai.Items.AddRange(TrangThaiDonHang.TatCa.Cast<object>().ToArray());
            cboTrangThai.SelectedIndex = 0;
            dtFrom = UiHelper.AddTool(filter, "Từ ngày:", UiHelper.MakeDate(DateTime.Today.AddMonths(-1)));
            dtFrom.ShowCheckBox = true;
            dtFrom.Checked = false;
            dtTo = UiHelper.AddTool(filter, "Đến:", UiHelper.MakeDate(DateTime.Today));
            dtTo.ShowCheckBox = true;
            dtTo.Checked = false;
            Button btnLoc = UiHelper.AddTool(filter, null, UiHelper.MakeButton("Lọc", UiHelper.Primary, 80));
            btnLoc.Click += (s, e) => LoadData(true);
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadData(true); } };
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData(false);

            FlowLayoutPanel actions = UiHelper.MakeToolbar();
            Button btnChiTiet = UiHelper.AddTool(actions, null, UiHelper.MakeButton("Xem chi tiết", UiHelper.Blue, 125));
            btnXacNhan = UiHelper.AddTool(actions, null, UiHelper.MakeButton("Xác nhận đơn", UiHelper.Primary, 130));
            btnHuy = UiHelper.AddTool(actions, null, UiHelper.MakeButton("Hủy / Từ chối", UiHelper.Danger, 130));
            btnGiaoHang = UiHelper.AddTool(actions, null, UiHelper.MakeButton("Cập nhật giao hàng", UiHelper.Purple, 170));
            Button btnRefresh = UiHelper.AddTool(actions, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            lblCount = UiHelper.AddTool(actions, null, new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(12, 10, 3, 3) });
            btnChiTiet.Click += (s, e) => XemChiTiet();
            btnXacNhan.Click += BtnXacNhan_Click;
            btnHuy.Click += BtnHuy_Click;
            btnGiaoHang.Click += BtnGiaoHang_Click;
            btnRefresh.Click += (s, e) => LoadData(false);

            grid = UiHelper.MakeGrid();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) XemChiTiet(); };
            grid.SelectionChanged += (s, e) => UpdateButtons();
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "TrangThai") return;
                var dh = grid.Rows[e.RowIndex].DataBoundItem as DonHangDTO;
                if (dh == null) return;
                e.CellStyle.ForeColor = UiHelper.StatusColor(dh.TrangThai);
                e.CellStyle.Font = UiHelper.BoldFont;
            };

            Controls.Add(grid);
            Controls.Add(actions);
            Controls.Add(filter);
            grid.BringToFront();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadData(false);
        }

        private void LoadData(bool showNotFound)
        {
            string tt = cboTrangThai.SelectedIndex <= 0 ? "" : cboTrangThai.SelectedItem.ToString();
            DateTime? from = dtFrom.Checked ? dtFrom.Value : (DateTime?)null;
            DateTime? to = dtTo.Checked ? dtTo.Value : (DateTime?)null;
            int? keepId = SelectedId();
            List<DonHangDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetList(txtSearch.Text, tt, from, to);
                UiHelper.Bind(grid, list,
                    "MaDH|Mã đơn||50", "NgayDat|Ngày đặt|dd/MM/yyyy HH:mm|95", "TenKH|Khách hàng||120", "SDT|SĐT||80",
                    "TamTinh|Tạm tính|N0|75", "TienGiam|Giảm giá|N0|65", "TongTien|Tổng tiền|N0|75",
                    "TenPTTT|Thanh toán||120", "TrangThai|Trạng thái||85", "TenQTV|Người xử lý||100");
            });
            if (list == null) return;
            lblCount.Text = list.Count + " đơn hàng | Tổng giá trị: " + list.Sum(x => x.TongTien).ToString("N0") + " đ";
            if (list.Count == 0 && showNotFound) UiHelper.Info(this, "Không tìm thấy dữ liệu phù hợp.");
            if (keepId.HasValue) SelectRow(keepId.Value);
            UpdateButtons();
        }

        private int? SelectedId()
        {
            DonHangDTO dh = UiHelper.Selected<DonHangDTO>(grid);
            return dh == null ? (int?)null : dh.MaDH;
        }

        private void SelectRow(int id)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                var dh = r.DataBoundItem as DonHangDTO;
                if (dh != null && dh.MaDH == id)
                {
                    r.Selected = true;
                    grid.CurrentCell = r.Cells[grid.Columns["MaDH"].Index];
                    return;
                }
            }
        }

        private void UpdateButtons()
        {
            DonHangDTO dh = UiHelper.Selected<DonHangDTO>(grid);
            bool cho = dh != null && TrangThaiDonHang.LaChoXuLy(dh.TrangThai);
            btnXacNhan.Enabled = cho;
            btnHuy.Enabled = cho;
            btnGiaoHang.Enabled = dh != null && DonHangBLL.TrangThaiTiepTheo(dh.TrangThai).Length > 0;
        }

        private DonHangDTO RequireSelected()
        {
            DonHangDTO dh = UiHelper.Selected<DonHangDTO>(grid);
            if (dh == null) UiHelper.Warn(this, "Hãy chọn một đơn hàng trong danh sách.");
            return dh;
        }

        private void XemChiTiet()
        {
            DonHangDTO dh = RequireSelected();
            if (dh == null) return;
            using (var f = new frmChiTietDonHang(dh.MaDH)) f.ShowDialog(this);
        }

        private void BtnXacNhan_Click(object sender, EventArgs e)
        {
            DonHangDTO dh = RequireSelected();
            if (dh == null) return;
            if (!UiHelper.Confirm(this, "Xác nhận đơn hàng #" + dh.MaDH + " của " + dh.TenKH + "?\n" +
                                        "Hệ thống sẽ tự động trừ số lượng tồn kho tương ứng.")) return;
            if (!UiHelper.Try(this, () => bll.XacNhan(dh.MaDH, Session.MaQTV)))
            {
                LoadData(false);
                return;
            }
            UiHelper.Info(this, "Đã xác nhận đơn hàng #" + dh.MaDH + " và cập nhật tồn kho.");
            LoadData(false);
        }

        private void BtnHuy_Click(object sender, EventArgs e)
        {
            DonHangDTO dh = RequireSelected();
            if (dh == null) return;
            string lyDo = InputDialog.Show(this, "Hủy đơn hàng #" + dh.MaDH, "Nhập lý do hủy / từ chối đơn hàng:", "Sản phẩm trong kho không đủ số lượng", true);
            if (lyDo == null) return;
            if (!UiHelper.Try(this, () => bll.Huy(dh.MaDH, lyDo, Session.MaQTV))) return;
            UiHelper.Info(this, "Đã hủy đơn hàng #" + dh.MaDH + ".");
            LoadData(false);
        }

        private void BtnGiaoHang_Click(object sender, EventArgs e)
        {
            DonHangDTO dh = RequireSelected();
            if (dh == null) return;
            using (var f = new frmCapNhatGiaoHang(dh))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadData(false);
            }
        }
    }

    /// <summary>UC 1.2.4.2 - Xem chi tiết đơn hàng</summary>
    public class frmChiTietDonHang : Form
    {
        public frmChiTietDonHang(int maDH)
        {
            Text = "Chi tiết đơn hàng #" + maDH;
            Font = UiHelper.BaseFont;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 640);
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;

            var bll = new DonHangBLL();
            DonHangDTO dh = null;
            List<ChiTietDonHangDTO> items = null;
            if (!UiHelper.Try(null, () => { dh = bll.GetById(maDH); items = bll.GetChiTiet(maDH); }))
            {
                Load += (s, e) => Close();
                return;
            }

            var info = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, Padding = new Padding(12) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            AddInfo(info, 0, 0, "Mã đơn:", "#" + dh.MaDH);
            AddInfo(info, 0, 2, "Ngày đặt:", dh.NgayDat.ToString("dd/MM/yyyy HH:mm"));
            AddInfo(info, 1, 0, "Khách hàng:", dh.TenKH);
            AddInfo(info, 1, 2, "Số điện thoại:", dh.SDT);
            AddInfo(info, 2, 0, "Email:", dh.Email);
            AddInfo(info, 2, 2, "Thanh toán:", dh.TenPTTT);
            AddInfo(info, 3, 0, "Địa chỉ giao:", dh.DiaChiGiao);
            Label lblTT = AddInfo(info, 3, 2, "Trạng thái:", dh.TrangThai);
            lblTT.ForeColor = UiHelper.StatusColor(dh.TrangThai);
            lblTT.Font = UiHelper.BoldFont;
            AddInfo(info, 4, 0, "Người xử lý:", string.IsNullOrEmpty(dh.TenQTV) ? "(chưa xử lý)" : dh.TenQTV);
            AddInfo(info, 4, 2, "Mã giảm giá:", string.IsNullOrEmpty(dh.CodeMGG) ? "(không)" : dh.CodeMGG);
            AddInfo(info, 5, 0, "Ghi chú:", dh.GhiChu);

            DataGridView grid = UiHelper.MakeGrid();
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var ct = grid.Rows[e.RowIndex].DataBoundItem as ChiTietDonHangDTO;
                if (ct != null && !ct.DuHang && TrangThaiDonHang.LaChoXuLy(dh.TrangThai))
                {
                    e.CellStyle.BackColor = UiHelper.WarningBack;
                    e.CellStyle.ForeColor = Color.DarkRed;
                }
            };
            UiHelper.Bind(grid, items,
                "MaSP|Mã SP||45", "TenSP|Sản phẩm||220", "SoLuong|Số lượng|N0|60", "DonGia|Đơn giá|N0|80",
                "ThanhTien|Thành tiền|N0|90", "SoLuongTon|Tồn kho hiện tại|N0|80");

            var totals = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 16, 0),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Tạm tính: " + dh.TamTinh.ToString("N0") + " đ\n" +
                       "Giảm giá: -" + dh.TienGiam.ToString("N0") + " đ\n" +
                       "TỔNG THANH TOÁN: " + dh.TongTien.ToString("N0") + " đ"
            };
            var note = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                ForeColor = Color.DarkRed,
                Padding = new Padding(12, 0, 0, 0),
                Visible = TrangThaiDonHang.LaChoXuLy(dh.TrangThai) && items.Any(x => !x.DuHang),
                Text = "Dòng tô đỏ: sản phẩm không đủ tồn kho để xác nhận đơn."
            };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            Button btnClose = UiHelper.MakeButton("Đóng", UiHelper.Gray, 100);
            btnClose.Click += (s, e) => Close();
            buttons.Controls.Add(btnClose);
            CancelButton = btnClose;

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
            gridPanel.Controls.Add(grid);

            Controls.Add(gridPanel);
            Controls.Add(note);
            Controls.Add(totals);
            Controls.Add(buttons);
            Controls.Add(info);
            gridPanel.BringToFront();
        }

        private static Label AddInfo(TableLayoutPanel t, int row, int col, string label, string value)
        {
            while (t.RowCount <= row)
            {
                t.RowCount++;
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            t.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 5, 3, 5) }, col, row);
            var v = new Label { Text = value ?? "", AutoSize = true, MaximumSize = new Size(300, 0), Margin = new Padding(3, 5, 3, 5) };
            t.Controls.Add(v, col + 1, row);
            return v;
        }
    }

    /// <summary>UC 1.2.4.4 - Cập nhật trạng thái giao hàng</summary>
    public class frmCapNhatGiaoHang : Form
    {
        public frmCapNhatGiaoHang(DonHangDTO dh)
        {
            Text = "Cập nhật giao hàng - Đơn #" + dh.MaDH;
            Font = UiHelper.BaseFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 300);

            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            UiHelper.AddField(table, "Trạng thái hiện tại:", new Label { Text = dh.TrangThai, AutoSize = true, Font = UiHelper.BoldFont, ForeColor = UiHelper.StatusColor(dh.TrangThai) });
            ComboBox cbo = UiHelper.AddField(table, "Trạng thái mới:", UiHelper.MakeCombo());
            cbo.Items.AddRange(DonHangBLL.TrangThaiTiepTheo(dh.TrangThai).Cast<object>().ToArray());
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
            TextBox txtLyDo = UiHelper.AddField(table, "Lý do / ghi chú:", UiHelper.MakeMultiline(80));
            Label hint = UiHelper.AddField(table, "", new Label { AutoSize = true, ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic) });
            EventHandler updateHint = (s, e) =>
            {
                bool thatBai = (cbo.SelectedItem as string) == TrangThaiDonHang.GiaoThatBai;
                hint.Text = thatBai ? "Bắt buộc nhập lý do. Số lượng hàng sẽ được hoàn lại kho." : "";
            };
            cbo.SelectedIndexChanged += updateHint;
            updateHint(null, EventArgs.Empty);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            Button btnHuy = UiHelper.MakeButton("Hủy", UiHelper.Gray, 90);
            Button btnLuu = UiHelper.MakeButton("Lưu cập nhật", UiHelper.Primary, 130);
            buttons.Controls.Add(btnHuy);
            buttons.Controls.Add(btnLuu);
            Controls.Add(table);
            Controls.Add(buttons);
            CancelButton = btnHuy;

            btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnLuu.Click += (s, e) =>
            {
                if (cbo.SelectedItem == null) { UiHelper.Warn(this, "Hãy chọn trạng thái mới."); return; }
                string moi = cbo.SelectedItem.ToString();
                if (!UiHelper.Try(this, () => new DonHangBLL().CapNhatGiaoHang(dh.MaDH, moi, txtLyDo.Text, Session.MaQTV))) return;
                UiHelper.Info(this, "Đã cập nhật đơn #" + dh.MaDH + " sang \"" + moi + "\".");
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
