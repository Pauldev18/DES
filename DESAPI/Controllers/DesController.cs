using DESAPI.Models;
using ESmart.Data;
using ESmart.QData;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DESAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public static CauHoiLT[] listCH = DSCauHoiLT.DsCauHoi;

        public DesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost("encrypt")]
        public IActionResult Encrypt([FromBody] EncryptRequest request)
        {
            string cipher = EncryptString(request.PlainText);
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine(connStr);
            using var conn = new NpgsqlConnection(connStr);

            conn.Open();
         

            var cmd = new NpgsqlCommand("INSERT INTO SBDS (des, type) VALUES (@des, @type)", conn);
            cmd.Parameters.AddWithValue("@des", cipher);
            cmd.Parameters.AddWithValue("@type", request.Type); 

            if (cmd.ExecuteNonQuery() > 0)
            {
                return Ok(new ResultResponse { Success = true, Data = cipher, Message = "Encrypted successfully" });
            }

            return BadRequest(new ResultResponse { Success = false, Message = "Failed to insert" });
        }

        [HttpPost("decrypt")]
        public IActionResult Decrypt([FromBody] DecryptRequest request)
        {
            if (string.IsNullOrEmpty(request.CipherText))
            {
                return BadRequest(new ResultResponse { Success = false, Message = "Thiếu CipherText." });
            }

            // Nếu MaDK rỗng thì dùng pass mặc định "aaffcc"
            string passphrase = string.IsNullOrWhiteSpace(request.MaDK) ? "aaffcc" : request.MaDK;

            string plain = DecryptString(request.CipherText, passphrase);

            if (string.IsNullOrEmpty(plain))
            {
                return BadRequest(new ResultResponse { Success = false, Message = "Giải mã thất bại." });
            }

            return Ok(new ResultResponse { Success = true, Data = plain, Message = "Giải mã thành công." });
        }


        [HttpGet("cauhoilt")]
        public IActionResult GetAllCauHoiLT()
        {
            var cauHoiBasic = listCH.Select(x => new
            {
                x.ID,
                x.CauDung
            }).ToList();

            return Ok(cauHoiBasic);
        }



        private string EncryptString(string message, string passphrase = "aaffcc")
        {
            using var md5Crypto = MD5.Create();
            var keyMd5 = md5Crypto.ComputeHash(Encoding.UTF8.GetBytes(passphrase));

            using var tdes = TripleDES.Create();
            tdes.Key = keyMd5;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            var dataBytes = Encoding.UTF8.GetBytes(message);
            var encryptedBytes = tdes.CreateEncryptor().TransformFinalBlock(dataBytes, 0, dataBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private string DecryptString(string cipher, string passphrase = "aaffcc")
        {
            try
            {
                using var md5Crypto = MD5.Create();
                var keyMd5 = md5Crypto.ComputeHash(Encoding.UTF8.GetBytes(passphrase));

                using var tdes = TripleDES.Create();
                tdes.Key = keyMd5;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                var cipherBytes = Convert.FromBase64String(cipher);
                var decryptedBytes = tdes.CreateDecryptor().TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        [HttpPost("submit-ketqua")]
        public IActionResult SubmitKetQua([FromBody] SubmitRequest request)
        {
            var soCauHoiTheoHang = new Dictionary<string, int>
            {
             // A
            { "AM", 25 },
            { "A.02", 25 },
            { "A.03", 25 },
            { "A", 25 },

            // A1
            { "A1M", 25 },
            { "A.01", 25 },
            { "A1", 25 },

            // B
            { "B.01", 30 },
            { "B.02", 30 },
            { "B.03", 30 },
            { "B.04", 30 },
            { "B.05", 30 },
            { "B",    30 },
            { "B1M", 25 },

            // C1
            { "C1", 35 },

            // C
            { "CM", 40 },
            { "C", 40 },

            // D1, D2, D, các nhóm E
            { "D1", 45 },
            { "D2", 45 },
            { "DM", 45 },
            { "C1E", 45 },
            { "D1E", 45 },
            { "BE", 45 },
            { "CE", 45 },
            { "D2E", 45 },
            { "DE", 45 },
            { "D", 45 }
            };

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            // B1: Insert trạng thái PROCESS
            using var conn = new NpgsqlConnection(connStr);
            conn.Open();

            using (var insertCmd = new NpgsqlCommand("INSERT INTO SBDS (des, type) VALUES (@des, @type)", conn))
            {
                insertCmd.Parameters.AddWithValue("@des", request.MaDK);
                insertCmd.Parameters.AddWithValue("@type", "PROCESS");
                insertCmd.ExecuteNonQuery();
            }

            string decrypted = DecryptString(request.KetQua, request.MaDK);

            if (string.IsNullOrEmpty(decrypted))
            {
                // B2: Update trạng thái FAILED
                using var updateFailCmd = new NpgsqlCommand("UPDATE SBDS SET type = @type WHERE des = @des", conn);
                updateFailCmd.Parameters.AddWithValue("@type", "FAILED");
                updateFailCmd.Parameters.AddWithValue("@des", request.MaDK);
                updateFailCmd.ExecuteNonQuery();

                return BadRequest(new ResultResponse
                {
                    Success = false,
                    Message = "Giải mã thất bại",
                    Data = null
                });
            }

            Dictionary<int, int> dictionary_0 = new()
            {
                { 19, 19 }, { 20, 20 }, { 21, 21 }, { 22, 22 }, { 23, 23 }, { 24, 24 },
                { 25, 25 }, { 26, 26 }, { 27, 27 }, { 28, 28 }, { 30, 30 }, { 32, 32 },
                { 34, 34 }, { 35, 35 }, { 47, 47 }, { 48, 48 }, { 52, 52 }, { 53, 53 },
                { 55, 55 }, { 58, 58 }, { 63, 63 }, { 64, 64 }, { 65, 65 }, { 66, 66 },
                { 67, 67 }, { 68, 68 }, { 70, 70 }, { 71, 71 }, { 72, 72 }, { 73, 73 },
                { 74, 74 }, { 85, 85 }, { 86, 86 }, { 87, 87 }, { 88, 88 }, { 89, 89 },
                { 90, 90 }, { 91, 91 }, { 92, 92 }, { 93, 93 }, { 97, 97 }, { 98, 98 },
                { 102, 102 }, { 117, 117 }, { 163, 163 }, { 165, 165 }, { 167, 167 },
                { 197, 197 }, { 198, 198 }, { 206, 206 }, { 215, 215 }, { 226, 226 },
                { 234, 234 }, { 245, 245 }, { 246, 246 }, { 252, 252 }, { 253, 253 },
                { 254, 254 }, { 255, 255 }, { 260, 260 }
            };
            Dictionary<int, int> dictionary_1 = new()
            {
                { 17, 17 }, { 18, 18 }, { 19, 19 }, { 20, 20 }, { 21, 21 }, { 22, 22 },
                { 23, 23 }, { 24, 24 }, { 25, 25 }, { 26, 26 }, { 27, 27 }, { 28, 28 },
                { 29, 29 }, { 30, 30 }, { 33, 33 }, { 35, 35 }, { 36, 36 }, { 37, 37 },
                { 40, 40 }, { 43, 43 }, { 45, 45 }, { 46, 46 }, { 47, 47 }, { 48, 48 },
                { 49, 49 }, { 50, 50 }, { 51, 51 }, { 52, 52 }, { 53, 53 },
                { 84, 84 }, { 91, 91 }, { 101, 101 }, { 109, 109 }, { 112, 112 },
                { 114, 114 }, { 118, 118 }, { 119, 119 }, { 143, 143 }, { 145, 145 },
                { 147, 147 }, { 150, 150 }, { 154, 154 }, { 161, 161 }, { 199, 199 },
                { 209, 209 }, { 210, 210 }, { 211, 211 }, { 214, 214 },
                { 227, 227 }, { 231, 231 }, { 242, 242 }, { 245, 245 }, { 248, 248 },
                { 258, 258 }, { 260, 260 }, { 261, 261 }, { 262, 262 }
            };

            var pairs = decrypted.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var ketQuaChuan = new List<(int ID, int CauChon)>();
            var cauHoiCoTheLamSai = new List<int>();
         
            Console.WriteLine($"🔍 Tổng số cặp ID-CauChon trong chuỗi giải mã: {pairs.Length}");

            for (int i = 0; i < pairs.Length; i++)
            {
                var parts = pairs[i].Split('-');
                if (parts.Length != 2) continue;

                if (int.TryParse(parts[0], out int id))
                {
                    var cauHoi = listCH.FirstOrDefault(x => x.ID == id);
                    if (cauHoi != null)
                    {
                        int chonDung = cauHoi.CauDung;

                        if (chonDung > 0)
                        {
                            ketQuaChuan.Add((id, chonDung));
                            if (!dictionary_0.ContainsKey(id) && !dictionary_1.ContainsKey(id))
                                cauHoiCoTheLamSai.Add(id);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Không tìm thấy câu hỏi với ID: {id} (chuỗi gốc: {pairs[i]})");

                        // B2: Update trạng thái FAILED
                        using var updateFailCmd = new NpgsqlCommand("UPDATE SBDS SET type = @type WHERE des = @des", conn);
                        updateFailCmd.Parameters.AddWithValue("@type", "FAILED");
                        updateFailCmd.Parameters.AddWithValue("@des", request.MaDK);
                        updateFailCmd.ExecuteNonQuery();

                        return BadRequest(new ResultResponse
                        {
                            Success = false,
                            Message = $"Không tìm thấy câu hỏi với ID: {id}",
                            Data = null
                        });
                    }
                }
            }


            int soCauSai = 0;
            if (cauHoiCoTheLamSai.Count > 0)
            {
                var random = new Random();
                soCauSai = random.Next(1, Math.Min(3, cauHoiCoTheLamSai.Count + 1));
                var idBiSai = cauHoiCoTheLamSai.OrderBy(_ => random.Next()).Take(soCauSai).ToList();

                foreach (var id in idBiSai)
                {
                    int index = ketQuaChuan.FindIndex(x => x.ID == id);
                    if (index != -1)
                    {
                        var current = ketQuaChuan[index];
                        int saiKhac = GetDapAnSai(current.CauChon, random);
                        ketQuaChuan[index] = (current.ID, saiKhac);
                    }
                }
            }

            string chuoiKetQua = string.Join(";", ketQuaChuan.Select(x => $"{x.ID}-{x.CauChon}")) + ";";
            string ketQuaMaHoa = EncryptString(chuoiKetQua, request.MaDK);
            // B3: Update trạng thái SUCCESS
            using (var updateCmd = new NpgsqlCommand("UPDATE SBDS SET type = @type WHERE des = @des", conn))
            {
                updateCmd.Parameters.AddWithValue("@type", "SUCCESS");
                updateCmd.Parameters.AddWithValue("@des", request.MaDK);
                updateCmd.ExecuteNonQuery();
            }
            var pairsDung = chuoiKetQua.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var hang = (request.HangGPLX ?? "").Trim().ToUpper();
            if (!soCauHoiTheoHang.TryGetValue(hang, out int soCauDungYeuCau))
            {
                return BadRequest(new ResultResponse
                {
                    Success = false,
                    Message = $"Hạng GPLX không hợp lệ: {request.HangGPLX}",
                    Data = null
                });
            }

            if (pairsDung.Length != soCauDungYeuCau)
            {
                return BadRequest(new ResultResponse
                {
                    Success = false,
                    Message = $"Số câu trả lời ({pairsDung.Length}) không khớp yêu cầu ({soCauDungYeuCau}) cho hạng {hang}",
                    Data = null
                });
            }

            return Ok(new
            {
                maDK = request.MaDK,
                ketQua = ketQuaMaHoa,
                soCauSai = soCauSai,
                soCauDung = pairsDung.Length - soCauSai
            });

        }
        private static int GetDapAnSai(int dung, Random rnd)
        {
            int[] all = { 1, 2, 4, 8 };
            return all.Where(x => x != dung).OrderBy(_ => rnd.Next()).First();
        }


    }

}
