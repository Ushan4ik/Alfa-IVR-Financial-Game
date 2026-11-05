package com.example.demo.daily

import com.example.demo.player.PlayerRepository
import org.springframework.beans.factory.annotation.Value
import org.springframework.http.HttpStatus
import org.springframework.stereotype.Service
import org.springframework.transaction.annotation.Transactional
import org.springframework.web.server.ResponseStatusException
import java.time.LocalDate
import java.time.ZoneId

@Service
class DailyProgressService(
    private val repository: DailyProgressRepository,
    private val rewardClaims: StreakRewardClaimRepository,
    private val players: PlayerRepository,
    @Value("\${app.daily-zone:Europe/Moscow}") dailyZone: String
) {
    private data class Reward(val days: Int, val coins: Int, val marketCash: Int)
    private val rewards = listOf(
        Reward(days = 7, coins = 4, marketCash = 2_000),
        Reward(days = 14, coins = 8, marketCash = 3_000)
    )
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

    @Transactional
    fun claimStreakReward(playerId: String, days: Int): DailyProgressResponse {
        val state = touch(playerId)
        val reward = rewards.firstOrNull { it.days == days }
            ?: throw ResponseStatusException(HttpStatus.NOT_FOUND, "Награда серии не найдена")
        if (state.currentStreak < reward.days)
            throw ResponseStatusException(HttpStatus.CONFLICT, "Награда ещё не открыта")

        val claimId = "$playerId:${reward.days}"
        if (rewardClaims.existsById(claimId))
            throw ResponseStatusException(HttpStatus.CONFLICT, "Награда уже получена")

        val player = players.lockById(playerId)
            ?: throw ResponseStatusException(HttpStatus.NOT_FOUND, "Игрок не найден")
        rewardClaims.save(StreakRewardClaim(
            id = claimId,
            playerId = playerId,
            milestoneDays = reward.days
        ))
        val updated = players.save(player.copy(
            coins = player.coins + reward.coins,
            cash = player.cash + reward.marketCash
        ))
        return response(state, updated)
    }

    @Transactional
    fun exchangeCoin(playerId: String): DailyProgressResponse {
        val state = touch(playerId)
        val player = players.lockById(playerId)
            ?: throw ResponseStatusException(HttpStatus.NOT_FOUND, "Игрок не найден")
        if (player.coins < 1)
            throw ResponseStatusException(HttpStatus.CONFLICT, "Недостаточно коинов")
        val updated = players.save(player.copy(coins = player.coins - 1, cash = player.cash + 1_000))
        return response(state, updated)
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

    private fun response(state: DailyProgress, suppliedPlayer: com.example.demo.player.Player? = null): DailyProgressResponse {
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
        val player = suppliedPlayer ?: players.findById(state.playerId).orElseThrow()
        val claimedDays = rewardClaims.findByPlayerId(state.playerId).map { it.milestoneDays }.toSet()
        return DailyProgressResponse(
            date = state.activityDate.toString(),
            currentStreak = state.currentStreak,
            bestStreak = state.bestStreak,
            completedCount = quests.count { it.completed },
            totalCount = quests.size,
            quests = quests,
            coins = player.coins,
            marketCash = player.cash,
            streakRewards = rewards.map { reward ->
                StreakRewardResponse(
                    days = reward.days,
                    coins = reward.coins,
                    marketCash = reward.marketCash,
                    unlocked = state.currentStreak >= reward.days,
                    claimed = reward.days in claimedDays
                )
            }
        )
    }
}
