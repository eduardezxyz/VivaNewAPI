using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Services
{
    public class Token
    {
        public string Identity { get; set; }
        public string Value { get; set; }
        public DateTime Expiration { get; set; }
    }

    public class TokenGenerator
    {
        public static async Task<Token> GetToken<TUser>(string userId, UserManager<TUser> userManager) 
            where TUser : class
        {
            Token generatedToken = new Token();
            string resetToken;
            byte[] complete, token;
            int ivsize;

            generatedToken.Identity = Guid.NewGuid().ToString();
            AESService.AESKey aesKey = GetKey(generatedToken.Identity);
            ivsize = aesKey.IV.Length;

            var user = await userManager.FindByIdAsync(userId);
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            token = AESService.Encrypt(Encoding.Default.GetBytes(resetToken), aesKey);
            complete = GetTotalBytes(token, aesKey.IV);

            generatedToken.Value = Convert.ToBase64String(complete);
            generatedToken.Expiration = DateTime.Now.AddMinutes(15);

            return generatedToken;
        }

        public static string ExtractToken(Token token)
        {
            byte[] data = Convert.FromBase64String(token.Value);
            byte[] IV = GetIV(ref data);

            AESService.AESKey aesKey = GetKey(token.Identity, IV);

            byte[] decryptedToken = AESService.Decrypt(data, aesKey);
            return Encoding.Default.GetString(decryptedToken);
        }

        private static AESService.AESKey GetKey(string identity = null, byte[] iv = null)
        {
            byte[] key, IV;

            if (identity == null && iv == null)
            {
                string ident = Guid.NewGuid().ToString();
                key = AESService.CreateAESKey(ident, out IV);
            }
            else if (iv == null)
            {
                key = AESService.CreateAESKey(identity, out IV);
            }
            else
            {
                IV = iv;
                key = AESService.CalculateAESKey(identity, iv);
            }

            return new AESService.AESKey(key, IV);
        }

        private static byte[] GetIV(ref byte[] data)
        {
            int sizePoint, ivPoint, size;
            sizePoint = data.Length - sizeof(int);

            size = BitConverter.ToInt32(data, sizePoint);
            Array.Resize(ref data, sizePoint);
            ivPoint = data.Length - size;

            byte[] IV = new byte[size];
            Buffer.BlockCopy(data, ivPoint, IV, 0, size);
            Array.Resize(ref data, ivPoint);

            return IV;
        }

        private static byte[] GetTotalBytes(byte[] token, byte[] iv)
        {
            int length = token.Length;
            int size = iv.Length;

            byte[] total = new byte[length + size + sizeof(int)];
            token.CopyTo(total, 0);
            iv.CopyTo(total, length);
            BitConverter.GetBytes(size).CopyTo(total, length + size);

            return total;
        }
    }
}