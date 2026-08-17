using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TommyPOS.Utils
{
    public static class VietQRHelper
    {
        // Default Bank Account Settings (Configurable / Public Demo STK)
        public const string DefaultBankBin = "970422"; // MB Bank
        public const string DefaultBankName = "MB Bank (Ngân hàng Quân Đội)";
        public const string DefaultAccountNo = "88060949553840";
        public const string DefaultAccountName = "NGUYEN HUYNH GIA VY";

        /// <summary>
        /// Generates real VietQR image from VietQR.io QuickLink API.
        /// Returns a Bitmap image scanmable by MB Bank, Vietcombank, Techcombank, MoMo, ZaloPay, VNPay, etc.
        /// </summary>
        public static async Task<Bitmap?> FetchVietQRImageAsync(string bankBin, string accountNo, string accountName, decimal amount, string orderCode)
        {
            try
            {
                string safeAccountName = Uri.EscapeDataString(accountName);
                string safeAddInfo = Uri.EscapeDataString(orderCode);
                long safeAmount = (long)amount;

                // VietQR Official QuickLink API (No Client-ID / API Key needed, 100% public & valid EMVCo standard)
                string url = $"https://img.vietqr.io/image/{bankBin}-{accountNo}-compact.png?amount={safeAmount}&addInfo={safeAddInfo}&accountName={safeAccountName}";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                byte[] data = await client.GetByteArrayAsync(url);
                if (data != null && data.Length > 0)
                {
                    using var ms = new MemoryStream(data);
                    using var img = Image.FromStream(ms);
                    return new Bitmap(img);
                }
            }
            catch
            {
                // Fallback to offline generator if no network
            }

            return null;
        }

        /// <summary>
        /// Builds VietQR EMVCo standardized payload string according to NAPAS / EMVCo spec.
        /// </summary>
        public static string BuildEMVCoPayload(string bankBin, string accountNo, decimal amount, string memo)
        {
            static string L(string val) => $"{val.Length:D2}{val}";
            static string Tag(string tag, string val) => $"{tag}{L(val)}";

            // Tag 38: Beneficiary Merchant Info
            string subTag00 = Tag("00", bankBin);
            string subTag01 = Tag("01", accountNo);
            string beneficiary = Tag("00", "A000000727") + Tag("01", subTag00 + subTag01) + Tag("02", "QRIBFTTA");

            // Strip Vietnamese accents for clean memo
            string cleanMemo = RemoveVietnameseAccents(memo);
            if (cleanMemo.Length > 25) cleanMemo = cleanMemo[..25];

            string payload = Tag("00", "01") +
                             Tag("01", "12") +
                             Tag("38", beneficiary) +
                             Tag("53", "704") +
                             Tag("54", amount.ToString("0")) +
                             Tag("58", "VN") +
                             Tag("62", Tag("08", cleanMemo)) +
                             "6304";

            ushort crc = CalculateCrc16(payload);
            return payload + crc.ToString("X4");
        }

        public static ushort CalculateCrc16(string text)
        {
            ushort crc = 0xFFFF;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            foreach (byte b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc = (ushort)(crc << 1);
                }
            }
            return crc;
        }

        public static string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string[] vietMap = {
                "aàáạảãâầấậẩẫăằắặẳẵ", "AÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ",
                "eèéẹẻẽêềếệểễ", "EÈÉẸẺẼÊỀẾỆỂỄ",
                "iìíịỉĩ", "IÌÍỊỈĨ",
                "oòóọỏõôồốộổỗơờớợởỡ", "OÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ",
                "uùúụủũưừứựửữ", "UÙÚỤỦŨƯỪỨỰỬỮ",
                "yỳýỵỷỹ", "YỲÝỴỶỸ",
                "dđ", "DĐ"
            };
            foreach (var group in vietMap)
            {
                char target = group[0];
                for (int i = 1; i < group.Length; i++)
                    text = text.Replace(group[i], target);
            }
            return text;
        }
    }
}
