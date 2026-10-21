package com.example.demo.market

import com.example.demo.player.Player
import com.example.demo.player.PlayerRepository
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Test
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.test.context.ActiveProfiles
import java.util.UUID
import java.util.concurrent.Callable
import java.util.concurrent.Executors

@ActiveProfiles("dev")
@SpringBootTest(properties = [
    "spring.datasource.url=jdbc:h2:mem:markettests;DB_CLOSE_DELAY=-1;NON_KEYWORDS=DAY",
    "learning.gemini.key=", "learning.gemini.model=", "learning.generation-delay-ms=3600000"
])
class MarketIntegrationTests {
    @Autowired lateinit var market: MarketService
    @Autowired lateinit var players: PlayerRepository
    @Autowired lateinit var stocks: StockRepository
    @Autowired lateinit var holdings: HoldingRepository
    @Autowired lateinit var orders: MarketOrderRepository

    @Test
    fun `simultaneous first purchases are serialized without server error`() {
        market.seedIfEmpty()
        val player = players.save(Player(
            email = "${UUID.randomUUID()}@test.invalid",
            passwordHash = "unused"
        ))
        val stock = stocks.findAll().first()
        val initialCash = player.cash
        val pool = Executors.newFixedThreadPool(2)

        try {
            val results = pool.invokeAll(List(2) {
                Callable { market.placeOrder(player.id, OrderRequest(stock.id, OrderType.BUY, 1.0)) }
            }).map { it.get() }

            assertEquals(2, results.size)
            assertEquals(2.0, holdings.findByPlayerIdAndStockId(player.id, stock.id)?.quantity)
            assertEquals(initialCash - stock.currentPrice * 2, players.findById(player.id).orElseThrow().cash, 0.001)
            assertEquals(2, orders.findByPlayerIdOrderByCreatedAtDesc(player.id).size)
        } finally {
            pool.shutdownNow()
        }
    }
}
