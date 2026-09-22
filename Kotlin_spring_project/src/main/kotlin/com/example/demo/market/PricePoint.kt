package com.example.demo.market

import jakarta.persistence.*
import java.util.UUID

// одна точка истории цены — для графика в Unity
@Entity
@Table(name = "price_points")
data class PricePoint(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val stockId: String = "",
    val day: Int = 0,
    val price: Double = 0.0
)
