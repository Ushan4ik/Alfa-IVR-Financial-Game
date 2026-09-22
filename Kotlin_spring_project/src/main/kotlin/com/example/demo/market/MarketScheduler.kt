package com.example.demo.market

import jakarta.annotation.PostConstruct
import org.springframework.scheduling.annotation.Scheduled
import org.springframework.stereotype.Component

@Component
class MarketScheduler(private val marketService: MarketService) {

    @PostConstruct
    fun init() {
        marketService.seedIfEmpty()
    }

    // раз в минуту проверяем не настало ли время нового рыночного дня
    @Scheduled(fixedRate = 60_000)
    fun checkTick() {
        marketService.maybeTick()
    }
}
