package com.example.demo.auth

data class RegisterRequest(
    val email: String,
    val password: String,
    val nickname: String? = null
)

data class LoginRequest(
    val email: String,
    val password: String
)

data class AuthResponse(
    val token: String,
    val playerId: String
)
