package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface MarketOrderRepository : JpaRepository<MarketOrder, String> {
    fun findByPlayerIdOrderByCreatedAtDesc(playerId: String): List<MarketOrder>
}
