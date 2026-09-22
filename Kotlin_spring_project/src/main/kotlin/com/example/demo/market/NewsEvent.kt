package com.example.demo.market

import jakarta.persistence.*
import java.time.Instant
import java.util.UUID

@Entity
@Table(name = "news_events")
data class NewsEvent(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val day: Int = 0,

    @Enumerated(EnumType.STRING)
    val sector: Sector = Sector.TECH,

    val headline: String = "",

    // от -2 (очень плохо для сектора) до +2 (очень хорошо)
    val sentiment: Int = 0,

    val createdAt: Instant = Instant.now()
)
