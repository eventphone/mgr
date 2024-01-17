using System;

namespace epmgr.Services
{
    public class RandomPasswordService
    {
        private static readonly string _digits = "0123456789";
        private static readonly string[] RandomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            "abcdefghijkmnopqrstuvwxyz",    // lowercase
            _digits,                   // digits
            "!@$?_-+/*%$"                   // non-alphanumeric
        };

        private readonly Random _random;

        public RandomPasswordService()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var seed = BitConverter.ToInt32(bytes, 0);
                _random = new Random(seed);
            }
        }

        public virtual string GenerateSipPassword()
        {
            var result = new char[20];
            for (int i = 0; i < result.Length; i++)
            {
                var type = _random.Next(4);
                var alphabet = RandomChars[type];
                result[i] = alphabet[_random.Next(alphabet.Length)];
            }
            return new string(result);
        }

        public string GenerateDectPin()
        {
            return _random.Next().ToString();
        }

        public string GenerateDesasterPin()
        {
            var result = new char[6];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = _digits[_random.Next(_digits.Length)];
            }
            return new string(result);
        }
    }
}