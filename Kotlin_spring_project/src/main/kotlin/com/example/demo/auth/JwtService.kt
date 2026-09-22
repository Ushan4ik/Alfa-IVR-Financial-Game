package com.example.demo.auth

import io.jsonwebtoken.Jwts
import io.jsonwebtoken.security.Keys
import org.springframework.beans.factory.annotation.Value
import org.springframework.stereotype.Service
import java.util.Date

@Service
class JwtService {

    @Value("\${jwt.secret}")
    private lateinit var secret: String

    private val expirationMs = 7 * 24 * 60 * 60 * 1000L // 7 дней

    fun generateToken(playerId: String): String =
        Jwts.builder()
            .subject(playerId)
            .issuedAt(Date())
            .expiration(Date(System.currentTimeMillis() + expirationMs))
            .signWith(Keys.hmacShaKeyFor(secret.toByteArray()))
            .compact()

    fun extractPlayerId(token: String): String =
        Jwts.parser()
            .verifyWith(Keys.hmacShaKeyFor(secret.toByteArray()))
            .build()
            .parseSignedClaims(token)
            .payload.subject
}
