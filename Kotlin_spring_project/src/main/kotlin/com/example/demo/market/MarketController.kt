package com.example.demo.market

import org.springframework.security.core.Authentication
import org.springframework.http.HttpStatus
import org.springframework.web.bind.annotation.*
import org.springframework.web.server.ResponseStatusException

@RestController
@RequestMapping("/market")
class MarketController(private val marketService: MarketService) {

    @GetMapping("/state")
    fun getState(): MarketStateResponse = marketService.getOrCreateState().toResponse()

    @GetMapping("/stocks")
    fun getStocks(): List<StockResponse> = marketService.getStocksView()

    @GetMapping("/stocks/{id}/history")
    fun getHistory(@PathVariable id: String): List<PricePointResponse> = marketService.getHistory(id)

    @GetMapping("/news")
    fun getNews(): List<NewsResponse> = marketService.getRecentNews()

    @GetMapping("/leaderboard")
    fun getLeaderboard(): List<LeaderboardEntryResponse> = marketService.getLeaderboard()

    @GetMapping("/portfolio")
    fun getPortfolio(authentication: Authentication): PortfolioResponse =
        marketService.getPortfolio(authentication.principal as String)

    @PostMapping("/order")
    fun placeOrder(@RequestBody req: OrderRequest, authentication: Authentication): PortfolioResponse =
        try {
            marketService.placeOrder(authentication.principal as String, req)
        } catch (error: IllegalArgumentException) {
            throw ResponseStatusException(HttpStatus.BAD_REQUEST, error.message, error)
        }
}
