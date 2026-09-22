package com.example.demo.market

import jakarta.persistence.Entity
import jakarta.persistence.Id
import jakarta.persistence.Table
import java.time.Instant

// единственная строка-синглтон с глобальными часами рынка
@Entity
@Table(name = "market_state")
data class MarketState(
    @Id
    val id: String = SINGLETON_ID,

    var currentDay: Int = 0,

    // интервал реального времени между рыночными днями, меняется из админ-панели
    var tickIntervalMinutes: Int = 1440,

    var lastTickAt: Instant = Instant.now()
) {
    companion object {
        const val SINGLETON_ID = "singleton"
    }
}
