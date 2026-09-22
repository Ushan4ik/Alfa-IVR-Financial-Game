package com.example.demo.player

import com.example.demo.ml.MlClient
import org.springframework.stereotype.Service
import java.time.Instant

@Service
class PlayerService(
    private val playerRepository: PlayerRepository,
    private val progressRepository: PlayerProgressRepository,
    private val mlClient: MlClient
) {

    fun getProfile(playerId: String): PlayerProfileResponse {
        val player = playerRepository.findById(playerId).orElseThrow()
        val progress = progressRepository.findById(playerId)
            .orElseGet { PlayerProgress(playerId = playerId) }

        return PlayerProfileResponse(
            id = player.id,
            email = player.email,
            nickname = player.nickname,
            level = progress.level,
            finLiteracyScore = progress.finLiteracyScore,
            interests = player.interests.split(",").filter { it.isNotBlank() }
        )
    }

    fun setInterests(playerId: String, text: String) {
        val interests = mlClient.classifyInterests(text)
        val player = playerRepository.findById(playerId).orElseThrow()
        playerRepository.save(player.copy(interests = interests.joinToString(",")))
    }

    fun updateProgress(playerId: String, req: UpdateProgressRequest) {
        throw org.springframework.web.server.ResponseStatusException(
            org.springframework.http.HttpStatus.CONFLICT,
            "Очки рассчитываются сервером при завершении /learning/attempts/{id}/finish"
        )
    }
}
