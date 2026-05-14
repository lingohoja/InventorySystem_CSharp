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
            string[] items = {
                "Dashboard",
                "購物車",
                "採購作業",
                "客戶資料維護",
                "產品資料維護",
                "供應商資料維護",
                "銷貨主檔報表",
                "銷貨明細報表",
                "採購主檔報表",
                "採購明細報表",
                "庫存報表",
                "備份還原"
            };
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

                // --- 新增：主檔與明細檔報表 ---
                case "銷貨主檔報表":
                    sql = @"SELECT M.SalesID AS [銷貨單號], M.SalesDate AS [銷售日期], 
                   C.CustomerName AS [客戶名稱], M.TotalAmount AS [總金額], M.Status AS [狀態] 
            FROM SalesMaster M 
            LEFT JOIN Customers C ON M.CustomerID = C.CustomerID";
                    break;
                case "銷貨明細報表":
                    sql = @"SELECT D.DetailID AS [明細編號], D.SalesID AS [銷貨單號], 
                   P.ProductName AS [產品名稱], D.Quantity AS [數量], D.UnitPrice AS [單價], D.SubTotal AS [小計] 
            FROM SalesDetails D 
            LEFT JOIN Products P ON D.ProductID = P.ProductID";
                    break;
                case "採購主檔報表":
                    sql = @"SELECT M.PurchaseID AS [採購單號], M.PurchaseDate AS [採購日期], 
                   S.CompanyName AS [供應商名稱], M.TotalAmount AS [總金額], M.Remark AS [備註] 
            FROM PurchaseMaster M 
            LEFT JOIN Suppliers S ON M.SupplierID = S.SupplierID";
                    break;
                case "採購明細報表":
                    sql = @"SELECT D.DetailID AS [明細編號], D.PurchaseID AS [採購單號], 
                   P.ProductName AS [產品名稱], D.Quantity AS [數量], D.UnitPrice AS [進價] 
            FROM PurchaseDetails D 
            LEFT JOIN Products P ON D.ProductID = P.ProductID";
                    break;

                // --- 獨立作業模組 ---
                case "購物車":
                    pnlContent.Controls.Add(CreateCheckoutSection());
                    break;
                case "採購作業":
                    pnlContent.Controls.Add(CreatePurchaseSection());
                    break;
                case "庫存報表":
                    pnlContent.Controls.Add(CreateInventoryReportSection());
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
            btnBackup.Click += (s, e) =>
            {
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
            btnRestore.Click += (s, e) =>
            {
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


                dgv.SelectionChanged += (s, e) =>
                {
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
            btnAdd.Click += (s, e) =>
            {
                // 呼叫 DBHelper 取得對應的資料表設定
                var config = DBHelper.GetConfig(title);
                if (config == null) return;

                using (EditForm f = new EditForm(config.TableName, config.PrimaryKey))
                {
                    if (f.ShowDialog() == DialogResult.OK) ShowModule(title);
                }
            };

            // 修改按鈕邏輯
            btnEdit.Click += (s, e) =>
            {
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
            btnDelete.Click += (s, e) =>
            {
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
            txtSearch.TextChanged += (s, e) =>
            {
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

            // --- 左側：輸入區域 ---
            Panel pnlInput = new Panel { Width = 350, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            Label lblCust = new Label { Text = "1. 選擇客戶", Top = 10, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            ComboBox cbCust = new ComboBox { Top = 35, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCust.DataSource = DBHelper.GetDataTable("SELECT CustomerID, CustomerName FROM Customers");
            cbCust.DisplayMember = "CustomerName"; cbCust.ValueMember = "CustomerID";

            Label lblProd = new Label { Text = "2. 選擇商品 (含目前庫存)", Top = 80, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            ComboBox cbProd = new ComboBox { Top = 105, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // 將庫存量顯示在選單中，讓結帳人員一目了然
            DataTable dtProd = DBHelper.GetDataTable("SELECT ProductID, ProductName + ' (庫存: ' + CAST(StockQuantity AS VARCHAR) + ')' AS DisplayName, ProductName, UnitPrice, StockQuantity FROM Products");
            cbProd.DataSource = dtProd;
            cbProd.DisplayMember = "DisplayName";
            cbProd.ValueMember = "ProductID";

            Label lblQty = new Label { Text = "3. 輸入數量", Top = 155, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            NumericUpDown numQty = new NumericUpDown { Top = 180, Width = 100, Minimum = 1, Maximum = 9999, Value = 1 };

            Button btnAdd = UIHelper.CreateStyledButton("加入購物車", Color.FromArgb(16, 185, 129), 300, 40);
            btnAdd.Location = new Point(15, 240);

            Button btnRemove = UIHelper.CreateStyledButton("移除選取品項", Color.FromArgb(239, 68, 68), 300, 40);
            btnRemove.Location = new Point(15, 290);

            pnlInput.Controls.AddRange(new Control[] { lblCust, cbCust, lblProd, cbProd, lblQty, numQty, btnAdd, btnRemove });

            // --- 右側：購物車區域 ---
            Panel pnlListContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 0, 0) };
            DataGridView dgvCart = new DataGridView
            {
                DataSource = cartTable,
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = false // 開放編輯
            };
            UIHelper.SetGridStyle(dgvCart);

            // 限制只能編輯「數量」欄位
            dgvCart.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgvCart.Columns) if (col.Name != "數量") col.ReadOnly = true;
            };

            Panel pnlBottom = new Panel { Height = 100, Dock = DockStyle.Bottom };
            Label lblTotal = new Label { Text = "總金額：$ 0", Top = 15, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            Button btnConfirm = UIHelper.CreateStyledButton("確認結帳 (產生銷貨單)", ColorPrimary, 250, 45);
            btnConfirm.Location = new Point(0, 50);

            pnlBottom.Controls.AddRange(new Control[] { lblTotal, btnConfirm });
            pnlListContainer.Controls.Add(dgvCart);
            pnlListContainer.Controls.Add(pnlBottom);

            // --- 核心邏輯 ---

            // 1. 加入購物車 (合併與防呆)
            btnAdd.Click += (s, e) =>
            {
                if (cbProd.SelectedItem == null) return;
                DataRowView selProd = (DataRowView)cbProd.SelectedItem;
                int pid = Convert.ToInt32(selProd["ProductID"]);
                int stock = Convert.ToInt32(selProd["StockQuantity"]);
                int want = (int)numQty.Value;

                DataRow[] found = cartTable.Select("ProductID = " + pid);
                if (found.Length > 0)
                {
                    int current = Convert.ToInt32(found[0]["數量"]);
                    if (current + want > stock)
                    {
                        MessageBox.Show($"庫存不足！清單已有 {current} 件，目前庫存僅剩 {stock} 件。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    found[0]["數量"] = current + want;
                }
                else
                {
                    if (want > stock)
                    {
                        MessageBox.Show($"庫存不足！目前僅剩 {stock} 件。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cartTable.Rows.Add(pid, selProd["ProductName"], selProd["UnitPrice"], want);
                }
            };

            // 2. 移除項目
            btnRemove.Click += (s, e) =>
            {
                if (dgvCart.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvCart.SelectedRows) cartTable.Rows.Remove(((DataRowView)row.DataBoundItem).Row);
                }
            };

            // 3. 自動更新總金額 (當新增、修改數量、或刪除時觸發)
            cartTable.RowChanged += (s, e) =>
            {
                object sum = cartTable.Compute("Sum(小計)", "");
                lblTotal.Text = "總金額：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
            };
            cartTable.RowDeleted += (s, e) =>
            {
                object sum = cartTable.Compute("Sum(小計)", "");
                lblTotal.Text = "總金額：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
            };

            // 4. 執行交易
            btnConfirm.Click += (s, e) =>
            {
                if (cartTable.Rows.Count == 0) return;

                object sumObj = cartTable.Compute("Sum(小計)", "");
                decimal total = sumObj == DBNull.Value ? 0 : Convert.ToDecimal(sumObj);

                // 呼叫 DBHelper 中的交易邏輯 (同時寫入 SalesMaster, SalesDetails 並扣庫存)
                if (DBHelper.PerformSalesTransaction((int)cbCust.SelectedValue, total, cartTable))
                {
                    MessageBox.Show("結帳成功！已寫入銷貨單並扣除庫存。");
                    cartTable.Rows.Clear();
                    ShowModule("銷貨結帳"); // 重新載入以更新下拉選單的庫存數字
                }
            };

            container.Controls.Add(pnlListContainer);
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

            Label lblProd = new Label { Text = "2. 選擇產品", Top = 80, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            ComboBox cbProd = new ComboBox { Top = 105, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblCost = new Label { Text = "3. 進貨單價", Top = 155, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            NumericUpDown numCost = new NumericUpDown { Top = 180, Width = 150, Maximum = 999999, DecimalPlaces = 0 };

            Label lblQty = new Label { Text = "4. 進貨數量", Top = 230, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            NumericUpDown numQty = new NumericUpDown { Top = 255, Width = 100, Minimum = 1, Maximum = 9999, Value = 1 };

            // --- 路線 A 核心邏輯：連動選單與自動報價 (修復初始空白問題) ---

            Action loadSupplierProducts = () =>
            {
                int sid = 0;
                // 嘗試從 SelectedValue 抓取
                if (cbSupp.SelectedValue != null && int.TryParse(cbSupp.SelectedValue.ToString(), out int parsed))
                {
                    sid = parsed;
                }
                // 【修復關鍵】：如果 SelectedValue 還沒準備好，直接從底層的 SelectedItem 抓取！
                else if (cbSupp.SelectedItem is DataRowView drv)
                {
                    sid = Convert.ToInt32(drv["SupplierID"]);
                }

                if (sid > 0)
                {
                    string sql = $@"SELECT P.ProductID, P.ProductName, SP.QuotePrice 
                            FROM Products P 
                            JOIN SupplierProducts SP ON P.ProductID = SP.ProductID 
                            WHERE SP.SupplierID = {sid}";
                    DataTable dtProds = DBHelper.GetDataTable(sql);

                    cbProd.DataSource = null;
                    cbProd.DataSource = dtProds;
                    cbProd.DisplayMember = "ProductName";
                    cbProd.ValueMember = "ProductID";

                    if (dtProds.Rows.Count > 0)
                    {
                        numCost.Value = Convert.ToDecimal(dtProds.Rows[0]["QuotePrice"]);
                    }
                    else
                    {
                        numCost.Value = 0;
                        cbProd.Text = "此供應商無設定商品";
                    }
                }
            };

            // 綁定產品改變時更新報價
            cbProd.SelectedIndexChanged += (s, e) =>
            {
                if (cbProd.SelectedItem is DataRowView row && row.Row.Table.Columns.Contains("QuotePrice"))
                {
                    if (row["QuotePrice"] != DBNull.Value) numCost.Value = Convert.ToDecimal(row["QuotePrice"]);
                }
            };

            // 綁定供應商改變時更新產品
            cbSupp.SelectedIndexChanged += (s, e) => loadSupplierProducts();

            // 載入供應商資料
            cbSupp.DataSource = DBHelper.GetDataTable("SELECT SupplierID, CompanyName FROM Suppliers");
            cbSupp.DisplayMember = "CompanyName";
            cbSupp.ValueMember = "SupplierID";

            // 【修復關鍵】：利用 BeginInvoke 確保 UI 資料綁定完成後，強制執行第一次載入
            this.BeginInvoke((MethodInvoker)delegate
            {
                loadSupplierProducts();
            });

            // ------------------------------------------

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
            UIHelper.SetGridStyle(dgvPurchase); // 套用通用表格美化

            // 開放數量編輯
            dgvPurchase.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgvPurchase.Columns) if (col.Name != "數量") col.ReadOnly = true;
            };

            Panel pnlBottom = new Panel { Height = 100, Dock = DockStyle.Bottom };
            Label lblTotal = new Label { Text = "總計進貨成本：$ 0", Top = 15, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };

            // 確認入庫 (增加庫存)
            Button btnConfirmPurchase = UIHelper.CreateStyledButton("確認入庫 (產生單據)", Color.FromArgb(16, 185, 129), 250, 45);
            btnConfirmPurchase.Location = new Point(0, 50);

            pnlBottom.Controls.AddRange(new Control[] { lblTotal, btnConfirmPurchase });
            pnlListContainer.Controls.Add(dgvPurchase);
            pnlListContainer.Controls.Add(pnlBottom);

            // --- 邏輯：合併、移除、連動 ---
            btnAddPurchase.Click += (s, e) =>
            {
                if (cbProd.SelectedItem == null)
                {
                    MessageBox.Show("該供應商目前無產品報價！", "提示");
                    return;
                }

                DataRowView selProd = (DataRowView)cbProd.SelectedItem;
                int pid = Convert.ToInt32(selProd["ProductID"]);
                DataRow[] found = purchaseCart.Select("ProductID = " + pid);
                if (found.Length > 0) found[0]["數量"] = Convert.ToInt32(found[0]["數量"]) + (int)numQty.Value;
                else purchaseCart.Rows.Add(pid, selProd["ProductName"], numCost.Value, (int)numQty.Value);
            };

            btnRemovePurchase.Click += (s, e) =>
            {
                if (dgvPurchase.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvPurchase.SelectedRows) purchaseCart.Rows.Remove(((DataRowView)row.DataBoundItem).Row);
                }
            };

            purchaseCart.RowChanged += (s, e) =>
            {
                object sum = purchaseCart.Compute("Sum(小計)", "");
                lblTotal.Text = "總計進貨成本：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
            };

            purchaseCart.RowDeleted += (s, e) =>
            {
                object sum = purchaseCart.Compute("Sum(小計)", "");
                lblTotal.Text = "總計進貨成本：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
            };

            btnConfirmPurchase.Click += (s, e) =>
            {
                if (purchaseCart.Rows.Count == 0) return;

                // 計算總金額
                object sumObj = purchaseCart.Compute("Sum(小計)", "");
                decimal total = sumObj == DBNull.Value ? 0 : Convert.ToDecimal(sumObj);

                // 呼叫更新後的 PerformPurchaseTransaction (傳入總額參數)
                if (DBHelper.PerformPurchaseTransaction((int)cbSupp.SelectedValue, total, purchaseCart))
                {
                    MessageBox.Show("採購成功！已寫入採購單並增加庫存。");
                    purchaseCart.Rows.Clear();
                    ShowModule("採購作業");
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
            dgv.DataBindingComplete += (s, e) =>
            {
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


    }
}