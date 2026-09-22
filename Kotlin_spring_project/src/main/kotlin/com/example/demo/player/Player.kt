package com.example.demo.player

import jakarta.persistence.*
import java.time.Instant
import java.util.UUID

enum class PlayerRole { PLAYER, ADMIN }

@Entity
@Table(name = "players")
data class Player(
    @Id
    val id: String = UUID.randomUUID().toString(),

    @Column(unique = true, nullable = false)
    val email: String = "",

    @Column(nullable = false)
    val passwordHash: String = "",

    val nickname: String = "",

    // comma-separated: "игры,учёба" — ставится один раз при онбординге через ML
    val interests: String = "",

    // стартовый виртуальный капитал для биржевой мини-игры
    val cash: Double = 10_000.0,

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    val role: PlayerRole = PlayerRole.PLAYER,

    val createdAt: Instant = Instant.now()
)
