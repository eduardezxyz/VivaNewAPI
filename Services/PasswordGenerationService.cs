using NewVivaApi.Authentication.Models;
using NewVivaApi.Models;

public static class PasswordGenerationService
{
    public static string GeneratePassword(PasswordRequirements requirements)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var chars = "";
        var password = "";
        var random = new Random();

        // Build character set based on requirements
        if (requirements.RequireLowercase) chars += lowercase;
        if (requirements.RequireUppercase) chars += uppercase;
        if (requirements.RequireNumber) chars += numbers;
        if (requirements.RequireSymbol) chars += symbols;

        // Ensure at least one character from each required category
        if (requirements.RequireLowercase)
            password += lowercase[random.Next(lowercase.Length)];
        if (requirements.RequireUppercase)
            password += uppercase[random.Next(uppercase.Length)];
        if (requirements.RequireNumber)
            password += numbers[random.Next(numbers.Length)];
        if (requirements.RequireSymbol)
            password += symbols[random.Next(symbols.Length)];

        // Fill the rest randomly
        int targetLength = random.Next(requirements.MinimumLength, requirements.MaximumLength + 1);
        while (password.Length < targetLength)
        {
            password += chars[random.Next(chars.Length)];
        }

        // Shuffle the password
        return new string(password.ToCharArray().OrderBy(x => random.Next()).ToArray());
    }
}