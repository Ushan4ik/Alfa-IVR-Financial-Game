package com.example.demo.market

import jakarta.persistence.*
import java.time.Instant
import java.util.UUID

enum class OrderType { BUY, SELL, SHORT, COVER }

// журнал сделок — нужен для истории операций игрока
@Entity
@Table(name = "market_orders")
data class MarketOrder(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val playerId: String = "",
    val stockId: String = "",

    @Enumerated(EnumType.STRING)
    val type: OrderType = OrderType.BUY,

    val quantity: Double = 0.0,
    val price: Double = 0.0,
    val day: Int = 0,
    val createdAt: Instant = Instant.now()
)
