package com.example.demo.daily

import com.example.demo.player.PlayerRepository
import org.springframework.beans.factory.annotation.Value
import org.springframework.stereotype.Service
import org.springframework.transaction.annotation.Transactional
import java.time.LocalDate
import java.time.ZoneId

@Service
class DailyProgressService(
    private val repository: DailyProgressRepository,
    private val players: PlayerRepository,
    @Value("\${app.daily-zone:Europe/Moscow}") dailyZone: String
) {
    private val zoneId = ZoneId.of(dailyZone)

    @Transactional
    fun get(playerId: String): DailyProgressResponse = response(touch(playerId))

    @Transactional
    fun recordTest(playerId: String): DailyProgressResponse {
        val state = touch(playerId)
        return response(repository.save(state.copy(testsCompleted = 1)))
    }

    // Вызов идемпотентно отмечает ежедневное задание после первого игрового действия.
    @Transactional
    fun recordMiniGame(playerId: String): DailyProgressResponse {
        val state = touch(playerId)
        return response(repository.save(state.copy(miniGamesPlayed = 1)))
    }

    private fun touch(playerId: String): DailyProgress {
        players.lockById(playerId) ?: error("Player not found")
        val today = LocalDate.now(zoneId)
        val stored = repository.findById(playerId).orElse(null)
            ?: return repository.save(DailyProgress(
                playerId = playerId,
                activityDate = today,
                currentStreak = 1,
                bestStreak = 1
            ))

        if (stored.activityDate == today) return stored

        val nextStreak = if (stored.activityDate == today.minusDays(1)) stored.currentStreak + 1 else 1
        return repository.save(stored.copy(
            activityDate = today,
            currentStreak = nextStreak,
            bestStreak = maxOf(stored.bestStreak, nextStreak),
            testsCompleted = 0,
            miniGamesPlayed = 0
        ))
    }

    private fun response(state: DailyProgress): DailyProgressResponse {
        val quests = listOf(
            DailyQuestResponse(
                id = "COMPLETE_TEST",
                title = "Пройди 1 тест",
                progress = state.testsCompleted,
                target = 1,
                completed = state.testsCompleted >= 1,
                available = true
            ),
            DailyQuestResponse(
                id = "PLAY_MINIGAME",
                title = "Сыграй 1 раз в мини-игру",
                progress = state.miniGamesPlayed,
                target = 1,
                completed = state.miniGamesPlayed >= 1,
                available = true
            )
        )
        return DailyProgressResponse(
            date = state.activityDate.toString(),
            currentStreak = state.currentStreak,
            bestStreak = state.bestStreak,
            completedCount = quests.count { it.completed },
            totalCount = quests.size,
            quests = quests
        )
    }
}
