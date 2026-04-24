namespace AdmissionPlex.Core.Interfaces.Services;

public interface ICCavenueService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
    string BuildRequestForm(string orderId, decimal amount, string currency, string redirectUrl, string cancelUrl, string customerEmail, string? customerPhone);
    Dictionary<string, string> ParseResponse(string encryptedResponse);
}
