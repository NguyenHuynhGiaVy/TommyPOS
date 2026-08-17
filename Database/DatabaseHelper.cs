using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TommyPOS.Models;

namespace TommyPOS.Database
{
    public static class DatabaseHelper
    {
        private const string ConnectionString = "Data Source=TommyPOS.db";

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static void InitializeDatabase()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Icon TEXT NOT NULL DEFAULT '☕',
                    DisplayOrder INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CategoryId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Description TEXT,
                    ImageUrl TEXT,
                    IsAvailable INTEGER DEFAULT 1,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
                );

                CREATE TABLE IF NOT EXISTS Toppings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL DEFAULT 0,
                    IsAvailable INTEGER DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS ProductSizes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL DEFAULT 0,
                    SizeLabel TEXT NOT NULL,
                    PriceExtra REAL NOT NULL DEFAULT 0,
                    DisplayOrder INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS DiningTables (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Capacity INTEGER DEFAULT 4,
                    Status INTEGER DEFAULT 0,
                    CurrentOrderId INTEGER
                );

                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderCode TEXT NOT NULL,
                    TableId INTEGER,
                    TableName TEXT NOT NULL,
                    OrderDate TEXT NOT NULL,
                    SubTotal REAL DEFAULT 0,
                    DiscountPercent REAL DEFAULT 0,
                    DiscountAmount REAL DEFAULT 0,
                    TotalAmount REAL DEFAULT 0,
                    CashGiven REAL DEFAULT 0,
                    ChangeAmount REAL DEFAULT 0,
                    Status INTEGER DEFAULT 0,
                    PaymentMethod TEXT DEFAULT 'Tiền mặt',
                    CashierName TEXT DEFAULT 'Thu Ngân'
                );

                CREATE TABLE IF NOT EXISTS OrderDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    Size TEXT DEFAULT 'M',
                    SizePriceExtra REAL DEFAULT 0,
                    Sugar TEXT DEFAULT '100%',
                    Ice TEXT DEFAULT 'Bình thường',
                    Toppings TEXT,
                    ToppingPriceExtra REAL DEFAULT 0,
                    Note TEXT,
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
                );
            ";
            cmd.ExecuteNonQuery();

            EnsureColumnsExist(conn);
            SeedInitialData(conn);
        }

        private static void EnsureColumnsExist(SqliteConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(Products);";
                using var reader = cmd.ExecuteReader();
                bool hasImageUrl = false;
                while (reader.Read())
                {
                    string colName = reader.GetString(1);
                    if (string.Equals(colName, "ImageUrl", StringComparison.OrdinalIgnoreCase))
                    {
                        hasImageUrl = true;
                        break;
                    }
                }
                reader.Close();

                if (!hasImageUrl)
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE Products ADD COLUMN ImageUrl TEXT DEFAULT '';";
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void SeedInitialData(SqliteConnection conn)
        {
            // Check if Categories already exist
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM Categories;";
                long count = (long)(checkCmd.ExecuteScalar() ?? 0);
                if (count > 0) return; // Data already seeded
            }

            // Seed Categories
            var categories = new[]
            {
                ("Cà Phê Truyền Thống", "☕", 1),
                ("Cà Phê Máy & Ý", "🥛", 2),
                ("Trà Trái Cây & Trà Milk", "🍑", 3),
                ("Đá Xay (Ice Blended)", "🥤", 4),
                ("Bánh Ngọt & Snack", "🥐", 5)
            };

            foreach (var cat in categories)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Categories (Name, Icon, DisplayOrder) VALUES (@name, @icon, @order);";
                cmd.Parameters.AddWithValue("@name", cat.Item1);
                cmd.Parameters.AddWithValue("@icon", cat.Item2);
                cmd.Parameters.AddWithValue("@order", cat.Item3);
                cmd.ExecuteNonQuery();
            }

            // Seed Products
            var products = new[]
            {
                (1, "Cà Phê Đen Đá",         25000m, "Cà phê Robusta Đắk Lắk đậm vị truyền thống"),
                (1, "Cà Phê Sữa Đá",         29000m, "Sự kết hợp hoàn hảo giữa cà phê đậm đà và sữa đặc ngọt ngào"),
                (1, "Bạc Xỉu Sài Gòn",       32000m, "Nhiều sữa ít cà phê, béo ngậy thơm ngon"),
                (1, "Cà Phê Muối Tommy",      35000m, "Lớp kem muối béo mặn kết hợp cà phê đậm đà chữ ký quán"),
                (1, "Cà Phê Trứng Hà Nội",   39000m, "Kem trứng đánh bông mịn thơm ngậy quyện cà phê nóng"),
                (2, "Espresso Double",         30000m, "Chiết xuất 100% Arabica thơm nồng đậm đà"),
                (2, "Americano Đá",            32000m, "Espresso pha loãng thanh nhẹ, sảng khoái"),
                (2, "Latte Đá",               42000m, "Cà phê Ý kết hợp sữa tươi thanh trùng mịn màng"),
                (2, "Cappuccino Nóng",         42000m, "Bọt sữa kem dày mịn chuẩn phong cách Ý"),
                (2, "Caramel Macchiato",       45000m, "Vị đắng espresso quyện sốt caramel ngọt ngào"),
                (3, "Trà Đào Cam Sả",         39000m, "Trà đào thơm mát kết hợp hương cam vàng và sả tươi"),
                (3, "Trà Vải Lài Kem Cheese", 45000m, "Trà hoa lài thanh mát cùng trái vải và lớp phô mai béo mặn"),
                (3, "Trà Sữa Ô Long Nướng",  39000m, "Trà Ô Long nướng đậm đà pha sữa tươi béo thơm"),
                (3, "Trà Chanh Giã Tay",      29000m, "Chanh tươi giã tay tỏa hương tinh dầu sảng khoái"),
                (4, "Matcha Đá Xay Kem Béo", 49000m, "Bột Matcha Uji Nhật Bản xay cùng sữa tươi và lớp whipped cream"),
                (4, "Cookie Cream Đá Xay",   49000m, "Bánh Oreo giòn rụm xay cùng sữa béo và sốt chocolate"),
                (4, "Cà Phê Cốt Dừa Đá Xay",45000m, "Cốt dừa Bến Tre thơm béo xay nhuyễn quyện cà phê đen"),
                (5, "Bánh Croissant Bơ Tỏi", 29000m, "Bánh sừng bò vỏ giòn rụm thơm lừng bơ tỏi"),
                (5, "Tiramisu Cacao",         35000m, "Bánh Tiramisu mềm mịn chuẩn vị cà phê cacao"),
                (5, "Bánh Mì Que Hải Phòng", 18000m, "Bánh mì que pate béo ngậy giòn giòn")
            };

            foreach (var prod in products)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Products (CategoryId, Name, Price, Description, IsAvailable) VALUES (@catId, @name, @price, @desc, 1);";
                cmd.Parameters.AddWithValue("@catId", prod.Item1);
                cmd.Parameters.AddWithValue("@name", prod.Item2);
                cmd.Parameters.AddWithValue("@price", prod.Item3);
                cmd.Parameters.AddWithValue("@desc", prod.Item4);
                cmd.ExecuteNonQuery();
            }

            // Seed Toppings
            var toppings = new[]
            {
                ("Trân châu đen",    5000m),
                ("Thạch củ năng",    5000m),
                ("Kem Cheese béo",   10000m),
                ("Bánh Flan trứng",  8000m),
                ("Pudding trứng",    7000m),
                ("Hạt lựu đỏ",      6000m)
            };

            foreach (var top in toppings)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Toppings (Name, Price, IsAvailable) VALUES (@name, @price, 1);";
                cmd.Parameters.AddWithValue("@name", top.Item1);
                cmd.Parameters.AddWithValue("@price", top.Item2);
                cmd.ExecuteNonQuery();
            }

            // Seed default global sizes (ProductId = 0 means global default)
            var defaultSizes = new[]
            {
                ("S", 0m,     1),
                ("M", 5000m,  2),
                ("L", 10000m, 3)
            };

            foreach (var sz in defaultSizes)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO ProductSizes (ProductId, SizeLabel, PriceExtra, DisplayOrder) VALUES (0, @label, @extra, @order);";
                cmd.Parameters.AddWithValue("@label", sz.Item1);
                cmd.Parameters.AddWithValue("@extra", sz.Item2);
                cmd.Parameters.AddWithValue("@order", sz.Item3);
                cmd.ExecuteNonQuery();
            }

            // Seed Tables
            var tables = new[]
            {
                ("Bàn 01 (Tầng 1)", 4),
                ("Bàn 02 (Tầng 1)", 4),
                ("Bàn 03 (Tầng 1)", 2),
                ("Bàn 04 (Tầng 1)", 2),
                ("Bàn 05 (Sân Vườn)", 6),
                ("Bàn 06 (Sân Vườn)", 6),
                ("Bàn 07 (Tầng 2)", 4),
                ("Bàn 08 (Tầng 2)", 4),
                ("Bàn 09 (Sofa Vip)", 8),
                ("Bàn 10 (Sofa Vip)", 8),
                ("Bàn 11 (Ban Công)", 2),
                ("Bàn 12 (Ban Công)", 2),
            };

            foreach (var tbl in tables)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DiningTables (Name, Capacity, Status) VALUES (@name, @cap, 0);";
                cmd.Parameters.AddWithValue("@name", tbl.Item1);
                cmd.Parameters.AddWithValue("@cap", tbl.Item2);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
