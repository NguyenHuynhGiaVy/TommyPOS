using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TommyPOS.Database;
using TommyPOS.Models;

namespace TommyPOS.Services
{
    public class PosService
    {
        // ─────────────────────────────────────────────
        #region CATEGORIES
        // ─────────────────────────────────────────────

        public List<Category> GetCategories()
        {
            var list = new List<Category>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Icon, DisplayOrder FROM Categories ORDER BY DisplayOrder ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Icon = reader.GetString(2),
                    DisplayOrder = reader.GetInt32(3)
                });
            }
            return list;
        }

        public void SaveCategory(Category category)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            if (category.Id == 0)
            {
                cmd.CommandText = "INSERT INTO Categories (Name, Icon, DisplayOrder) VALUES (@name, @icon, @order);";
            }
            else
            {
                cmd.CommandText = "UPDATE Categories SET Name = @name, Icon = @icon, DisplayOrder = @order WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", category.Id);
            }
            cmd.Parameters.AddWithValue("@name", category.Name);
            cmd.Parameters.AddWithValue("@icon", category.Icon);
            cmd.Parameters.AddWithValue("@order", category.DisplayOrder);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Xóa danh mục. Trả về false nếu còn sản phẩm thuộc danh mục này.</summary>
        public bool DeleteCategory(int categoryId)
        {
            using var conn = DatabaseHelper.GetConnection();

            // Kiểm tra còn sản phẩm không
            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = "SELECT COUNT(*) FROM Products WHERE CategoryId = @id;";
                chk.Parameters.AddWithValue("@id", categoryId);
                long cnt = (long)(chk.ExecuteScalar() ?? 0L);
                if (cnt > 0) return false;
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Categories WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", categoryId);
            cmd.ExecuteNonQuery();
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────
        #region PRODUCTS
        // ─────────────────────────────────────────────

        public List<Product> GetProducts(int categoryId = 0, string search = "", bool includeUnavailable = false)
        {
            var list = new List<Product>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            string query = @"
                SELECT p.Id, p.CategoryId, c.Name as CategoryName, p.Name, p.Price, p.Description, p.IsAvailable, p.ImageUrl
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE 1=1
            ";

            if (!includeUnavailable) query += " AND p.IsAvailable = 1";
            if (categoryId > 0) query += " AND p.CategoryId = @catId";
            if (!string.IsNullOrWhiteSpace(search)) query += " AND p.Name LIKE @search";
            query += " ORDER BY p.Name ASC;";

            cmd.CommandText = query;
            if (categoryId > 0) cmd.Parameters.AddWithValue("@catId", categoryId);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    CategoryName = reader.GetString(2),
                    Name = reader.GetString(3),
                    Price = Convert.ToDecimal(reader.GetDouble(4)),
                    Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    IsAvailable = reader.GetInt32(6) == 1,
                    ImageUrl = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }
            return list;
        }

        public void SaveProduct(Product product)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            if (product.Id == 0)
            {
                cmd.CommandText = "INSERT INTO Products (CategoryId, Name, Price, Description, IsAvailable, ImageUrl) VALUES (@cat, @name, @price, @desc, @avail, @img);";
            }
            else
            {
                cmd.CommandText = "UPDATE Products SET CategoryId = @cat, Name = @name, Price = @price, Description = @desc, IsAvailable = @avail, ImageUrl = @img WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", product.Id);
            }
            cmd.Parameters.AddWithValue("@cat", product.CategoryId);
            cmd.Parameters.AddWithValue("@name", product.Name);
            cmd.Parameters.AddWithValue("@price", product.Price);
            cmd.Parameters.AddWithValue("@desc", product.Description ?? "");
            cmd.Parameters.AddWithValue("@avail", product.IsAvailable ? 1 : 0);
            cmd.Parameters.AddWithValue("@img", product.ImageUrl ?? "");
            cmd.ExecuteNonQuery();
        }

        public void DeleteProduct(int productId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Products WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.ExecuteNonQuery();
        }

        #endregion

        // ─────────────────────────────────────────────
        #region TOPPINGS
        // ─────────────────────────────────────────────

        public List<ToppingItem> GetToppings(bool availableOnly = false)
        {
            var list = new List<ToppingItem>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = availableOnly
                ? "SELECT Id, Name, Price, IsAvailable FROM Toppings WHERE IsAvailable = 1 ORDER BY Id ASC;"
                : "SELECT Id, Name, Price, IsAvailable FROM Toppings ORDER BY Id ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ToppingItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = Convert.ToDecimal(reader.GetDouble(2)),
                    IsAvailable = reader.GetInt32(3) == 1
                });
            }
            return list;
        }

        public void SaveTopping(ToppingItem topping)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            if (topping.Id == 0)
            {
                cmd.CommandText = "INSERT INTO Toppings (Name, Price, IsAvailable) VALUES (@name, @price, @avail);";
            }
            else
            {
                cmd.CommandText = "UPDATE Toppings SET Name = @name, Price = @price, IsAvailable = @avail WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", topping.Id);
            }
            cmd.Parameters.AddWithValue("@name", topping.Name);
            cmd.Parameters.AddWithValue("@price", topping.Price);
            cmd.Parameters.AddWithValue("@avail", topping.IsAvailable ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void DeleteTopping(int toppingId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Toppings WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", toppingId);
            cmd.ExecuteNonQuery();
        }

        #endregion

        // ─────────────────────────────────────────────
        #region PRODUCT SIZES
        // ─────────────────────────────────────────────

        /// <summary>
        /// Lấy sizes cho một sản phẩm cụ thể. Nếu sản phẩm chưa có sizes riêng,
        /// trả về sizes mặc định (ProductId = 0).
        /// </summary>
        public List<ProductSize> GetSizesForProduct(int productId)
        {
            using var conn = DatabaseHelper.GetConnection();

            // Check if product has its own sizes
            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = "SELECT COUNT(*) FROM ProductSizes WHERE ProductId = @pid;";
                chk.Parameters.AddWithValue("@pid", productId);
                long cnt = (long)(chk.ExecuteScalar() ?? 0L);
                if (cnt > 0)
                {
                    return LoadSizes(conn, productId);
                }
            }

            // Fall back to global defaults (ProductId = 0)
            return LoadSizes(conn, 0);
        }

        public List<ProductSize> GetAllGlobalSizes()
        {
            using var conn = DatabaseHelper.GetConnection();
            return LoadSizes(conn, 0);
        }

        private static List<ProductSize> LoadSizes(SqliteConnection conn, int productId)
        {
            var list = new List<ProductSize>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ProductId, SizeLabel, PriceExtra, DisplayOrder FROM ProductSizes WHERE ProductId = @pid ORDER BY DisplayOrder ASC;";
            cmd.Parameters.AddWithValue("@pid", productId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ProductSize
                {
                    Id = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    SizeLabel = reader.GetString(2),
                    PriceExtra = Convert.ToDecimal(reader.GetDouble(3)),
                    DisplayOrder = reader.GetInt32(4)
                });
            }
            return list;
        }

        public void SaveProductSize(ProductSize size)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            if (size.Id == 0)
            {
                cmd.CommandText = "INSERT INTO ProductSizes (ProductId, SizeLabel, PriceExtra, DisplayOrder) VALUES (@pid, @label, @extra, @order);";
            }
            else
            {
                cmd.CommandText = "UPDATE ProductSizes SET ProductId = @pid, SizeLabel = @label, PriceExtra = @extra, DisplayOrder = @order WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", size.Id);
            }
            cmd.Parameters.AddWithValue("@pid", size.ProductId);
            cmd.Parameters.AddWithValue("@label", size.SizeLabel);
            cmd.Parameters.AddWithValue("@extra", size.PriceExtra);
            cmd.Parameters.AddWithValue("@order", size.DisplayOrder);
            cmd.ExecuteNonQuery();
        }

        public void DeleteProductSize(int sizeId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ProductSizes WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", sizeId);
            cmd.ExecuteNonQuery();
        }

        #endregion

        // ─────────────────────────────────────────────
        #region TABLES
        // ─────────────────────────────────────────────

        public List<DiningTable> GetTables()
        {
            var list = new List<DiningTable>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Capacity, Status, CurrentOrderId FROM DiningTables ORDER BY Id ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var table = new DiningTable
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Capacity = reader.GetInt32(2),
                    Status = (TableStatus)reader.GetInt32(3),
                    CurrentOrderId = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                };

                if (table.CurrentOrderId.HasValue)
                {
                    table.CurrentTotal = GetOrderTotal(table.CurrentOrderId.Value);
                }

                list.Add(table);
            }
            return list;
        }

        public void SaveTable(DiningTable table)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            if (table.Id == 0)
            {
                cmd.CommandText = "INSERT INTO DiningTables (Name, Capacity, Status) VALUES (@name, @cap, 0);";
            }
            else
            {
                cmd.CommandText = "UPDATE DiningTables SET Name = @name, Capacity = @cap WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", table.Id);
            }
            cmd.Parameters.AddWithValue("@name", table.Name);
            cmd.Parameters.AddWithValue("@cap", table.Capacity);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Xóa bàn. Trả về false nếu bàn đang có khách (Occupied).</summary>
        public bool DeleteTable(int tableId)
        {
            using var conn = DatabaseHelper.GetConnection();

            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = "SELECT Status FROM DiningTables WHERE Id = @id;";
                chk.Parameters.AddWithValue("@id", tableId);
                var val = chk.ExecuteScalar();
                if (val != null && val != DBNull.Value && (long)val == 1)
                    return false; // Occupied
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DiningTables WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", tableId);
            cmd.ExecuteNonQuery();
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────
        #region ORDERS
        // ─────────────────────────────────────────────

        public Order GetOrCreateActiveOrder(int? tableId, string tableName)
        {
            using var conn = DatabaseHelper.GetConnection();

            if (tableId.HasValue)
            {
                using var tableCmd = conn.CreateCommand();
                tableCmd.CommandText = "SELECT CurrentOrderId FROM DiningTables WHERE Id = @tblId;";
                tableCmd.Parameters.AddWithValue("@tblId", tableId.Value);
                var currentOrdObj = tableCmd.ExecuteScalar();
                if (currentOrdObj != null && currentOrdObj != DBNull.Value)
                {
                    int ordId = Convert.ToInt32(currentOrdObj);
                    var existingOrder = GetOrderById(ordId);
                    if (existingOrder != null && existingOrder.Status == PaymentStatus.Pending)
                    {
                        return existingOrder;
                    }
                }
            }

            // Create new order
            var newOrder = new Order
            {
                OrderCode = "ORD" + DateTime.Now.ToString("yyMMddHHmmss"),
                TableId = tableId,
                TableName = string.IsNullOrWhiteSpace(tableName) ? "Mang về" : tableName,
                OrderDate = DateTime.Now,
                Status = PaymentStatus.Pending
            };

            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Orders (OrderCode, TableId, TableName, OrderDate, SubTotal, DiscountPercent, DiscountAmount, TotalAmount, Status, PaymentMethod, CashierName)
                VALUES (@code, @tblId, @tblName, @date, 0, 0, 0, 0, 0, 'Tiền mặt', 'Thu Ngân');
                SELECT last_insert_rowid();
            ";
            insertCmd.Parameters.AddWithValue("@code", newOrder.OrderCode);
            insertCmd.Parameters.AddWithValue("@tblId", (object?)tableId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@tblName", newOrder.TableName);
            insertCmd.Parameters.AddWithValue("@date", newOrder.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"));

            newOrder.Id = Convert.ToInt32(insertCmd.ExecuteScalar());

            if (tableId.HasValue)
            {
                using var updTable = conn.CreateCommand();
                updTable.CommandText = "UPDATE DiningTables SET Status = 1, CurrentOrderId = @ordId WHERE Id = @tblId;";
                updTable.Parameters.AddWithValue("@ordId", newOrder.Id);
                updTable.Parameters.AddWithValue("@tblId", tableId.Value);
                updTable.ExecuteNonQuery();
            }

            return newOrder;
        }

        public Order? GetOrderById(int orderId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, OrderCode, TableId, TableName, OrderDate, SubTotal, DiscountPercent, DiscountAmount, TotalAmount, CashGiven, ChangeAmount, Status, PaymentMethod, CashierName FROM Orders WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var order = new Order
            {
                Id = reader.GetInt32(0),
                OrderCode = reader.GetString(1),
                TableId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                TableName = reader.GetString(3),
                OrderDate = DateTime.Parse(reader.GetString(4)),
                SubTotal = Convert.ToDecimal(reader.GetDouble(5)),
                DiscountPercent = Convert.ToDecimal(reader.GetDouble(6)),
                DiscountAmount = Convert.ToDecimal(reader.GetDouble(7)),
                TotalAmount = Convert.ToDecimal(reader.GetDouble(8)),
                CashGiven = Convert.ToDecimal(reader.GetDouble(9)),
                ChangeAmount = Convert.ToDecimal(reader.GetDouble(10)),
                Status = (PaymentStatus)reader.GetInt32(11),
                PaymentMethod = reader.GetString(12),
                CashierName = reader.GetString(13)
            };

            order.Details = GetOrderDetails(order.Id);
            return order;
        }

        public List<OrderDetail> GetOrderDetails(int orderId)
        {
            var list = new List<OrderDetail>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, Size, SizePriceExtra, Sugar, Ice, Toppings, ToppingPriceExtra, Note
                FROM OrderDetails
                WHERE OrderId = @ordId ORDER BY Id ASC;
            ";
            cmd.Parameters.AddWithValue("@ordId", orderId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new OrderDetail
                {
                    Id = reader.GetInt32(0),
                    OrderId = reader.GetInt32(1),
                    ProductId = reader.GetInt32(2),
                    ProductName = reader.GetString(3),
                    Quantity = reader.GetInt32(4),
                    UnitPrice = Convert.ToDecimal(reader.GetDouble(5)),
                    Size = reader.GetString(6),
                    SizePriceExtra = Convert.ToDecimal(reader.GetDouble(7)),
                    Sugar = reader.GetString(8),
                    Ice = reader.GetString(9),
                    Toppings = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    ToppingPriceExtra = Convert.ToDecimal(reader.GetDouble(11)),
                    Note = reader.IsDBNull(12) ? "" : reader.GetString(12)
                });
            }
            return list;
        }

        public void SaveOrderDetailItem(OrderDetail detail)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            if (detail.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO OrderDetails (OrderId, ProductId, ProductName, Quantity, UnitPrice, Size, SizePriceExtra, Sugar, Ice, Toppings, ToppingPriceExtra, Note)
                    VALUES (@ordId, @prodId, @prodName, @qty, @unitPrice, @size, @sizeExtra, @sugar, @ice, @toppings, @topExtra, @note);
                ";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE OrderDetails
                    SET Quantity = @qty, Size = @size, SizePriceExtra = @sizeExtra, Sugar = @sugar, Ice = @ice, Toppings = @toppings, ToppingPriceExtra = @topExtra, Note = @note
                    WHERE Id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", detail.Id);
            }

            cmd.Parameters.AddWithValue("@ordId", detail.OrderId);
            cmd.Parameters.AddWithValue("@prodId", detail.ProductId);
            cmd.Parameters.AddWithValue("@prodName", detail.ProductName);
            cmd.Parameters.AddWithValue("@qty", detail.Quantity);
            cmd.Parameters.AddWithValue("@unitPrice", detail.UnitPrice);
            cmd.Parameters.AddWithValue("@size", detail.Size);
            cmd.Parameters.AddWithValue("@sizeExtra", detail.SizePriceExtra);
            cmd.Parameters.AddWithValue("@sugar", detail.Sugar);
            cmd.Parameters.AddWithValue("@ice", detail.Ice);
            cmd.Parameters.AddWithValue("@toppings", detail.Toppings ?? "");
            cmd.Parameters.AddWithValue("@topExtra", detail.ToppingPriceExtra);
            cmd.Parameters.AddWithValue("@note", detail.Note ?? "");
            cmd.ExecuteNonQuery();

            RecalculateOrderTotal(detail.OrderId);
        }

        public void RemoveOrderDetailItem(int detailId, int orderId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM OrderDetails WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", detailId);
            cmd.ExecuteNonQuery();

            RecalculateOrderTotal(orderId);
        }

        public void RecalculateOrderTotal(int orderId)
        {
            var details = GetOrderDetails(orderId);
            decimal subTotal = 0;
            foreach (var d in details) subTotal += d.SubTotal;

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DiscountPercent FROM Orders WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);
            decimal discountPct = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
            decimal discountAmt = subTotal * (discountPct / 100m);
            decimal total = subTotal - discountAmt;

            using var updCmd = conn.CreateCommand();
            updCmd.CommandText = @"
                UPDATE Orders
                SET SubTotal = @sub, DiscountAmount = @discAmt, TotalAmount = @tot
                WHERE Id = @id;
            ";
            updCmd.Parameters.AddWithValue("@sub", subTotal);
            updCmd.Parameters.AddWithValue("@discAmt", discountAmt);
            updCmd.Parameters.AddWithValue("@tot", total);
            updCmd.Parameters.AddWithValue("@id", orderId);
            updCmd.ExecuteNonQuery();
        }

        public void ApplyDiscount(int orderId, decimal discountPercent)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Orders SET DiscountPercent = @disc WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@disc", discountPercent);
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();

            RecalculateOrderTotal(orderId);
        }

        public void CheckoutOrder(Order order)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Orders
                SET Status = 1, PaymentMethod = @payMethod, CashGiven = @cash, ChangeAmount = @change
                WHERE Id = @id;
            ";
            cmd.Parameters.AddWithValue("@payMethod", order.PaymentMethod);
            cmd.Parameters.AddWithValue("@cash", order.CashGiven);
            cmd.Parameters.AddWithValue("@change", order.ChangeAmount);
            cmd.Parameters.AddWithValue("@id", order.Id);
            cmd.ExecuteNonQuery();

            if (order.TableId.HasValue)
            {
                using var tblCmd = conn.CreateCommand();
                tblCmd.CommandText = "UPDATE DiningTables SET Status = 0, CurrentOrderId = NULL WHERE Id = @tblId;";
                tblCmd.Parameters.AddWithValue("@tblId", order.TableId.Value);
                tblCmd.ExecuteNonQuery();
            }
        }

        public void CancelOrder(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order == null) return;

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Orders SET Status = 2 WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();

            if (order.TableId.HasValue)
            {
                using var tblCmd = conn.CreateCommand();
                tblCmd.CommandText = "UPDATE DiningTables SET Status = 0, CurrentOrderId = NULL WHERE Id = @tblId;";
                tblCmd.Parameters.AddWithValue("@tblId", order.TableId.Value);
                tblCmd.ExecuteNonQuery();
            }
        }

        public decimal GetOrderTotal(int orderId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TotalAmount FROM Orders WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);
            var obj = cmd.ExecuteScalar();
            return obj != null && obj != DBNull.Value ? Convert.ToDecimal(obj) : 0m;
        }

        public List<Order> GetCompletedOrders(DateTime startDate, DateTime endDate)
        {
            var list = new List<Order>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, OrderCode, TableId, TableName, OrderDate, SubTotal, DiscountPercent, DiscountAmount, TotalAmount, CashGiven, ChangeAmount, Status, PaymentMethod, CashierName
                FROM Orders
                WHERE Status = 1 AND datetime(OrderDate) >= datetime(@start) AND datetime(OrderDate) <= datetime(@end)
                ORDER BY OrderDate DESC;
            ";
            cmd.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd 00:00:00"));
            cmd.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd 23:59:59"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ord = new Order
                {
                    Id = reader.GetInt32(0),
                    OrderCode = reader.GetString(1),
                    TableId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    TableName = reader.GetString(3),
                    OrderDate = DateTime.Parse(reader.GetString(4)),
                    SubTotal = Convert.ToDecimal(reader.GetDouble(5)),
                    DiscountPercent = Convert.ToDecimal(reader.GetDouble(6)),
                    DiscountAmount = Convert.ToDecimal(reader.GetDouble(7)),
                    TotalAmount = Convert.ToDecimal(reader.GetDouble(8)),
                    CashGiven = Convert.ToDecimal(reader.GetDouble(9)),
                    ChangeAmount = Convert.ToDecimal(reader.GetDouble(10)),
                    Status = (PaymentStatus)reader.GetInt32(11),
                    PaymentMethod = reader.GetString(12),
                    CashierName = reader.GetString(13)
                };
                ord.Details = GetOrderDetails(ord.Id);
                list.Add(ord);
            }
            return list;
        }

        public (decimal TodayRevenue, int TodayOrderCount, decimal AverageBill) GetTodayStats()
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*), COALESCE(SUM(TotalAmount), 0)
                FROM Orders
                WHERE Status = 1 AND date(OrderDate) = date('now', 'localtime');
            ";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int count = reader.GetInt32(0);
                decimal revenue = Convert.ToDecimal(reader.GetDouble(1));
                decimal avg = count > 0 ? revenue / count : 0;
                return (revenue, count, avg);
            }
            return (0m, 0, 0m);
        }

        public (int CategoryCount, int ProductCount, int TableCount, int SizeCount, int ToppingCount) GetEntityCounts()
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    (SELECT COUNT(*) FROM Categories) AS CatCount,
                    (SELECT COUNT(*) FROM Products) AS ProdCount,
                    (SELECT COUNT(*) FROM DiningTables) AS TblCount,
                    (SELECT COUNT(*) FROM ProductSizes) AS SzCount,
                    (SELECT COUNT(*) FROM Toppings) AS TopCount;
            ";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4)
                );
            }
            return (0, 0, 0, 0, 0);
        }

        public List<RevenueChartItem> GetRevenueDataByPeriod(string periodType)
        {
            var result = new List<RevenueChartItem>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            if (periodType == "month")
            {
                cmd.CommandText = @"
                    WITH RECURSIVE Months(MonthVal) AS (
                        SELECT strftime('%Y-01', 'now', 'localtime')
                        UNION ALL
                        SELECT strftime('%Y-%m', datetime(MonthVal || '-01', '+1 month'))
                        FROM Months 
                        WHERE MonthVal < strftime('%Y-12', 'now', 'localtime')
                    )
                    SELECT 
                        'Thg ' || cast(strftime('%m', m.MonthVal || '-01') as integer) AS Label,
                        COALESCE(SUM(o.TotalAmount), 0) AS TotalRev,
                        COUNT(o.Id) AS TotalCount
                    FROM Months m
                    LEFT JOIN Orders o ON strftime('%Y-%m', o.OrderDate) = m.MonthVal AND o.Status = 1
                    GROUP BY m.MonthVal
                    ORDER BY m.MonthVal ASC;
                ";
            }
            else if (periodType == "year")
            {
                cmd.CommandText = @"
                    WITH RECURSIVE Years(YearVal) AS (
                        SELECT strftime('%Y', 'now', 'localtime', '-4 years')
                        UNION ALL
                        SELECT strftime('%Y', datetime(YearVal || '-01-01', '+1 year'))
                        FROM Years 
                        WHERE YearVal < strftime('%Y', 'now', 'localtime')
                    )
                    SELECT 
                        'Năm ' || y.YearVal AS Label,
                        COALESCE(SUM(o.TotalAmount), 0) AS TotalRev,
                        COUNT(o.Id) AS TotalCount
                    FROM Years y
                    LEFT JOIN Orders o ON strftime('%Y', o.OrderDate) = y.YearVal AND o.Status = 1
                    GROUP BY y.YearVal
                    ORDER BY y.YearVal ASC;
                ";
            }
            else // "day" default
            {
                cmd.CommandText = @"
                    WITH RECURSIVE Dates(DateVal) AS (
                        SELECT date('now', '-6 days', 'localtime')
                        UNION ALL
                        SELECT date(DateVal, '+1 day') FROM Dates WHERE DateVal < date('now', 'localtime')
                    )
                    SELECT 
                        strftime('%d/%m', d.DateVal) AS Label,
                        COALESCE(SUM(o.TotalAmount), 0) AS TotalRev,
                        COUNT(o.Id) AS TotalCount
                    FROM Dates d
                    LEFT JOIN Orders o ON date(o.OrderDate) = d.DateVal AND o.Status = 1
                    GROUP BY d.DateVal
                    ORDER BY d.DateVal ASC;
                ";
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RevenueChartItem
                {
                    PeriodLabel = reader.GetString(0),
                    Revenue = Convert.ToDecimal(reader.GetDouble(1)),
                    OrderCount = reader.GetInt32(2)
                });
            }

            return result;
        }

        #endregion
    }

    public class RevenueChartItem
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
