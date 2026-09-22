package com.example.demo.market

import jakarta.persistence.*
import java.util.UUID

// позиция игрока по одной акции. quantity > 0 — лонг, quantity < 0 — шорт
@Entity
@Table(
    name = "holdings",
    uniqueConstraints = [UniqueConstraint(columnNames = ["playerId", "stockId"])]
)
data class Holding(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val playerId: String = "",
    val stockId: String = "",

    var quantity: Double = 0.0,
    var avgPrice: Double = 0.0
)
