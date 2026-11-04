package com.example.demo.daily

import org.springframework.security.core.Authentication
import org.springframework.web.bind.annotation.GetMapping
import org.springframework.web.bind.annotation.PostMapping
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.RestController

@RestController
@RequestMapping("/daily")
class DailyProgressController(private val service: DailyProgressService) {
    @GetMapping
    fun get(auth: Authentication) = service.get(auth.principal as String)

    // Точка интеграции для будущей мини-игры.
    @PostMapping("/minigame-completed")
    fun miniGameCompleted(auth: Authentication) = service.recordMiniGame(auth.principal as String)
}
