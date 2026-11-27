using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// UC 1.2.12 - Quản lý tồn kho: xem tồn hiện tại + cảnh báo sắp hết, lập phiếu nhập,
    /// xem phiếu nhập và theo dõi lịch sử nhập / xuất
    /// </summary>
    public class frmTonKho : Form
    {
        private readonly KhoBLL bll = new KhoBLL();

        // tab tồn kho
        private readonly DataGridView gridTon;
        private readonly TextBox txtSearch;
        private readonly CheckBox chkSapHet;
        private readonly Label lblTonSummary;
        // tab phiếu nhập
        private readonly DataGridView gridPN;
        private readonly DataGridView gridCTPN;
        private readonly DateTimePicker pnFrom;
        private readonly DateTimePicker pnTo;
        private readonly Label lblPnSummary;
        private readonly Label lblCtTitle;
        private readonly SplitContainer splitPN;
        // tab lịch sử
        private readonly DataGridView gridLS;
        private readonly DateTimePicker lsFrom;
        private readonly DateTimePicker lsTo;
        private readonly TextBox txtLsSP;
        private readonly Label lblLsSummary;

        private readonly bool moNhapKho;
        private readonly TabPage tabPhieuNhap;
        private readonly TabControl tabs;

        public frmTonKho() : this(false) { }

        /// <param name="moNhapKho">true: chuyển sang tab Phiếu nhập và mở form lập phiếu (frmNhapKho)</param>
        public frmTonKho(bool moNhapKho)
        {
            this.moNhapKho = moNhapKho;
            UiHelper.SetupChild(this, moNhapKho ? "Nhập kho" : "Quản lý tồn kho");
            tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };

            // ================= TAB 1: TỒN KHO HIỆN TẠI
            var tabTon = new TabPage("Tồn kho hiện tại") { BackColor = Color.White };
            FlowLayoutPanel tb1 = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(tb1, "Tìm sản phẩm:", new TextBox { Width = 220 });
            chkSapHet = UiHelper.AddTool(tb1, null, new CheckBox { Text = "Chỉ hiện sản phẩm sắp hết hàng", AutoSize = true });
            Button btnNhap = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("+ Lập phiếu nhập kho", UiHelper.Primary, 195));
            Button btnRefresh = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            UiHelper.OnTextChangedDebounced(txtSearch, LoadTonKho);
            chkSapHet.CheckedChanged += (s, e) => LoadTonKho();
            btnNhap.Click += (s, e) => LapPhieuNhap();
            btnRefresh.Click += (s, e) => LoadTonKho();

            lblTonSummary = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            gridTon = UiHelper.MakeGrid();
            gridTon.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var sp = gridTon.Rows[e.RowIndex].DataBoundItem as SanPhamDTO;
                if (sp != null && sp.SapHetHang())
                {
                    e.CellStyle.BackColor = UiHelper.WarningBack;
                    e.CellStyle.ForeColor = Color.DarkRed;
                }
            };
            gridTon.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) LapPhieuNhap(); };
            tabTon.Controls.Add(gridTon);
            tabTon.Controls.Add(lblTonSummary);
            tabTon.Controls.Add(tb1);
            gridTon.BringToFront();

            // ================= TAB 2: PHIẾU NHẬP
            var tabPN = new TabPage("Phiếu nhập kho") { BackColor = UiHelper.PageBack };
            FlowLayoutPanel tb2 = UiHelper.MakeToolbar();
            pnFrom = UiHelper.AddTool(tb2, "Từ ngày:", UiHelper.MakeDate(DateTime.Today.AddMonths(-12)));
            pnTo = UiHelper.AddTool(tb2, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            Button btnXemPN = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Xem", UiHelper.Primary, 86));
            Button btnLapPN2 = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("+ Lập phiếu nhập kho", UiHelper.Success, 195));
            Button btnLamMoiPN = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            btnXemPN.Click += (s, e) => LoadPhieuNhap();
            btnLapPN2.Click += (s, e) => LapPhieuNhap(0);
            btnLamMoiPN.Click += (s, e) => LoadPhieuNhap();

            lblPnSummary = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.White,
                ForeColor = UiHelper.Muted,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            var pnWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = UiHelper.PageBack };
            splitPN = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 10,
                BackColor = UiHelper.PageBack
            };
            gridPN = UiHelper.MakeGrid();
            gridCTPN = UiHelper.MakeGrid();
            gridPN.SelectionChanged += (s, e) => LoadChiTietPhieuNhap();

            CardPanel cardPN = UiHelper.MakeCard("Danh sách phiếu nhập kho", gridPN, Glyph.Document);
            splitPN.Panel1.BackColor = UiHelper.PageBack;
            splitPN.Panel1.Controls.Add(cardPN);

            var cardCT = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 14) };
            lblCtTitle = UiHelper.MakeCardHeader("Chi tiết phiếu nhập", Glyph.Box);
            var gapCT = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.White };
            gridCTPN.Dock = DockStyle.Fill;
            cardCT.Controls.Add(gridCTPN);
            cardCT.Controls.Add(gapCT);
            cardCT.Controls.Add(lblCtTitle);
            splitPN.Panel2.BackColor = UiHelper.PageBack;
            splitPN.Panel2.Controls.Add(cardCT);

            pnWrap.Controls.Add(splitPN);
            tabPN.Controls.Add(pnWrap);
            tabPN.Controls.Add(lblPnSummary);
            tabPN.Controls.Add(tb2);
            pnWrap.BringToFront();

            // ================= TAB 3: LỊCH SỬ NHẬP / XUẤT
            var tabLS = new TabPage("Lịch sử nhập / xuất") { BackColor = Color.White };
            FlowLayoutPanel tb3 = UiHelper.MakeToolbar();
            lsFrom = UiHelper.AddTool(tb3, "Từ ngày:", UiHelper.MakeDate(DateTime.Today.AddMonths(-3)));
            lsTo = UiHelper.AddTool(tb3, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            txtLsSP = UiHelper.AddTool(tb3, "Sản phẩm:", new TextBox { Width = 200 });
            Button btnXemLS = UiHelper.AddTool(tb3, null, UiHelper.MakeButton("Xem", UiHelper.Primary, 80));
            btnXemLS.Click += (s, e) => LoadLichSu();
            txtLsSP.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadLichSu(); } };
            lblLsSummary = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            gridLS = UiHelper.MakeGrid();
            gridLS.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || gridLS.Columns[e.ColumnIndex].Name != "Loai") return;
                var ls = gridLS.Rows[e.RowIndex].DataBoundItem as LichSuKhoDTO;
                if (ls != null) e.CellStyle.ForeColor = ls.Loai == "Nhập kho" ? UiHelper.Primary : UiHelper.Blue;
            };
            tabLS.Controls.Add(gridLS);
            tabLS.Controls.Add(lblLsSummary);
            tabLS.Controls.Add(tb3);
            gridLS.BringToFront();

            tabs.TabPages.Add(tabTon);
            tabs.TabPages.Add(tabPN);
            tabPhieuNhap = tabPN;
            tabs.TabPages.Add(tabLS);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == tabPN)
                {
                    SetSplitRatio();
                    if (gridPN.DataSource == null) LoadPhieuNhap();
                }
                if (tabs.SelectedTab == tabLS && gridLS.DataSource == null) LoadLichSu();
            };
            UiHelper.StyleTabs(tabs);
            Controls.Add(tabs);
        }

        private void SetSplitRatio()
        {
            if (splitPN.Height > 220)
            {
                int target = (int)(splitPN.Height * 0.52);
                if (target > splitPN.Panel1MinSize && target < splitPN.Height - splitPN.Panel2MinSize)
                    splitPN.SplitterDistance = target;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadTonKho();
            if (moNhapKho)
            {
                tabs.SelectedTab = tabPhieuNhap;
                BeginInvoke((Action)(() => LapPhieuNhap(0)));
            }
        }

        private void LoadTonKho()
        {
            List<SanPhamDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetTonKho(txtSearch.Text, chkSapHet.Checked);
                UiHelper.Bind(gridTon, list,
                    "MaSP|Mã||45", "TenSP|Sản phẩm||220", "TenDM|Danh mục||100", "SoLuongTon|Tồn hiện tại|N0|70",
                    "MucTonToiThieu|Mức tối thiểu|N0|70", "GiaBan|Giá bán|N0|80", "TinhTrangKho|Tình trạng||85");
            });
            if (list != null)
                lblTonSummary.Text = list.Count + " sản phẩm  |  Tổng số lượng tồn: " + list.Sum(x => x.SoLuongTon).ToString("N0") +
                                     "  |  Dòng tô đỏ: tồn kho ≤ mức tối thiểu (" + list.Count(x => x.SapHetHang()) + " sản phẩm). Nhấp đúp để nhập hàng.";
        }

        private void LapPhieuNhap(int? maSPOverride = null)
        {
            int maSP = maSPOverride.HasValue ? maSPOverride.Value
                : (UiHelper.Selected<SanPhamDTO>(gridTon)?.MaSP ?? 0);
            using (var f = new frmNhapKho(maSP, false))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    LoadTonKho();
                    if (gridPN.DataSource != null) LoadPhieuNhap();
                    gridLS.DataSource = null;
                }
            }
        }

        private void LoadPhieuNhap()
        {
            List<PhieuNhapKhoDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetPhieuNhap(pnFrom.Value, pnTo.Value);
                UiHelper.Bind(gridPN, list,
                    "MaPN|Mã phiếu||55", "NgayNhap|Ngày nhập|dd/MM/yyyy HH:mm|110", "TongTienNhap|Tổng tiền nhập|N0|95",
                    "TenQTV|Người lập||120", "GhiChu|Ghi chú||220");
            });
            if (list != null)
                lblPnSummary.Text = list.Count + " phiếu nhập trong kỳ  |  Tổng giá trị nhập: " +
                                    list.Sum(x => x.TongTienNhap).ToString("N0") + " đ";
            LoadChiTietPhieuNhap();
        }

        private void LoadChiTietPhieuNhap()
        {
            PhieuNhapKhoDTO pn = UiHelper.Selected<PhieuNhapKhoDTO>(gridPN);
            List<ChiTietPhieuNhapDTO> items = new List<ChiTietPhieuNhapDTO>();
            if (pn != null)
            {
                UiHelper.Try(this, () => items = bll.GetChiTietPhieuNhap(pn.MaPN));
                lblCtTitle.Text = "      Chi tiết phiếu nhập PN" + pn.MaPN + "  ·  " + pn.NgayNhap.ToString("dd/MM/yyyy HH:mm") +
                                  "  ·  Tổng: " + pn.TongTienNhap.ToString("N0") + " đ";
            }
            else
            {
                lblCtTitle.Text = "      Chi tiết phiếu nhập";
            }
            UiHelper.Bind(gridCTPN, items,
                "MaSP|Mã SP||50", "TenSP|Sản phẩm||250", "SoLuongNhap|Số lượng nhập|N0|80", "GiaNhap|Giá nhập|N0|80", "ThanhTien|Thành tiền|N0|90");
        }

        private void LoadLichSu()
        {
            List<LichSuKhoDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetLichSu(lsFrom.Value, lsTo.Value, txtLsSP.Text);
                UiHelper.Bind(gridLS, list,
                    "Ngay|Ngày|dd/MM/yyyy HH:mm|90", "Loai|Loại||60", "ChungTu|Chứng từ||60", "TenSP|Sản phẩm||220",
                    "SoLuong|Số lượng|N0|60", "DonGia|Đơn giá|N0|80", "ThanhTien|Thành tiền|N0|90");
            });
            if (list != null)
                lblLsSummary.Text = "Tổng nhập: " + list.Where(x => x.Loai == "Nhập kho").Sum(x => x.SoLuong).ToString("N0") +
                                    "  |  Tổng xuất: " + list.Where(x => x.Loai != "Nhập kho").Sum(x => x.SoLuong).ToString("N0") +
                                    "  (Xuất = đơn hàng đã xác nhận / đang giao / đã giao)";
        }
    }

    /// <summary>
    /// UC 1.2.12 - Lập phiếu nhập kho (cộng dồn tồn kho trong transaction).
    /// Hỗ trợ chạy trực tiếp nhúng trong frmMain (menu "Nhập kho") hoặc mở dạng hộp thoại từ Quản lý tồn kho.
    /// </summary>
    public class frmNhapKho : Form
    {
        private readonly BindingList<ChiTietPhieuNhapDTO> lines = new BindingList<ChiTietPhieuNhapDTO>();
        private readonly bool embedded;
        private readonly int initialMaSP;

        private readonly ComboBox cboDanhMuc;
        private readonly TextBox txtTimSP;
        private readonly ComboBox cboSP;
        private readonly PictureBox picSP;
        private readonly Label lblSpMeta;
        private readonly Label lblSpStock;
        private readonly Label lblSpGiaBan;
        private readonly NumericUpDown numSL;
        private readonly NumericUpDown numGia;
        private readonly Label lblLineTotal;
        private readonly Button btnAdd;
        private readonly Button btnCancelEdit;

        private readonly DataGridView grid;
        private readonly Label lblGridCount;
        private readonly TextBox txtGhiChu;
        private readonly Label lblSummaryMeta;
        private readonly Label lblTong;

        private List<SanPhamDTO> allProducts = new List<SanPhamDTO>();
        private List<DanhMucDTO> categories = new List<DanhMucDTO>();
        private int editingMaSP;
        private bool bindingProducts;

        public frmNhapKho(int maSPChon = 0, bool embedded = false)
        {
            this.initialMaSP = maSPChon;
            this.embedded = embedded;

            if (embedded)
            {
                UiHelper.SetupChild(this, "Nhập kho");
                BackColor = UiHelper.PageBack;
            }
            else
            {
                Text = "Lập phiếu nhập kho - SportZone Admin";
                Icon = UiHelper.AppIcon;
                Font = UiHelper.BaseFont;
                StartPosition = FormStartPosition.CenterParent;
                Size = new Size(1080, 690);
                MinimumSize = new Size(920, 580);
                MinimizeBox = false;
                ShowInTaskbar = false;
                BackColor = UiHelper.PageBack;
                Padding = new Padding(14);
            }

            // ----- bố cục chính 2 cột: trái chọn & nhập dòng (375px), phải danh sách + tổng kết
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UiHelper.PageBack,
                Margin = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 385));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ========================================================= CỘT TRÁI: THÊM SẢN PHẨM VÀO PHIẾU
            var leftCard = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 10, 18, 16),
                Margin = new Padding(0, 0, 14, 0),
                AutoScroll = true
            };
            var leftStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };
            leftStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cboDanhMuc = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill };
            txtTimSP = new TextBox { BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill };
            cboSP = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill };

            // Khung xem trước thông tin sản phẩm đang chọn
            var previewBox = new Panel
            {
                Height = 90,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 249, 252),
                Padding = new Padding(10),
                Margin = new Padding(0, 6, 0, 8)
            };
            previewBox.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiHelper.Line))
                    e.Graphics.DrawRectangle(pen, 0, 0, previewBox.Width - 1, previewBox.Height - 1);
            };
            picSP = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(70, 70),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            lblSpMeta = new Label
            {
                Location = new Point(90, 10),
                Size = new Size(240, 22),
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = UiHelper.PrimaryDark,
                AutoEllipsis = true
            };
            lblSpGiaBan = new Label
            {
                Location = new Point(90, 34),
                Size = new Size(240, 20),
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.Muted,
                AutoEllipsis = true
            };
            lblSpStock = new Label
            {
                Location = new Point(90, 56),
                Size = new Size(240, 22),
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = UiHelper.Success,
                AutoEllipsis = true
            };
            previewBox.Resize += (s, e) =>
            {
                int infoW = Math.Max(120, previewBox.Width - 102);
                lblSpMeta.Width = infoW;
                lblSpGiaBan.Width = infoW;
                lblSpStock.Width = infoW;
            };
            previewBox.Controls.AddRange(new Control[] { picSP, lblSpMeta, lblSpGiaBan, lblSpStock });

            // Số lượng + nút tăng nhanh (+5, +10, +20)
            var qtyWrap = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 36,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0, 2, 0, 6)
            };
            qtyWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            qtyWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
            qtyWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
            qtyWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));

            numSL = UiHelper.MakeNumber(1, 100000, 1);
            numSL.Dock = DockStyle.Fill;
            numSL.Value = 10;
            numSL.Margin = new Padding(0, 3, 6, 0);
            qtyWrap.Controls.Add(numSL, 0, 0);
            qtyWrap.Controls.Add(MakeQuickQtyBtn("+5", 5), 1, 0);
            qtyWrap.Controls.Add(MakeQuickQtyBtn("+10", 10), 2, 0);
            qtyWrap.Controls.Add(MakeQuickQtyBtn("+20", 20), 3, 0);

            numGia = UiHelper.MakeNumber(0, 1000000000, 1000);
            numGia.Dock = DockStyle.Fill;
            numGia.Margin = new Padding(0, 2, 0, 4);

            lblLineTotal = new Label
            {
                Dock = DockStyle.Fill,
                Height = 24,
                Font = new Font("Segoe UI Semibold", 9.75F),
                ForeColor = UiHelper.Primary,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 0, 0, 10)
            };

            btnAdd = UiHelper.MakeButton("+ Thêm vào phiếu", UiHelper.Primary, 320);
            btnAdd.Dock = DockStyle.Fill;
            btnAdd.Height = 40;
            btnAdd.Margin = new Padding(0, 2, 0, 6);

            btnCancelEdit = UiHelper.MakeButton("Hủy sửa dòng", UiHelper.Gray, 320);
            btnCancelEdit.Dock = DockStyle.Fill;
            btnCancelEdit.Height = 34;
            btnCancelEdit.Margin = new Padding(0, 0, 0, 4);
            btnCancelEdit.Visible = false;

            AddStackedField(leftStack, "Lọc theo danh mục", cboDanhMuc);
            AddStackedField(leftStack, "Tìm nhanh sản phẩm (tên hoặc mã)", txtTimSP);
            AddStackedField(leftStack, "Chọn sản phẩm", cboSP);
            AddStackedControl(leftStack, previewBox);
            AddStackedField(leftStack, "Số lượng nhập", qtyWrap);
            AddStackedField(leftStack, "Đơn giá nhập (VNĐ · gợi ý 70% giá bán)", numGia);
            AddStackedControl(leftStack, lblLineTotal);
            AddStackedControl(leftStack, btnAdd);
            AddStackedControl(leftStack, btnCancelEdit);

            leftCard.Controls.Add(leftStack);
            leftCard.Controls.Add(UiHelper.MakeCardHeader("Thêm sản phẩm vào phiếu", Glyph.Add));

            // ========================================================= CỘT PHẢI: DANH SÁCH + TỔNG KẾT
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiHelper.PageBack,
                Margin = new Padding(0)
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 196));

            // --- Thẻ trên: Danh sách dòng hàng nhập
            var cardGrid = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 10, 16, 12),
                Margin = new Padding(0, 0, 0, 14)
            };
            var gridToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 6, 0, 4),
                BackColor = Color.White,
                WrapContents = false
            };
            Button btnLowStock = UiHelper.MakeButton("+ Nạp SP sắp hết", UiHelper.Orange, 165);
            Button btnEditLine = UiHelper.MakeButton("Sửa dòng", UiHelper.Gray, 110);
            Button btnRemoveLine = UiHelper.MakeButton("Xóa dòng", UiHelper.Danger, 110);
            Button btnClearAll = UiHelper.MakeButton("Xóa hết", UiHelper.Gray, 96);
            gridToolbar.Controls.AddRange(new Control[] { btnLowStock, btnEditLine, btnRemoveLine, btnClearAll });

            grid = UiHelper.MakeGrid();
            grid.RowTemplate.Height = 36;
            UiHelper.Bind(grid, lines,
                "MaSP|Mã SP||52", "TenSP|Sản phẩm||260", "SoLuongNhap|Số lượng|N0|75", "GiaNhap|Giá nhập|N0|95", "ThanhTien|Thành tiền|N0|105");

            lblGridCount = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.Muted,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };

            cardGrid.Controls.Add(grid);
            cardGrid.Controls.Add(lblGridCount);
            cardGrid.Controls.Add(gridToolbar);
            cardGrid.Controls.Add(UiHelper.MakeCardHeader("Danh sách hàng nhập trong phiếu", Glyph.Document));
            grid.BringToFront();

            // --- Thẻ dưới: Ghi chú + Tổng tiền + Nút lưu
            var cardBottom = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 10, 16, 12),
                Margin = new Padding(0)
            };
            var bottomGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0, 6, 0, 0)
            };
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));

            var notePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 14, 0), BackColor = Color.White };
            var lblGhiChu = new Label
            {
                Text = "Ghi chú phiếu nhập (nhà cung cấp, đợt hàng...):",
                Dock = DockStyle.Top,
                Height = 22,
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.Muted
            };
            txtGhiChu = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 255
            };
            notePanel.Controls.Add(txtGhiChu);
            notePanel.Controls.Add(lblGhiChu);
            txtGhiChu.BringToFront();

            var summaryPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            string tenQtv = Session.CurrentAdmin != null ? Session.CurrentAdmin.HoTen : "Quản trị viên";
            var lblCreator = new Label
            {
                Text = "Người lập: " + tenQtv + "   ·   " + DateTime.Now.ToString("dd/MM/yyyy"),
                Dock = DockStyle.Top,
                Height = 20,
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.Muted,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblSummaryMeta = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.TextColor,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblTong = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 14.5F, FontStyle.Bold),
                ForeColor = UiHelper.Danger,
                TextAlign = ContentAlignment.MiddleRight
            };

            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.White
            };
            Button btnSave = UiHelper.MakeButton("Lưu phiếu nhập", UiHelper.Primary, 158);
            btnSave.Height = 38;
            Button btnSecondary = UiHelper.MakeButton(embedded ? "Làm mới" : "Hủy", UiHelper.Gray, 108);
            btnSecondary.Height = 38;
            actionFlow.Controls.Add(btnSave);
            actionFlow.Controls.Add(btnSecondary);

            summaryPanel.Controls.Add(actionFlow);
            summaryPanel.Controls.Add(lblTong);
            summaryPanel.Controls.Add(lblSummaryMeta);
            summaryPanel.Controls.Add(lblCreator);

            bottomGrid.Controls.Add(notePanel, 0, 0);
            bottomGrid.Controls.Add(summaryPanel, 1, 0);
            cardBottom.Controls.Add(bottomGrid);
            cardBottom.Controls.Add(UiHelper.MakeCardHeader("Xác nhận phiếu nhập", Glyph.Save));

            rightLayout.Controls.Add(cardGrid, 0, 0);
            rightLayout.Controls.Add(cardBottom, 0, 1);

            root.Controls.Add(leftCard, 0, 0);
            root.Controls.Add(rightLayout, 1, 0);
            Controls.Add(root);

            // ========================================================= SỰ KIỆN
            cboDanhMuc.SelectedIndexChanged += (s, e) => FilterProducts();
            UiHelper.OnTextChangedDebounced(txtTimSP, () => FilterProducts());
            cboSP.SelectedIndexChanged += (s, e) => OnProductSelected();
            numSL.ValueChanged += (s, e) => UpdateLinePreview();
            numGia.ValueChanged += (s, e) => UpdateLinePreview();
            numSL.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddOrUpdateLine(); } };
            numGia.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddOrUpdateLine(); } };

            btnAdd.Click += (s, e) => AddOrUpdateLine();
            btnCancelEdit.Click += (s, e) => CancelEditMode();
            btnLowStock.Click += (s, e) => LoadLowStockProducts();
            btnEditLine.Click += (s, e) => BeginEditSelectedLine();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BeginEditSelectedLine(); };
            btnRemoveLine.Click += (s, e) => RemoveSelectedLine();
            btnClearAll.Click += (s, e) =>
            {
                if (lines.Count == 0) return;
                if (!UiHelper.Confirm(this, "Xóa toàn bộ " + lines.Count + " dòng hàng đang nhập trong phiếu?")) return;
                lines.Clear();
                CancelEditMode();
                UpdateTotal();
            };

            btnSave.Click += (s, e) => Save();
            btnSecondary.Click += (s, e) =>
            {
                if (embedded)
                {
                    if (lines.Count > 0 && !UiHelper.Confirm(this, "Làm mới phiếu nhập đang lập? Các dòng chưa lưu sẽ bị xóa.")) return;
                    ResetForm();
                }
                else
                {
                    if (lines.Count > 0 && !UiHelper.Confirm(this, "Hủy phiếu nhập đang lập? Dữ liệu chưa được lưu.")) return;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            FormClosed += (s, e) =>
            {
                if (picSP.Image != null) { picSP.Image.Dispose(); picSP.Image = null; }
            };

            Load += (s, e) => LoadInitialData(initialMaSP);
        }

        private Button MakeQuickQtyBtn(string text, int addValue)
        {
            var b = new FlatButton
            {
                Text = text,
                Outline = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 2, 0, 2),
                Font = new Font("Segoe UI Semibold", 8.75F),
                Radius = 6
            };
            b.Click += (s, e) =>
            {
                decimal next = numSL.Value + addValue;
                numSL.Value = Math.Min(numSL.Maximum, next);
            };
            return b;
        }

        private static void AddStackedField(TableLayoutPanel table, string label, Control control)
        {
            int r1 = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Font = UiHelper.SmallFont,
                ForeColor = UiHelper.Muted,
                Margin = new Padding(0, 6, 0, 2)
            }, 0, r1);

            int r2 = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Margin = new Padding(0, 0, 0, 4);
            table.Controls.Add(control, 0, r2);
        }

        private static void AddStackedControl(TableLayoutPanel table, Control control)
        {
            int r = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, r);
        }

        private void LoadInitialData(int selectMaSP)
        {
            UiHelper.Try(this, () =>
            {
                allProducts = new SanPhamBLL().GetAll().OrderBy(x => x.TenSP).ToList();
                categories = new DanhMucBLL().GetAll().OrderBy(x => x.TenDM).ToList();
            });

            var dmItems = new List<DanhMucDTO> { new DanhMucDTO { MaDM = 0, TenDM = "Tất cả danh mục (" + allProducts.Count + " SP)" } };
            dmItems.AddRange(categories);
            cboDanhMuc.DisplayMember = "TenDM";
            cboDanhMuc.ValueMember = "MaDM";
            cboDanhMuc.DataSource = dmItems;

            FilterProducts(selectMaSP);
            UpdateTotal();
        }

        private void FilterProducts(int preferMaSP = 0)
        {
            var dm = cboDanhMuc.SelectedItem as DanhMucDTO;
            int maDM = dm == null ? 0 : dm.MaDM;
            string kw = (txtTimSP.Text ?? "").Trim().ToLowerInvariant();

            List<SanPhamDTO> filtered = allProducts.Where(p =>
                (maDM == 0 || p.MaDM == maDM) &&
                (kw == "" || (p.TenSP ?? "").ToLowerInvariant().Contains(kw) ||
                 ("sp" + p.MaSP.ToString("D2")).Contains(kw) || p.MaSP.ToString() == kw)
            ).ToList();

            int targetId = preferMaSP > 0 ? preferMaSP : ((cboSP.SelectedItem as SanPhamDTO)?.MaSP ?? 0);
            bindingProducts = true;
            cboSP.DataSource = null;
            cboSP.DataSource = filtered;
            SanPhamDTO match = filtered.FirstOrDefault(x => x.MaSP == targetId);
            if (match != null) cboSP.SelectedItem = match;
            else if (filtered.Count > 0) cboSP.SelectedIndex = 0;
            bindingProducts = false;

            OnProductSelected();
        }

        private void OnProductSelected()
        {
            if (bindingProducts) return;
            var sp = cboSP.SelectedItem as SanPhamDTO;
            if (picSP.Image != null) { picSP.Image.Dispose(); picSP.Image = null; }

            if (sp == null)
            {
                lblSpMeta.Text = "Chưa có sản phẩm phù hợp";
                lblSpGiaBan.Text = "Giá bán: —";
                lblSpStock.Text = "Tồn kho: —";
                lblSpStock.ForeColor = UiHelper.Muted;
                UpdateLinePreview();
                return;
            }

            picSP.Image = ImageHelper.Load(sp.HinhAnh);
            lblSpMeta.Text = "#SP" + sp.MaSP.ToString("D2") + "  ·  " + sp.TenDM;
            lblSpGiaBan.Text = "Giá bán: " + sp.GiaBan.ToString("N0") + " đ";
            lblSpStock.Text = "Tồn: " + sp.SoLuongTon.ToString("N0") + " / tối thiểu " + sp.MucTonToiThieu.ToString("N0") + "  (" + sp.TinhTrangKho + ")";
            lblSpStock.ForeColor = UiHelper.StockColor(sp.TinhTrangKho);

            if (editingMaSP != sp.MaSP)
            {
                CancelEditMode();
                numGia.Value = SuggestImportPrice(sp.GiaBan);
            }
            UpdateLinePreview();
        }

        private decimal SuggestImportPrice(decimal giaBan)
        {
            decimal v = Math.Round(giaBan * 0.7m / 1000m) * 1000m;
            if (v <= 0) v = giaBan;
            return Math.Min(numGia.Maximum, Math.Max(numGia.Minimum, v));
        }

        private void UpdateLinePreview()
        {
            decimal total = numSL.Value * numGia.Value;
            lblLineTotal.Text = "Thành tiền dòng: " + total.ToString("N0") + " đ";
        }

        private void AddOrUpdateLine()
        {
            var sp = cboSP.SelectedItem as SanPhamDTO;
            if (sp == null) { UiHelper.Warn(this, "Hãy chọn sản phẩm có trong danh sách."); return; }
            if (numSL.Value <= 0 || numGia.Value <= 0) { UiHelper.Warn(this, "Số lượng và đơn giá nhập phải lớn hơn 0."); return; }

            ChiTietPhieuNhapDTO existing = lines.FirstOrDefault(x => x.MaSP == sp.MaSP);
            if (existing != null)
            {
                if (editingMaSP == sp.MaSP)
                    existing.SoLuongNhap = (int)numSL.Value;
                else
                    existing.SoLuongNhap += (int)numSL.Value;
                existing.GiaNhap = numGia.Value;
                lines.ResetBindings();
            }
            else
            {
                lines.Add(new ChiTietPhieuNhapDTO
                {
                    MaSP = sp.MaSP,
                    TenSP = sp.TenSP,
                    SoLuongNhap = (int)numSL.Value,
                    GiaNhap = numGia.Value
                });
            }

            CancelEditMode();
            UpdateTotal();
        }

        private void BeginEditSelectedLine()
        {
            ChiTietPhieuNhapDTO ct = UiHelper.Selected<ChiTietPhieuNhapDTO>(grid);
            if (ct == null) { UiHelper.Warn(this, "Hãy chọn một dòng hàng trong bảng để sửa."); return; }

            if (cboDanhMuc.SelectedIndex != 0) cboDanhMuc.SelectedIndex = 0;
            if (txtTimSP.Text != "") txtTimSP.Text = "";
            FilterProducts(ct.MaSP);

            editingMaSP = ct.MaSP;
            numSL.Value = Math.Min(numSL.Maximum, Math.Max(numSL.Minimum, ct.SoLuongNhap));
            numGia.Value = Math.Min(numGia.Maximum, Math.Max(numGia.Minimum, ct.GiaNhap));
            btnAdd.Text = "Cập nhật dòng";
            btnCancelEdit.Visible = true;
            numSL.Focus();
        }

        private void CancelEditMode()
        {
            editingMaSP = 0;
            btnAdd.Text = "+ Thêm vào phiếu";
            btnCancelEdit.Visible = false;
        }

        private void RemoveSelectedLine()
        {
            ChiTietPhieuNhapDTO ct = UiHelper.Selected<ChiTietPhieuNhapDTO>(grid);
            if (ct == null) return;
            if (editingMaSP == ct.MaSP) CancelEditMode();
            lines.Remove(ct);
            UpdateTotal();
        }

        private void LoadLowStockProducts()
        {
            List<SanPhamDTO> low = allProducts.Where(x => x.SapHetHang()).ToList();
            if (low.Count == 0)
            {
                UiHelper.Info(this, "Hiện không có sản phẩm nào sắp hết hàng.");
                return;
            }
            int added = 0;
            foreach (SanPhamDTO sp in low)
            {
                if (lines.Any(x => x.MaSP == sp.MaSP)) continue;
                int goiY = Math.Max(5, sp.MucTonToiThieu * 2 - sp.SoLuongTon);
                lines.Add(new ChiTietPhieuNhapDTO
                {
                    MaSP = sp.MaSP,
                    TenSP = sp.TenSP,
                    SoLuongNhap = goiY,
                    GiaNhap = SuggestImportPrice(sp.GiaBan)
                });
                added++;
            }
            UpdateTotal();
            if (added == 0)
                UiHelper.Info(this, "Tất cả sản phẩm sắp hết hàng đã có sẵn trong phiếu nhập.");
            else
                UiHelper.Info(this, "Đã nạp " + added + " sản phẩm sắp hết hàng với số lượng và giá nhập gợi ý.");
        }

        private void UpdateTotal()
        {
            int soDong = lines.Count;
            int tongSL = lines.Sum(x => x.SoLuongNhap);
            decimal tongTien = lines.Sum(x => x.ThanhTien);

            lblGridCount.Text = soDong == 0
                ? "Chưa có sản phẩm nào. Chọn sản phẩm bên trái hoặc bấm \"+ Nạp SP sắp hết\"."
                : soDong + " sản phẩm trong phiếu  ·  Tổng số lượng nhập: " + tongSL.ToString("N0") + "  (nhấp đúp dòng để sửa)";
            lblSummaryMeta.Text = "Số mặt hàng: " + soDong + "   ·   Tổng SL nhập: " + tongSL.ToString("N0");
            lblTong.Text = "Tổng tiền: " + tongTien.ToString("N0") + " đ";
        }

        private void ResetForm()
        {
            lines.Clear();
            txtGhiChu.Clear();
            txtTimSP.Clear();
            CancelEditMode();
            LoadInitialData(0);
        }

        private void Save()
        {
            var pn = new PhieuNhapKhoDTO { GhiChu = txtGhiChu.Text };
            pn.SetChiTiet(lines.ToList());
            int maPN = 0;
            if (!UiHelper.Try(this, () => maPN = new KhoBLL().LapPhieuNhap(pn))) return;
            UiHelper.Info(this, "Đã lưu phiếu nhập PN" + maPN + ". Tồn kho đã được cộng dồn.");

            if (embedded)
            {
                ResetForm();
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
