using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json.Nodes;
using NewVivaApi.Models;

namespace NewVivaApi.Services;

public class FinancialSecurityService
{
    private readonly IConfiguration _configuration;
    private readonly string _aesKeyString;

    public FinancialSecurityService(IConfiguration configuration)
    {
        _configuration = configuration;
        _aesKeyString = _configuration["AWS:AesKey"] ?? throw new InvalidOperationException("AesKey configuration is missing");
    }

    public GeneralContractorsVw GenerateUnprotectedJsonAttributes(GeneralContractorsVw model)
    {
        if (string.IsNullOrEmpty(model.JsonAttributes))
            return model;

        try
        {
            var jsonAttributes = JsonNode.Parse(model.JsonAttributes)?.AsObject();
            
            if (jsonAttributes?.ContainsKey("IV") == true)
            {
                var unprotectedJsonAttributes = Decrypt(jsonAttributes);
                model.JsonAttributes = unprotectedJsonAttributes.ToJsonString();
            }
        }
        catch (Exception ex)
        {
            // Log the exception appropriately
            throw new InvalidOperationException("Failed to decrypt JSON attributes", ex);
        }

        return model;
    }

    public string GenerateUnprotectedJsonAttributes(string model)
    {
        if (string.IsNullOrEmpty(model))
            return model;

        try
        {
            var jsonAttributes = JsonNode.Parse(model)?.AsObject();

            if (jsonAttributes?.ContainsKey("IV") == true)
            {
                var unprotectedJsonAttributes = Decrypt(jsonAttributes);
                return unprotectedJsonAttributes.ToJsonString();
            }
        }
        catch (Exception ex)
        {
            // Log the exception appropriately
            throw new InvalidOperationException("Failed to decrypt JSON string", ex);
        }

        return model;
    }

    public string ProtectJsonAttributes(string jsonAttributes)
    {
        if (string.IsNullOrEmpty(jsonAttributes))
            return string.Empty;

        try
        {
            var jsonObj = JsonNode.Parse(jsonAttributes)?.AsObject();
            
            if (jsonObj == null)
                return string.Empty;

            if (ContainsSensitiveData(jsonObj))
            {
                var protectedJsonAttributes = Encrypt(jsonObj);
                return protectedJsonAttributes.ToJsonString();
            }
        }
        catch (Exception ex)
        {
            // Log the exception appropriately
            throw new InvalidOperationException("Failed to encrypt JSON attributes", ex);
        }

        return string.Empty;
    }

    private static bool ContainsSensitiveData(JsonObject jsonObj)
    {
        return jsonObj.ContainsKey("AccountNumber") ||
               jsonObj.ContainsKey("RoutingNumber") ||
               jsonObj.ContainsKey("PaymentAccountNumber") ||
               jsonObj.ContainsKey("PaymentRoutingNumber");
    }

    public JsonObject Encrypt(JsonObject jsonAttributes)
    {
        var key = AESService.CreateAESKey(_aesKeyString, out var iv);
        var aesKey = new AESService.AESKey(key, iv);

        var sensitiveFields = new[] { "AccountNumber", "RoutingNumber", "PaymentAccountNumber", "PaymentRoutingNumber" };

        foreach (var field in sensitiveFields)
        {
            EncryptField(jsonAttributes, field, aesKey);
        }

        jsonAttributes["IV"] = Convert.ToBase64String(iv);

        return jsonAttributes;
    }

    private static void EncryptField(JsonObject jsonAttributes, string fieldName, AESService.AESKey aesKey)
    {
        if (!jsonAttributes.ContainsKey(fieldName))
            return;

        var fieldValue = jsonAttributes[fieldName]?.ToString();
        if (string.IsNullOrEmpty(fieldValue))
            return;

        var dataToByteArray = Encoding.UTF8.GetBytes(fieldValue);
        var dataEncrypted = AESService.Encrypt(dataToByteArray, aesKey);
        var storedData = Convert.ToBase64String(dataEncrypted);

        jsonAttributes[fieldName] = storedData;
    }

    public JsonObject Decrypt(JsonObject protectedJsonAttributes)
    {
        if (!protectedJsonAttributes.ContainsKey("IV"))
            return protectedJsonAttributes;

        var ivString = protectedJsonAttributes["IV"]?.ToString();
        if (string.IsNullOrEmpty(ivString))
            return protectedJsonAttributes;

        try
        {
            var encodedIv = Convert.FromBase64String(ivString);
            var calAesKey = AESService.CalculateAESKey(_aesKeyString, encodedIv);
            var aesKey = new AESService.AESKey(calAesKey, encodedIv);

            var sensitiveFields = new[] { "AccountNumber", "RoutingNumber", "PaymentAccountNumber", "PaymentRoutingNumber" };

            foreach (var field in sensitiveFields)
            {
                DecryptField(protectedJsonAttributes, field, aesKey);
            }

            // Remove the IV after decryption
            protectedJsonAttributes.Remove("IV");
        }
        catch (Exception ex)
        {
            // Log the exception appropriately
            throw new InvalidOperationException("Failed to decrypt protected JSON attributes", ex);
        }

        return protectedJsonAttributes;
    }

    private static void DecryptField(JsonObject jsonAttributes, string fieldName, AESService.AESKey aesKey)
    {
        if (!jsonAttributes.ContainsKey(fieldName))
            return;

        var fieldValue = jsonAttributes[fieldName]?.ToString();
        if (string.IsNullOrEmpty(fieldValue))
            return;

        try
        {
            var encryptedData = Convert.FromBase64String(fieldValue);
            var decryptedData = AESService.Decrypt(encryptedData, aesKey);
            var decryptedString = Encoding.UTF8.GetString(decryptedData);
            
            jsonAttributes[fieldName] = decryptedString;
        }
        catch (Exception ex)
        {
            // Log decryption failure for this field
            throw new InvalidOperationException($"Failed to decrypt field {fieldName}", ex);
        }
    }
}