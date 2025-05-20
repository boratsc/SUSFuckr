namespace SUSFuckr
{
    public static class SecretProvider
    {
        public static string GetDownloadToken()
        {
            // Przyk³ad: token zakodowany base64
            return Decrypt("ZTRhMWM3YjJmM2Q4ZTlhMGI1YzZkN2U4ZjlhMWIyYzNkNGU1ZjZhN2I4YzlkMGUxZjJhM2I0YzVkNmU3ZjhhOQ=="); // <-- tu wstaw swój token w base64
        }
        public static string Get7zPassword()
        {
            return Decrypt("VDl2I0t3MlhwIThnRGVAelJxTTReSHU3Qkx5JW9BY0o="); // <-- tu wstaw swoje has³o w base64
        }
        private static string Decrypt(string encrypted)
        {
            // Prosty przyk³ad: base64 decode
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
        }
    }
}