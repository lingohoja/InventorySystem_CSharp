using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace final_project
{
    public class TableConfig
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
    }
    public static class DBHelper
    {
        // 統一管理連線字串
        private static string connString = @"Data Source=.\sqlexpress;Initial Catalog=InventorySystem;Integrated Security=True;Encrypt=False";
        public static TableConfig GetConfig(string moduleName)
        {
            switch (moduleName)
            {
                case "客戶資料維護":
                    return new TableConfig { TableName = "Customers", PrimaryKey = "CustomerID" };
                case "產品資料維護":
                    return new TableConfig { TableName = "Products", PrimaryKey = "ProductID" };
                case "供應商資料維護":
                    return new TableConfig { TableName = "Suppliers", PrimaryKey = "SupplierID" };
                default:
                    return null; // 找不到對應表時回傳 null
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
            catch (Exception ex)
            {
                MessageBox.Show("資料庫讀取失敗：\n" + ex.Message);
            }
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
            catch (Exception ex)
            {
                MessageBox.Show("指令執行失敗：\n" + ex.Message);
                return -1;
            }
        }

        // ==========================================
        //  重構封裝：銷貨結帳交易 (同時寫入單據與扣庫存)
        // ==========================================
        public static bool PerformSalesTransaction(int customerId, decimal total, DataTable cartTable)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. 寫入銷貨主檔
                    string sqlM = "INSERT INTO SalesMaster (CustomerID, TotalAmount, SalesDate) OUTPUT INSERTED.SalesID VALUES (@cid, @total, GETDATE())";
                    SqlCommand cmdM = new SqlCommand(sqlM, conn, trans);
                    cmdM.Parameters.AddWithValue("@cid", customerId);
                    cmdM.Parameters.AddWithValue("@total", total);
                    int salesId = (int)cmdM.ExecuteScalar();

                    // 2. 寫入明細並扣除庫存
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
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("結帳交易失敗：\n" + ex.Message);
                    return false;
                }
            }
        }

        // ==========================================
        //  重構封裝：採購入庫交易 (增加庫存)
        // ==========================================
        public static bool PerformPurchaseTransaction(int supplierId, DataTable purchaseCart)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    foreach (DataRow row in purchaseCart.Rows)
                    {
                        string sqlUp = "UPDATE Products SET StockQuantity = StockQuantity + @qty WHERE ProductID = @pid";
                        SqlCommand cmdUp = new SqlCommand(sqlUp, conn, trans);
                        cmdUp.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdUp.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdUp.ExecuteNonQuery();
                    }
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("採購交易失敗：\n" + ex.Message);
                    return false;
                }
            }
        }
        // ==========================================
        //  重構封裝：通用刪除邏輯
        // ==========================================
        public static bool DeleteRecord(string tableName, string pkName, object id)
        {
            // 統一處理防呆警告對話框
            DialogResult result = MessageBox.Show(
                "確定要永久刪除這筆資料嗎？\n(注意：若已有相關單據紀錄，為保持資料完整性將無法刪除)",
                "確認刪除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // 組合 SQL 語法，例如 DELETE FROM [Customers] WHERE [CustomerID] = @id
                string sql = string.Format("DELETE FROM [{0}] WHERE [{1}] = @id", tableName, pkName);
                SqlParameter[] p = { new SqlParameter("@id", id) };

                // 執行刪除，大於 0 代表有成功刪除資料
                if (ExecuteNonQuery(sql, p) > 0)
                {
                    MessageBox.Show("刪除成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }

            // 若選擇 No，或執行失敗，則回傳 false
            return false;
        }
    }
}