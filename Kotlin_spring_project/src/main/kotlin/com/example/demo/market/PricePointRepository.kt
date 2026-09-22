package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface PricePointRepository : JpaRepository<PricePoint, String> {
    fun findByStockIdOrderByDayAsc(stockId: String): List<PricePoint>
}
