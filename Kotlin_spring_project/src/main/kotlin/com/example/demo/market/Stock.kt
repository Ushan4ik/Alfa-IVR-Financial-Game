package com.example.demo.market

import jakarta.persistence.*
import java.util.UUID

@Entity
@Table(name = "stocks")
data class Stock(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val ticker: String = "",
    val name: String = "",

    @Enumerated(EnumType.STRING)
    val sector: Sector = Sector.TECH,

    val basePrice: Double = 100.0,
    var currentPrice: Double = 100.0,

    // цена на начало текущего рыночного дня — нужна чтобы показать % изменения за день
    var previousPrice: Double = 100.0,

    // базовая волатильность (доля от цены), задаёт случайный разброс шума за один тик
    val volatility: Double = 0.03,

    // суммарный объём покупок(+) / продаж и шортов(-) игроков с прошлого тика, сбрасывается каждый тик
    var pendingPressure: Double = 0.0
)
