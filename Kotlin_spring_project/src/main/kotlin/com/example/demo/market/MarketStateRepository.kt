package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface MarketStateRepository : JpaRepository<MarketState, String>
