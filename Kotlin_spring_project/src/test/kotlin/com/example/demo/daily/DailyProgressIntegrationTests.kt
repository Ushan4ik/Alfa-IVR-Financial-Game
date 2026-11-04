package com.example.demo.daily

import com.example.demo.player.Player
import com.example.demo.player.PlayerRepository
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.test.context.ActiveProfiles
import java.time.LocalDate
import java.time.ZoneId
import java.util.UUID

@ActiveProfiles("dev")
@SpringBootTest(properties = [
    "spring.datasource.url=jdbc:h2:mem:dailytests;DB_CLOSE_DELAY=-1;NON_KEYWORDS=DAY",
    "learning.gemini.key=",
    "learning.generation-delay-ms=3600000"
])
class DailyProgressIntegrationTests {
    @Autowired lateinit var service: DailyProgressService
    @Autowired lateinit var repository: DailyProgressRepository
    @Autowired lateinit var players: PlayerRepository

    private fun player() = players.save(
        Player(email = "${UUID.randomUUID()}@test.invalid", passwordHash = "unused")
    ).id

    @Test
    fun `daily streak advances once per day and quests reset`() {
        val playerId = player()
        val first = service.get(playerId)
        assertEquals(1, first.currentStreak)
        assertEquals(0, first.completedCount)
        assertEquals(2, first.totalCount)
        assertTrue(first.quests.single { it.id == "PLAY_MINIGAME" }.available)

        service.recordTest(playerId)
        service.recordTest(playerId)
        assertEquals(1, repository.findById(playerId).orElseThrow().testsCompleted)

        service.recordMiniGame(playerId)
        service.recordMiniGame(playerId)
        assertEquals(1, repository.findById(playerId).orElseThrow().miniGamesPlayed)

        val today = LocalDate.now(ZoneId.of("Europe/Moscow"))
        repository.save(DailyProgress(
            playerId = playerId,
            activityDate = today.minusDays(1),
            currentStreak = 2,
            bestStreak = 4,
            testsCompleted = 1,
            miniGamesPlayed = 1
        ))

        val nextDay = service.get(playerId)
        assertEquals(3, nextDay.currentStreak)
        assertEquals(4, nextDay.bestStreak)
        assertEquals(0, nextDay.completedCount)
        assertTrue(nextDay.quests.none { it.completed })

        repository.save(repository.findById(playerId).orElseThrow().copy(
            activityDate = today.minusDays(3),
            currentStreak = 3
        ))
        assertEquals(1, service.get(playerId).currentStreak)
    }
}
