package com.example.demo.player

import org.springframework.security.core.Authentication
import org.springframework.web.bind.annotation.*

@RestController
@RequestMapping("/player")
class PlayerController(private val playerService: PlayerService) {

    @GetMapping("/profile")
    fun getProfile(auth: Authentication): PlayerProfileResponse =
        playerService.getProfile(auth.principal as String)

    @PostMapping("/interests")
    fun setInterests(@RequestBody req: SetInterestsRequest, auth: Authentication) =
        playerService.setInterests(auth.principal as String, req.text)

    @PutMapping("/progress")
    fun updateProgress(@RequestBody req: UpdateProgressRequest, auth: Authentication) =
        playerService.updateProgress(auth.principal as String, req)
}
