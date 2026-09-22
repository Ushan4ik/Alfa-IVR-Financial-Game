package com.example.demo.market

import org.springframework.web.bind.annotation.*

@RestController
@RequestMapping("/admin/market")
class AdminMarketController(private val marketService: MarketService) {

    @GetMapping("/state")
    fun getState(): MarketStateResponse = marketService.getOrCreateState().toResponse()

    @PutMapping("/interval")
    fun setInterval(@RequestBody req: SetIntervalRequest): MarketStateResponse =
        marketService.setInterval(req.minutes)

    @PostMapping("/skip")
    fun skip(@RequestBody req: SkipDaysRequest): MarketStateResponse =
        marketService.skipDays(req.days)
}
