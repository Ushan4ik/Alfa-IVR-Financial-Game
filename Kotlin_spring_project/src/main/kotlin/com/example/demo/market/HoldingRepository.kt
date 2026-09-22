package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface HoldingRepository : JpaRepository<Holding, String> {
    fun findByPlayerId(playerId: String): List<Holding>
    fun findByPlayerIdAndStockId(playerId: String, stockId: String): Holding?
}
