using System;
using System.Collections.Generic;
using System.Data;       
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices; // 為了實現 Placeholder 效果
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
namespace final_project
{
    public partial class Form1 : Form
    {
       
        private readonly Color ColorSidebar = Color.FromArgb(243, 246, 252);
        private readonly Color ColorPrimary = Color.FromArgb(79, 70, 229);
        private readonly Color ColorTextDark = Color.FromArgb(30, 41, 59);

        private Panel pnlSidebar, pnlTopBar, pnlContent;
        private Label lblPageTitle;

       
        private DataTable purchaseCart; // 用於存放採購清單
        private DataTable cartTable; // 用來存購物車的內容
        public Form1()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1280, 800);
            this.Text = "進銷存系統 - 現代化版";
            this.Font = new Font("Microsoft JhengHei UI", 10);
            this.BackColor = Color.White;

            ApplyModernTheme();
        }

        private void ApplyModernTheme()
        {
            // 側邊
            pnlSidebar = new Panel { Width = 260, Dock = DockStyle.Left, BackColor = ColorSidebar, Padding = new Padding(15) };
            // 頂部
            pnlTopBar = new Panel { Height = 70, Dock = DockStyle.Top, BackColor = Color.White };
            lblPageTitle = new Label { Text = "Dashboard", Font = new Font("Microsoft JhengHei UI", 16, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true, ForeColor = ColorTextDark };
            pnlTopBar.Controls.Add(lblPageTitle);
            // 主內容
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(30) };
            // 生成選單
            string[] items = { "Dashboard", "客戶資料維護", "產品資料維護", "供應商資料維護", "銷貨結帳", "採購作業", "庫存報表", "備份還原" };
            int y = 80;
            foreach (var item in items)
            {
                Button btn = new Button
                {
                    Text = "   " + item,
                    Size = new Size(230, 48),
                    Location = new Point(15, y),
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => { lblPageTitle.Text = item; ShowModule(item); };
                pnlSidebar.Controls.Add(btn);
                y += 55;
            }

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTopBar);
            this.Controls.Add(pnlSidebar);
            ShowModule("Dashboard");
        }

        private void ShowModule(string module)
        {
            pnlContent.Controls.Clear();
            lblPageTitle.Text = module;

            // 1. 處理「備份還原」特殊頁面
            if (module == "備份還原")
            {
                pnlContent.Controls.Add(CreateBackupRestoreSection());
                return;
            }

            // 2. 處理「Dashboard」首頁
            if (module == "Dashboard")
            {
                Label lbl = new Label
                {
                    Text = "歡迎使用進銷存系統！\n目前資料庫狀態：連線正常\n系統時間：" + DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                    AutoSize = true,
                    Font = new Font("Microsoft JhengHei UI", 12),
                    Location = new Point(30, 30)
                };
                pnlContent.Controls.Add(lbl);
                return;
            }
            // 3. 處理「一般維護頁面」（自動生成工具列與 DataGrid）
            string sql = "";
            switch (module)
            {
                case "客戶資料維護": sql = "SELECT CustomerID AS [編號], CustomerName AS [客戶名稱], Phone AS [電話], Email AS [信箱] FROM Customers"; break;
                case "產品資料維護": sql = "SELECT ProductID AS [編號], ProductName AS [產品名稱], UnitPrice AS [建議售價], StockQuantity AS [庫存量], SafetyStock AS [安全水位] FROM Products"; break;
                case "供應商資料維護": sql = "SELECT SupplierID AS [編號], CompanyName AS [公司名稱], ContactName AS [聯絡人], Phone AS [電話] FROM Suppliers"; break;
                // 在 ShowModule 的 switch (module) 內修改
                case "採購作業":
                    pnlContent.Controls.Add(CreatePurchaseSection()); // 呼叫下方新建立的方法
                    break;
                case "庫存報表":
                    pnlContent.Controls.Add(CreateInventoryReportSection()); // 呼叫下方新建立的方法
                    break;
                case "銷貨結帳":
                    pnlContent.Controls.Add(CreateCheckoutSection());
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
            {
                pnlContent.Controls.Add(CreateGridSection(module, sql));
            }
        }

        // 專門處理備份還原的介面生成
        private Control CreateBackupRestoreSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            Label lblTitle = new Label { Text = "系統維護工具", Font = new Font("Microsoft JhengHei UI", 14, FontStyle.Bold), AutoSize = true, Location = new Point(10, 10) };
            Label lblDesc = new Label { Text = "建議定期進行資料庫備份，以防資料遺失。", ForeColor = Color.Gray, AutoSize = true, Location = new Point(10, 45) };

            // 備份按鈕
            // 執行資料庫備份
            Button btnBackup = UIHelper.CreateStyledButton("執行資料庫備份", ColorPrimary, 200, 50);
            btnBackup.Location = new Point(10, 90);
            btnBackup.Click += (s, e) => {
                string path = @"C:\Temp\InventoryBackup.bak";
                // 確保路徑資料夾存在，或是提示使用者
                if (!System.IO.Directory.Exists(@"C:\Temp")) System.IO.Directory.CreateDirectory(@"C:\Temp");

                string backupSql = $@"BACKUP DATABASE [InventorySystem] TO DISK = '{path}' WITH FORMAT";
                if (DBHelper.ExecuteNonQuery(backupSql) != -1)
                {
                    MessageBox.Show($"備份成功！\n路徑：{path}", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // 還原按鈕
            // 執行資料庫還原
            Button btnRestore = UIHelper.CreateStyledButton("執行資料庫還原", Color.FromArgb(100, 116, 139), 200, 50);
            btnRestore.Location = new Point(220, 90);
            btnRestore.Click += (s, e) => {
                if (MessageBox.Show("還原將會覆蓋現有資料，確定執行嗎？", "確認還原", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    string path = @"C:\Temp\InventoryBackup.bak";
                    // 還原 SQL 需要切換到 master 資料庫或強行中斷連線，這裡提供基礎指令
                    string restoreSql = $@"USE master; ALTER DATABASE [InventorySystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; 
                                   RESTORE DATABASE [InventorySystem] FROM DISK = '{path}' WITH REPLACE;
                                   ALTER DATABASE [InventorySystem] SET MULTI_USER;";

                    if (DBHelper.ExecuteNonQuery(restoreSql) != -1)
                    {
                        MessageBox.Show("還原成功！系統將自動重新載入。", "完成");
                        Application.Restart(); 
                    }
                }
            };

            container.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnBackup, btnRestore });
            return container;
        }

        
        private Control CreateGridSection(string title, string sql)
        {
            Panel container = new Panel { Dock = DockStyle.Fill };
            Panel toolBar = new Panel { Height = 60, Dock = DockStyle.Top, Padding = new Padding(0, 10, 0, 10) };

            // 工具列按鈕
            TextBox txtSearch = new TextBox { Width = 180, Location = new Point(0, 15) };
            UIHelper.SetPlaceholder(txtSearch, "搜尋內容...");
            Button btnAdd = UIHelper.CreateStyledButton("新增", ColorPrimary, 80, 35);
            btnAdd.Location = new Point(200, 15);
            Button btnEdit = UIHelper.CreateStyledButton("修改", Color.Orange, 80, 35);
            btnEdit.Location = new Point(290, 15);
            Button btnDelete = UIHelper.CreateStyledButton("刪除", Color.Red, 80, 35);
            btnDelete.Location = new Point(380, 15);
            toolBar.Controls.AddRange(new Control[] { txtSearch, btnAdd, btnEdit, btnDelete });

            // 資料表格
            DataTable data = DBHelper.GetDataTable(sql);
            DataGridView dgv = new DataGridView
            {
                DataSource = data,
                Dock = DockStyle.Fill,
            };
            UIHelper.SetGridStyle(dgv);

            // 產品相片預覽
            if (title == "產品資料維護")
            {
                Panel pnlPhoto = new Panel { Width = 220, Dock = DockStyle.Right, Padding = new Padding(10), BackColor = Color.FromArgb(248, 250, 252) };
                PictureBox pb = new PictureBox { Width = 200, Height = 200, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
                pnlPhoto.Controls.Add(pb);
                container.Controls.Add(pnlPhoto);

              
                dgv.SelectionChanged += (s, e) => {
                    if (dgv.SelectedRows.Count > 0)
                    {
                        var id = dgv.SelectedRows[0].Cells[0].Value;
                        DataTable dt = DBHelper.GetDataTable(string.Format("SELECT ProductImage FROM Products WHERE ProductID = {0}", id));

                        if (dt.Rows.Count > 0 && dt.Rows[0]["ProductImage"] != DBNull.Value)
                        {
                            try
                            {
                                byte[] bytes = (byte[])dt.Rows[0]["ProductImage"];
                                using (MemoryStream ms = new MemoryStream(bytes))
                                {
                                    // 釋放舊圖片資源，避免記憶體洩漏
                                    if (pb.Image != null) pb.Image.Dispose();
                                    pb.Image = Image.FromStream(ms);
                                }
                            }
                            catch
                            {
                                pb.Image = null;
                            }
                        }
                        else { pb.Image = null; }
                    }
                };
            }

            // 按鈕點擊事件 (呼叫 EditForm)
            // --- 替換 CreateGridSection 裡的按鈕事件 ---

            // 新增按鈕邏輯
            btnAdd.Click += (s, e) => {
                // 呼叫 DBHelper 取得對應的資料表設定
                var config = DBHelper.GetConfig(title);
                if (config == null) return;

                using (EditForm f = new EditForm(config.TableName, config.PrimaryKey))
                {
                    if (f.ShowDialog() == DialogResult.OK) ShowModule(title);
                }
            };

            // 修改按鈕邏輯
            btnEdit.Click += (s, e) => {
                if (dgv.SelectedRows.Count > 0)
                {
                    var config = DBHelper.GetConfig(title);
                    if (config == null) return;

                    var id = dgv.SelectedRows[0].Cells[0].Value;
                    using (EditForm f = new EditForm(config.TableName, config.PrimaryKey, id))
                    {
                        if (f.ShowDialog() == DialogResult.OK) ShowModule(title);
                    }
                }
                else
                {
                    MessageBox.Show("請先選擇要修改的資料。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            btnDelete.Click += (s, e) => {
                // 1. 檢查是否有選取資料
                if (dgv.SelectedRows.Count > 0)
                {

                    // 2. 透過字典取得資料表設定
                    var config = DBHelper.GetConfig(title);
                    if (config == null) return;

                    // 3. 取得選取列的 ID (主鍵)
                    var id = dgv.SelectedRows[0].Cells[0].Value;

                    // 4. 呼叫通用的 DeleteRecord，若成功則重新載入畫面
                    if (DBHelper.DeleteRecord(config.TableName, config.PrimaryKey, id))
                    {
                        ShowModule(title);
                    }
                }
                else
                {
                    MessageBox.Show("請先選擇要刪除的資料。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            // 搜尋功能實作
            txtSearch.TextChanged += (s, e) => {
                if (dgv.DataSource is DataTable dt)
                    dt.DefaultView.RowFilter = string.Format("[{0}] LIKE '%{1}%'", dt.Columns[1].ColumnName, txtSearch.Text);
            };

            container.Controls.Add(dgv);
            container.Controls.Add(toolBar);
            return container;
        }

       
        // --- 重構後的銷貨結帳模組 ---
        private Control CreateCheckoutSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductID", typeof(int));
            cartTable.Columns.Add("產品名稱", typeof(string));
            cartTable.Columns.Add("單價", typeof(decimal));
            cartTable.Columns.Add("數量", typeof(int));
            cartTable.Columns.Add("小計", typeof(decimal), "單價 * 數量");

            // --- 左側：輸入區 ---
            Panel pnlInput = new Panel { Width = 350, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            Label lblCust = new Label { Text = "1. 選擇客戶", Top = 10, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cbCust = new ComboBox { Top = 35, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCust.DataSource = DBHelper.GetDataTable("SELECT CustomerID, CustomerName FROM Customers");
            cbCust.DisplayMember = "CustomerName"; cbCust.ValueMember = "CustomerID";

            Label lblProd = new Label { Text = "2. 選擇商品", Top = 80, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cbProd = new ComboBox { Top = 105, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbProd.DataSource = DBHelper.GetDataTable("SELECT ProductID, ProductName, UnitPrice, StockQuantity FROM Products");
            cbProd.DisplayMember = "ProductName"; cbProd.ValueMember = "ProductID";

            Label lblQty = new Label { Text = "3. 輸入數量", Top = 155, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            NumericUpDown numQty = new NumericUpDown { Top = 180, Width = 100, Minimum = 1, Maximum = 9999, Value = 1 };

            // 加入清單
            Button btnAdd = UIHelper.CreateStyledButton("加入清單", Color.FromArgb(16, 185, 129), 300, 40);
            btnAdd.Location = new Point(15, 230);

            // 移除選取品項
            Button btnRemove = UIHelper.CreateStyledButton("移除選取品項", Color.FromArgb(239, 68, 68), 300, 40);
            btnRemove.Location = new Point(15, 280);

          

            pnlInput.Controls.AddRange(new Control[] { lblCust, cbCust, lblProd, cbProd, lblQty, numQty, btnAdd, btnRemove });

            // --- 右側：購物車 ---
            Panel pnlCart = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 0, 0) };
            DataGridView dgvCart = new DataGridView
            {
                DataSource = cartTable,
                Dock = DockStyle.Top,
                Height = 450,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // 【功能】開放數量欄位編輯
            dgvCart.ReadOnly = false;
            dgvCart.DataBindingComplete += (s, e) => {
                foreach (DataGridViewColumn col in dgvCart.Columns)
                {
                    if (col.Name != "數量") col.ReadOnly = true;
                }
            };

            Label lblTotal = new Label { Text = "總金額：$ 0", Top = 470, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            // 確認結帳 (產生單據)
            Button btnConfirm = UIHelper.CreateStyledButton("確認結帳 (產生單據)", ColorPrimary, 250, 50);
            btnConfirm.Location = new Point(0, 530);

            pnlCart.Controls.AddRange(new Control[] { dgvCart, lblTotal, btnConfirm });

            // --- 邏輯 1：加入清單 (含重複合併) ---
            btnAdd.Click += (s, e) => {
                DataRowView selProd = (DataRowView)cbProd.SelectedItem;
                int pid = Convert.ToInt32(selProd["ProductID"]);
                int stock = Convert.ToInt32(selProd["StockQuantity"]);
                int want = (int)numQty.Value;

                DataRow[] found = cartTable.Select("ProductID = " + pid);
                if (found.Length > 0)
                {
                    int current = Convert.ToInt32(found[0]["數量"]);
                    if (current + want > stock) { MessageBox.Show("超過庫存！"); return; }
                    found[0]["數量"] = current + want;
                }
                else
                {
                    if (want > stock) { MessageBox.Show("庫存不足！"); return; }
                    cartTable.Rows.Add(pid, selProd["ProductName"], selProd["UnitPrice"], want);
                }
                UpdateTotal(lblTotal);
            };

            // --- 邏輯 2：移除功能 ---
            btnRemove.Click += (s, e) => {
                if (dgvCart.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvCart.SelectedRows) cartTable.Rows.Remove(((DataRowView)row.DataBoundItem).Row);
                    UpdateTotal(lblTotal);
                }
            };

            // --- 邏輯 3：即時數量連動 ---
            cartTable.RowChanged += (s, e) => UpdateTotal(lblTotal);
            cartTable.RowDeleted += (s, e) => UpdateTotal(lblTotal);

            btnConfirm.Click += (s, e) => {
                if (cartTable.Rows.Count == 0) return;
                decimal total = (decimal)cartTable.Compute("Sum(小計)", "");
                if (DBHelper.PerformSalesTransaction((int)cbCust.SelectedValue, total, cartTable))
                {
                    MessageBox.Show("結帳成功！");
                    cartTable.Rows.Clear(); UpdateTotal(lblTotal);
                    ShowModule("銷貨結帳");
                }
            };

            container.Controls.Add(pnlCart);
            container.Controls.Add(pnlInput);
            return container;
        }

        // --- 重構後的採購作業模組 (修復排版跑位與按鈕消失問題) ---
        private Control CreatePurchaseSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            purchaseCart = new DataTable();
            purchaseCart.Columns.Add("ProductID", typeof(int));
            purchaseCart.Columns.Add("產品名稱", typeof(string));
            purchaseCart.Columns.Add("進貨單價", typeof(decimal));
            purchaseCart.Columns.Add("數量", typeof(int));
            purchaseCart.Columns.Add("小計", typeof(decimal), "進貨單價 * 數量");

            Panel pnlInput = new Panel { Width = 350, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            Label lblSupp = new Label { Text = "1. 選擇供應商", Top = 10, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            ComboBox cbSupp = new ComboBox { Top = 35, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbSupp.DataSource = DBHelper.GetDataTable("SELECT SupplierID, CompanyName FROM Suppliers");
            cbSupp.DisplayMember = "CompanyName"; cbSupp.ValueMember = "SupplierID";

            Label lblProd = new Label { Text = "2. 選擇產品", Top = 80, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            ComboBox cbProd = new ComboBox { Top = 105, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbProd.DataSource = DBHelper.GetDataTable("SELECT ProductID, ProductName, UnitPrice FROM Products");
            cbProd.DisplayMember = "ProductName"; cbProd.ValueMember = "ProductID";

            Label lblCost = new Label { Text = "3. 進貨單價", Top = 155, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            NumericUpDown numCost = new NumericUpDown { Top = 180, Width = 150, Maximum = 999999, DecimalPlaces = 0 };

            Label lblQty = new Label { Text = "4. 進貨數量", Top = 230, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            NumericUpDown numQty = new NumericUpDown { Top = 255, Width = 100, Minimum = 1, Maximum = 9999, Value = 1 };

            // 加入採購清單
            Button btnAddPurchase = UIHelper.CreateStyledButton("加入採購清單", ColorPrimary, 300, 40);
            btnAddPurchase.Location = new Point(15, 310);

            // 移除選取品項
            Button btnRemovePurchase = UIHelper.CreateStyledButton("移除選取品項", Color.FromArgb(239, 68, 68), 300, 40);
            btnRemovePurchase.Location = new Point(15, 360);

            pnlInput.Controls.AddRange(new Control[] { lblSupp, cbSupp, lblProd, cbProd, lblCost, numCost, lblQty, numQty, btnAddPurchase, btnRemovePurchase });

            Panel pnlListContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 0, 0) };
            DataGridView dgvPurchase = new DataGridView
            {
                DataSource = purchaseCart,
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = false
            };

            // 開放數量編輯
            dgvPurchase.DataBindingComplete += (s, e) => {
                foreach (DataGridViewColumn col in dgvPurchase.Columns) if (col.Name != "數量") col.ReadOnly = true;
            };

            Panel pnlBottom = new Panel { Height = 100, Dock = DockStyle.Bottom };
            Label lblTotal = new Label { Text = "總計進貨成本：$ 0", Top = 15, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            // 確認入庫 (增加庫存)
            Button btnConfirmPurchase = UIHelper.CreateStyledButton("確認入庫 (增加庫存)", Color.FromArgb(16, 185, 129), 250, 45);
            btnConfirmPurchase.Location = new Point(0, 50);

            pnlBottom.Controls.AddRange(new Control[] { lblTotal, btnConfirmPurchase });
            pnlListContainer.Controls.Add(dgvPurchase);
            pnlListContainer.Controls.Add(pnlBottom);

            // --- 邏輯：合併、移除、連動 ---
            btnAddPurchase.Click += (s, e) => {
                DataRowView selProd = (DataRowView)cbProd.SelectedItem;
                int pid = Convert.ToInt32(selProd["ProductID"]);
                DataRow[] found = purchaseCart.Select("ProductID = " + pid);
                if (found.Length > 0) found[0]["數量"] = Convert.ToInt32(found[0]["數量"]) + (int)numQty.Value;
                else purchaseCart.Rows.Add(pid, selProd["ProductName"], numCost.Value, (int)numQty.Value);
            };

            btnRemovePurchase.Click += (s, e) => {
                if (dgvPurchase.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvPurchase.SelectedRows) purchaseCart.Rows.Remove(((DataRowView)row.DataBoundItem).Row);
                }
            };

            purchaseCart.RowChanged += (s, e) => {
                object sum = purchaseCart.Compute("Sum(小計)", "");
                lblTotal.Text = "總計進貨成本：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
            };

            btnConfirmPurchase.Click += (s, e) => {
                if (purchaseCart.Rows.Count == 0) return;
                if (DBHelper.PerformPurchaseTransaction((int)cbSupp.SelectedValue, purchaseCart))
                {
                    MessageBox.Show("採購成功！"); purchaseCart.Rows.Clear(); ShowModule("採購作業");
                }
            };

            container.Controls.Add(pnlListContainer);
            container.Controls.Add(pnlInput);
            return container;
        }

        private Control CreateInventoryReportSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill };

            // 1. 頂部資訊統計
            Panel pnlHeader = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = Color.White };
            Label lblTitle = new Label { Text = "📊 現有庫存狀況報表", Top = 20, Left = 10, Font = new Font("Microsoft JhengHei UI", 16, FontStyle.Bold), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);

            // 2. 取得資料
            string sql = @"SELECT ProductID AS [編號], ProductName AS [產品名稱], 
                   StockQuantity AS [目前庫存], SafetyStock AS [安全水位],
                   CASE WHEN StockQuantity < SafetyStock THEN '庫存不足' ELSE '正常' END AS [狀態]
                   FROM Products";
            DataTable data = DBHelper.GetDataTable(sql);

            // 3. 建立表格
            DataGridView dgv = new DataGridView
            {
                DataSource = data,
                Dock = DockStyle.Fill,
            };
            UIHelper.SetGridStyle(dgv);
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);

            // 高階功能：自動標色 (低於安全水位顯示紅色)
            dgv.DataBindingComplete += (s, e) => {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    int stock = Convert.ToInt32(row.Cells["目前庫存"].Value);
                    int safety = Convert.ToInt32(row.Cells["安全水位"].Value);
                    if (stock < safety)
                    {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                        row.Cells["狀態"].Style.Font = new Font(dgv.Font, FontStyle.Bold);
                    }
                }
            };

            container.Controls.Add(dgv);
            container.Controls.Add(pnlHeader);
            return container;
        }
        private void UpdateTotal(Label lbl)
        {
            object sum = cartTable.Compute("Sum(小計)", "");
            lbl.Text = "總金額：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
        }
        
    }
}