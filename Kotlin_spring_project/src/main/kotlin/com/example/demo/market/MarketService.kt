package com.example.demo.market

import com.example.demo.player.PlayerRepository
import org.springframework.stereotype.Service
import org.springframework.transaction.annotation.Transactional
import java.time.Duration
import java.time.Instant

@Service
class MarketService(
    private val stockRepository: StockRepository,
    private val pricePointRepository: PricePointRepository,
    private val newsEventRepository: NewsEventRepository,
    private val holdingRepository: HoldingRepository,
    private val marketOrderRepository: MarketOrderRepository,
    private val marketStateRepository: MarketStateRepository,
    private val playerRepository: PlayerRepository
) {

    // сколько % от цены сдвигает суммарное давление игроков за один тик, и потолок этого сдвига
    private val pressureWeight = 0.0008
    private val maxPressureImpact = 0.15

    // насколько % сдвигает одна новость определённой силы (sentiment 1 == ~4%)
    private val newsImpactPerSentiment = 0.04

    fun getOrCreateState(): MarketState =
        marketStateRepository.findById(MarketState.SINGLETON_ID).orElseGet { marketStateRepository.save(MarketState()) }

    fun seedIfEmpty() {
        if (stockRepository.count() > 0L) return
        val seedStocks = listOf(
            Stock(ticker = "GOLD", name = "ГлобалГорно", sector = Sector.MINING, basePrice = 120.0, currentPrice = 120.0, previousPrice = 120.0, volatility = 0.03),
            Stock(ticker = "TECHX", name = "ТехноКрафт", sector = Sector.TECH, basePrice = 250.0, currentPrice = 250.0, previousPrice = 250.0, volatility = 0.05),
            Stock(ticker = "OILCO", name = "ЭнергоПром", sector = Sector.ENERGY, basePrice = 80.0, currentPrice = 80.0, previousPrice = 80.0, volatility = 0.04),
            Stock(ticker = "BANK1", name = "НацБанк", sector = Sector.FINANCE, basePrice = 60.0, currentPrice = 60.0, previousPrice = 60.0, volatility = 0.02),
            Stock(ticker = "SHOP1", name = "МаркетПлюс", sector = Sector.RETAIL, basePrice = 45.0, currentPrice = 45.0, previousPrice = 45.0, volatility = 0.025),
            Stock(ticker = "AGRO1", name = "ЗерноХолд", sector = Sector.AGRICULTURE, basePrice = 30.0, currentPrice = 30.0, previousPrice = 30.0, volatility = 0.035)
        )
        stockRepository.saveAll(seedStocks)
    }

    // вызывается планировщиком каждую минуту — сам решает пора ли наступить новому дню
    fun maybeTick() {
        val state = getOrCreateState()
        val elapsedMinutes = Duration.between(state.lastTickAt, Instant.now()).toMinutes()
        if (elapsedMinutes >= state.tickIntervalMinutes) tick()
    }

    // принудительно пропустить N дней разом (для админки/демонстрации)
    fun skipDays(days: Int): MarketStateResponse {
        var result = getOrCreateState().toResponse()
        repeat(days.coerceAtLeast(0)) { result = tick() }
        return result
    }

    fun setInterval(minutes: Int): MarketStateResponse {
        val state = getOrCreateState()
        state.tickIntervalMinutes = minutes.coerceAtLeast(1)
        marketStateRepository.save(state)
        return state.toResponse()
    }

    @Transactional
    fun tick(): MarketStateResponse {
        val state = getOrCreateState()
        state.currentDay += 1

        val newsCount = (1..3).random()
        val todaysNews = NewsTemplates.pool.shuffled().take(newsCount).map {
            newsEventRepository.save(
                NewsEvent(day = state.currentDay, sector = it.sector, headline = it.headline, sentiment = it.sentiment)
            )
        }

        val stocks = stockRepository.findAll()
        for (stock in stocks) {
            val noise = (Math.random() - 0.5) * 2 * stock.volatility
            val newsImpact = todaysNews.filter { it.sector == stock.sector }.sumOf { it.sentiment * newsImpactPerSentiment }
            val pressureImpact = (stock.pendingPressure * pressureWeight).coerceIn(-maxPressureImpact, maxPressureImpact)

            val newPrice = (stock.currentPrice * (1 + noise + newsImpact + pressureImpact)).coerceAtLeast(0.5)

            pricePointRepository.save(PricePoint(stockId = stock.id, day = state.currentDay, price = newPrice))

            stock.previousPrice = stock.currentPrice
            stock.currentPrice = newPrice
            stock.pendingPressure = 0.0
        }
        stockRepository.saveAll(stocks)

        state.lastTickAt = Instant.now()
        marketStateRepository.save(state)

        return state.toResponse()
    }

    fun getLeaderboard(): List<LeaderboardEntryResponse> {
        val players = playerRepository.findAll()
        val allHoldings = holdingRepository.findAll().filter { it.quantity != 0.0 }.groupBy { it.playerId }
        val stocks = stockRepository.findAll().associateBy { it.id }

        return players.map { player ->
            val holdings = allHoldings[player.id] ?: emptyList()
            val holdingsValue = holdings.sumOf { h -> (stocks[h.stockId]?.currentPrice ?: 0.0) * h.quantity }
            LeaderboardEntryResponse(
                rank = 0,
                playerId = player.id,
                nickname = player.nickname,
                cash = player.cash,
                holdingsValue = holdingsValue,
                totalValue = player.cash + holdingsValue
            )
        }.sortedByDescending { it.totalValue }
         .mapIndexed { i, e -> e.copy(rank = i + 1) }
    }

    fun getStocksView(): List<StockResponse> = stockRepository.findAll().map { it.toResponse() }

    fun getHistory(stockId: String): List<PricePointResponse> =
        pricePointRepository.findByStockIdOrderByDayAsc(stockId).map { it.toResponse() }

    fun getRecentNews(): List<NewsResponse> =
        newsEventRepository.findTop20ByOrderByCreatedAtDesc().map { it.toResponse() }

    fun getPortfolio(playerId: String): PortfolioResponse {
        val player = playerRepository.findById(playerId).orElseThrow()
        val holdings = holdingRepository.findByPlayerId(playerId).filter { it.quantity != 0.0 }
        val stocksById = stockRepository.findAllById(holdings.map { it.stockId }).associateBy { it.id }

        val holdingResponses = holdings.mapNotNull { h ->
            val stock = stocksById[h.stockId] ?: return@mapNotNull null
            HoldingResponse(
                stockId = stock.id,
                ticker = stock.ticker,
                name = stock.name,
                quantity = h.quantity,
                avgPrice = h.avgPrice,
                currentPrice = stock.currentPrice,
                unrealizedPnl = (stock.currentPrice - h.avgPrice) * h.quantity
            )
        }

        val holdingsValue = holdingResponses.sumOf { it.quantity * it.currentPrice }
        return PortfolioResponse(
            cash = player.cash,
            holdingsValue = holdingsValue,
            totalValue = player.cash + holdingsValue,
            holdings = holdingResponses
        )
    }

    @Transactional
    fun placeOrder(playerId: String, req: OrderRequest): PortfolioResponse {
        require(req.quantity > 0) { "Количество должно быть положительным" }

        val stock = stockRepository.findById(req.stockId).orElseThrow { IllegalArgumentException("Акция не найдена") }
        val player = playerRepository.findById(playerId).orElseThrow()
        val holding = holdingRepository.findByPlayerIdAndStockId(playerId, req.stockId)
            ?: Holding(playerId = playerId, stockId = req.stockId)

        val price = stock.currentPrice
        val qty = req.quantity

        when (req.type) {
            OrderType.BUY -> {
                require(holding.quantity >= 0) { "Сначала закрой шорт-позицию (COVER)" }
                val cost = qty * price
                require(player.cash >= cost) { "Недостаточно денег" }

                val newQty = holding.quantity + qty
                holding.avgPrice = if (holding.quantity == 0.0) price
                    else (holding.avgPrice * holding.quantity + price * qty) / newQty
                holding.quantity = newQty

                playerRepository.save(player.copy(cash = player.cash - cost))
                stock.pendingPressure += qty
            }

            OrderType.SELL -> {
                require(holding.quantity > 0) { "Нет акций в портфеле" }
                require(holding.quantity >= qty) { "Недостаточно акций для продажи" }

                holding.quantity -= qty
                playerRepository.save(player.copy(cash = player.cash + qty * price))
                stock.pendingPressure -= qty
            }

            OrderType.SHORT -> {
                require(holding.quantity <= 0) { "Сначала продай длинную позицию" }

                val absQty = -holding.quantity
                val newAbsQty = absQty + qty
                holding.avgPrice = if (absQty == 0.0) price
                    else (holding.avgPrice * absQty + price * qty) / newAbsQty
                holding.quantity -= qty

                playerRepository.save(player.copy(cash = player.cash + qty * price))
                stock.pendingPressure -= qty
            }

            OrderType.COVER -> {
                require(holding.quantity < 0) { "Нет открытой шорт-позиции" }
                require(-holding.quantity >= qty) { "Нельзя закрыть больше чем открыто" }

                val cost = qty * price
                require(player.cash >= cost) { "Недостаточно денег чтобы закрыть шорт" }

                holding.quantity += qty
                playerRepository.save(player.copy(cash = player.cash - cost))
                stock.pendingPressure += qty
            }
        }

        holdingRepository.save(holding)
        stockRepository.save(stock)
        marketOrderRepository.save(
            MarketOrder(playerId = playerId, stockId = req.stockId, type = req.type, quantity = qty, price = price, day = getOrCreateState().currentDay)
        )

        return getPortfolio(playerId)
    }
}
