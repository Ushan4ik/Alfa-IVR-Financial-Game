package com.example.demo.daily

import org.springframework.security.core.Authentication
import org.springframework.web.bind.annotation.GetMapping
import org.springframework.web.bind.annotation.PostMapping
import org.springframework.web.bind.annotation.PathVariable
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.RestController

@RestController
@RequestMapping("/daily")
class DailyProgressController(private val service: DailyProgressService) {
    @GetMapping
    fun get(auth: Authentication) = service.get(auth.principal as String)

    // Точка интеграции для завершения ежедневного задания мини-игры.
    @PostMapping("/minigame-completed")
    fun miniGameCompleted(auth: Authentication) = service.recordMiniGame(auth.principal as String)

    @PostMapping("/streak-rewards/{days}/claim")
    fun claimStreakReward(auth: Authentication, @PathVariable days: Int) =
        service.claimStreakReward(auth.principal as String, days)

    @PostMapping("/coins/exchange")
    fun exchangeCoin(auth: Authentication) = service.exchangeCoin(auth.principal as String)
}
