using System.Security.Cryptography;
using System.Text;
using AdmissionPlex.Core.Interfaces.Services;

namespace AdmissionPlex.Api.Services;

public class CCavenueService : ICCavenueService
{
    private readonly string _workingKey;
    private readonly string _accessCode;
    private readonly string _merchantId;
    private readonly string _transactionUrl;

    public CCavenueService(IConfiguration configuration)
    {
        _workingKey = configuration["CCAvenue:WorkingKey"] ?? throw new InvalidOperationException("CCAvenue WorkingKey not configured.");
        _accessCode = configuration["CCAvenue:AccessCode"] ?? "";
        _merchantId = configuration["CCAvenue:MerchantId"] ?? "";
        _transactionUrl = configuration["CCAvenue:TransactionUrl"] ?? "https://test.ccavenue.com/transaction/transaction.do?command=initiateTransaction";
    }

    public string Encrypt(string plainText)
    {
        var hashKey = ComputeMd5Hash(_workingKey);
        var keyBytes = HexToBytes(hashKey);
        var iv = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                              0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128;
        aes.Key = keyBytes;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var inputBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        return BytesToHex(encrypted);
    }

    public string Decrypt(string encryptedText)
    {
        var hashKey = ComputeMd5Hash(_workingKey);
        var keyBytes = HexToBytes(hashKey);
        var iv = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                              0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128;
        aes.Key = keyBytes;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = HexToBytes(encryptedText);
        var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    public string BuildRequestForm(string orderId, decimal amount, string currency,
        string redirectUrl, string cancelUrl, string customerEmail, string? customerPhone)
    {
        var data = $"merchant_id={_merchantId}" +
                   $"&order_id={orderId}" +
                   $"&currency={currency}" +
                   $"&amount={amount:F2}" +
                   $"&redirect_url={redirectUrl}" +
                   $"&cancel_url={cancelUrl}" +
                   $"&language=EN" +
                   $"&billing_email={customerEmail}";

        if (!string.IsNullOrEmpty(customerPhone))
            data += $"&billing_tel={customerPhone}";

        var encrypted = Encrypt(data);

        return $@"<form id='ccavenue-form' method='post' action='{_transactionUrl}'>
            <input type='hidden' name='encRequest' value='{encrypted}' />
            <input type='hidden' name='access_code' value='{_accessCode}' />
        </form>
        <script>document.getElementById('ccavenue-form').submit();</script>";
    }

    public Dictionary<string, string> ParseResponse(string encryptedResponse)
    {
        var decrypted = Decrypt(encryptedResponse);
        var result = new Dictionary<string, string>();

        foreach (var pair in decrypted.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[parts[0]] = Uri.UnescapeDataString(parts[1]);
        }

        return result;
    }

    private static string ComputeMd5Hash(string input)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BytesToHex(hashBytes);
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
