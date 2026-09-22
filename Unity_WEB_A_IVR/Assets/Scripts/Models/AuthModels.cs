using System;

namespace IVR.Models
{
    [Serializable]
    public class RegisterRequest
    {
        public string email;
        public string password;
        public string nickname;
    }

    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public string token;
        public string playerId;
    }
}
