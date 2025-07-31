namespace DESAPI.Models
{
    public class DecryptRequest
    {
        public string CipherText { get; set; }
        public string? MaDK { get; set; }
    }
}
