package com.example.demo.player

import jakarta.persistence.*
import java.time.Instant

@Entity
@Table(name = "player_progress")
data class PlayerProgress(
    @Id
    val playerId: String = "",

    val finLiteracyScore: Int = 0,
    val level: Int = 1,
    val lastUpdated: Instant = Instant.now()
)
