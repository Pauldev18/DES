using DESAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DESAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public DesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("encrypt")]
        public IActionResult Encrypt([FromBody] EncryptRequest request)
        {
            string cipher = EncryptString(request.PlainText);
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            var cmd = new MySqlCommand("INSERT INTO sbds (des) VALUES (@des)", conn);
            cmd.Parameters.AddWithValue("@des", cipher);

            if (cmd.ExecuteNonQuery() > 0)
            {
                return Ok(new ResultResponse { Success = true, Data = cipher, Message = "Encrypted successfully" });
            }

            return BadRequest(new ResultResponse { Success = false, Message = "Failed to insert" });
        }

        [HttpPost("decrypt")]
        public IActionResult Decrypt([FromBody] DecryptRequest request)
        {
            string plain = DecryptString(request.CipherText);

            if (string.IsNullOrEmpty(plain))
            {
                return BadRequest(new ResultResponse { Success = false, Message = "Decryption failed" });
            }

            return Ok(new ResultResponse { Success = true, Data = plain, Message = "Decrypted successfully" });
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
    }
}