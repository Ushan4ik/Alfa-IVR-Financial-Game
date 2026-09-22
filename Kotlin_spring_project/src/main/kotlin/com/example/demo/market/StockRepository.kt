package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface StockRepository : JpaRepository<Stock, String> {
    fun findBySector(sector: Sector): List<Stock>
}
