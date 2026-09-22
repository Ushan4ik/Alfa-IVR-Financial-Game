package com.example.demo.auth

import com.example.demo.player.Player
import com.example.demo.player.PlayerRepository
import org.springframework.http.HttpStatus
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder
import org.springframework.web.bind.annotation.*
import org.springframework.web.server.ResponseStatusException

@RestController
@RequestMapping("/auth")
class AuthController(
    private val playerRepository: PlayerRepository,
    private val jwtService: JwtService,
    private val passwordEncoder: BCryptPasswordEncoder
) {

    @PostMapping("/register")
    fun register(@RequestBody req: RegisterRequest): AuthResponse {
        if (playerRepository.findByEmail(req.email) != null)
            throw ResponseStatusException(HttpStatus.CONFLICT, "Email уже занят")

        val hash = passwordEncoder.encode(req.password) ?: ""
        val nick = if (req.nickname.isNullOrBlank()) req.email.substringBefore("@") else req.nickname

        val player = playerRepository.save(
            Player(
                email = req.email,
                passwordHash = hash,
                nickname = nick
            )
        )
        return AuthResponse(token = jwtService.generateToken(player.id), playerId = player.id)
    }

    @PostMapping("/login")
    fun login(@RequestBody req: LoginRequest): AuthResponse {
        val player = playerRepository.findByEmail(req.email)
            ?: throw ResponseStatusException(HttpStatus.UNAUTHORIZED, "Неверный email или пароль")

        if (!passwordEncoder.matches(req.password, player.passwordHash))
            throw ResponseStatusException(HttpStatus.UNAUTHORIZED, "Неверный email или пароль")

        return AuthResponse(token = jwtService.generateToken(player.id), playerId = player.id)
    }
}
