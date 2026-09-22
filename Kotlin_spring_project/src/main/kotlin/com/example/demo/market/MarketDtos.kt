package com.example.demo.market

import java.time.Instant

data class StockResponse(
    val id: String,
    val ticker: String,
    val name: String,
    val sector: String,
    val sectorName: String,
    val price: Double,
    val changePercent: Double
)

fun Stock.toResponse() = StockResponse(
    id = id,
    ticker = ticker,
    name = name,
    sector = sector.name,
    sectorName = sector.displayName,
    price = currentPrice,
    changePercent = if (previousPrice != 0.0) (currentPrice - previousPrice) / previousPrice * 100 else 0.0
)

data class PricePointResponse(val day: Int, val price: Double)

fun PricePoint.toResponse() = PricePointResponse(day = day, price = price)

data class NewsResponse(
    val id: String,
    val day: Int,
    val sector: String,
    val sectorName: String,
    val headline: String,
    val sentiment: Int,
    val createdAt: Instant
)

fun NewsEvent.toResponse() = NewsResponse(
    id = id,
    day = day,
    sector = sector.name,
    sectorName = sector.displayName,
    headline = headline,
    sentiment = sentiment,
    createdAt = createdAt
)

data class HoldingResponse(
    val stockId: String,
    val ticker: String,
    val name: String,
    val quantity: Double,
    val avgPrice: Double,
    val currentPrice: Double,
    val unrealizedPnl: Double
)

data class PortfolioResponse(
    val cash: Double,
    val holdingsValue: Double,
    val totalValue: Double,
    val holdings: List<HoldingResponse>
)

data class OrderRequest(
    val stockId: String,
    val type: OrderType,
    val quantity: Double
)

data class MarketStateResponse(
    val currentDay: Int,
    val tickIntervalMinutes: Int,
    val lastTickAt: Instant
)

fun MarketState.toResponse() = MarketStateResponse(
    currentDay = currentDay,
    tickIntervalMinutes = tickIntervalMinutes,
    lastTickAt = lastTickAt
)

data class SetIntervalRequest(val minutes: Int)
data class SkipDaysRequest(val days: Int)

data class LeaderboardEntryResponse(
    val rank: Int,
    val playerId: String,
    val nickname: String,
    val cash: Double,
    val holdingsValue: Double,
    val totalValue: Double
)
