using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace final_project
{
    // 統一管理資料表設定
    public class TableConfig
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
    }

    public static class DBHelper
    {
        private static string connString = @"Data Source=.\sqlexpress;Initial Catalog=InventorySystem;Integrated Security=True;Encrypt=False";

        // ==========================================
        //  報表與資料表字典 (取代舊的 GetTableMetadata)
        // ==========================================
        public static TableConfig GetConfig(string moduleName)
        {
            switch (moduleName)
            {
                case "客戶資料維護": return new TableConfig { TableName = "Customers", PrimaryKey = "CustomerID" };
                case "產品資料維護": return new TableConfig { TableName = "Products", PrimaryKey = "ProductID" };
                case "供應商資料維護": return new TableConfig { TableName = "Suppliers", PrimaryKey = "SupplierID" };

                // --- 新增：主細檔報表支援點擊編輯 ---
                case "銷貨主檔報表": return new TableConfig { TableName = "SalesMaster", PrimaryKey = "SalesID" };
                case "銷貨明細報表": return new TableConfig { TableName = "SalesDetails", PrimaryKey = "DetailID" };
                case "採購主檔報表": return new TableConfig { TableName = "PurchaseMaster", PrimaryKey = "PurchaseID" };
                case "採購明細報表": return new TableConfig { TableName = "PurchaseDetails", PrimaryKey = "DetailID" };

                default: return null;
            }
        }

        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex) { MessageBox.Show("資料庫讀取失敗：\n" + ex.Message); }
            return dt;
        }

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show("指令執行失敗：\n" + ex.Message); return -1; }
        }

        public static bool DeleteRecord(string tableName, string pkName, object id)
        {
            if (MessageBox.Show("確定要刪除這筆資料嗎？\n(注意：若有相關連單據將無法刪除)", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string sql = string.Format("DELETE FROM [{0}] WHERE [{1}] = @id", tableName, pkName);
                SqlParameter[] p = { new SqlParameter("@id", id) };
                if (ExecuteNonQuery(sql, p) > 0)
                {
                    MessageBox.Show("刪除成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }
            return false;
        }

        // ==========================================
        //  路線 A：銷貨結帳交易 (SalesMaster + SalesDetails + 扣庫存)
        // ==========================================
        public static bool PerformSalesTransaction(int customerId, decimal total, DataTable cartTable)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    string sqlM = "INSERT INTO SalesMaster (CustomerID, TotalAmount, SalesDate, Status) OUTPUT INSERTED.SalesID VALUES (@cid, @total, GETDATE(), '已結帳')";
                    SqlCommand cmdM = new SqlCommand(sqlM, conn, trans);
                    cmdM.Parameters.AddWithValue("@cid", customerId);
                    cmdM.Parameters.AddWithValue("@total", total);
                    int salesId = (int)cmdM.ExecuteScalar();

                    foreach (DataRow row in cartTable.Rows)
                    {
                        string sqlD = "INSERT INTO SalesDetails (SalesID, ProductID, Quantity, UnitPrice) VALUES (@sid, @pid, @qty, @price)";
                        SqlCommand cmdD = new SqlCommand(sqlD, conn, trans);
                        cmdD.Parameters.AddWithValue("@sid", salesId);
                        cmdD.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdD.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdD.Parameters.AddWithValue("@price", row["單價"]);
                        cmdD.ExecuteNonQuery();

                        string sqlUp = "UPDATE Products SET StockQuantity = StockQuantity - @qty WHERE ProductID = @pid";
                        SqlCommand cmdUp = new SqlCommand(sqlUp, conn, trans);
                        cmdUp.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdUp.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdUp.ExecuteNonQuery();
                    }
                    trans.Commit(); return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback(); MessageBox.Show("結帳失敗：" + ex.Message); return false;
                }
            }
        }

        // ==========================================
        //  路線 A：採購入庫交易 (PurchaseMaster + PurchaseDetails + 加庫存)
        // ==========================================
        public static bool PerformPurchaseTransaction(int supplierId, decimal total, DataTable purchaseCart)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    string sqlM = "INSERT INTO PurchaseMaster (SupplierID, TotalAmount, PurchaseDate) OUTPUT INSERTED.PurchaseID VALUES (@sid, @total, GETDATE())";
                    SqlCommand cmdM = new SqlCommand(sqlM, conn, trans);
                    cmdM.Parameters.AddWithValue("@sid", supplierId);
                    cmdM.Parameters.AddWithValue("@total", total);
                    int purchaseId = (int)cmdM.ExecuteScalar();

                    foreach (DataRow row in purchaseCart.Rows)
                    {
                        string sqlD = "INSERT INTO PurchaseDetails (PurchaseID, ProductID, Quantity, UnitPrice) VALUES (@poid, @pid, @qty, @price)";
                        SqlCommand cmdD = new SqlCommand(sqlD, conn, trans);
                        cmdD.Parameters.AddWithValue("@poid", purchaseId);
                        cmdD.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdD.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdD.Parameters.AddWithValue("@price", row["進貨單價"]);
                        cmdD.ExecuteNonQuery();

                        string sqlUp = "UPDATE Products SET StockQuantity = StockQuantity + @qty WHERE ProductID = @pid";
                        SqlCommand cmdUp = new SqlCommand(sqlUp, conn, trans);
                        cmdUp.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdUp.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdUp.ExecuteNonQuery();
                    }
                    trans.Commit(); return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback(); MessageBox.Show("採購失敗：" + ex.Message); return false;
                }
            }
        }
    }
}