package com.example.demo.player

import com.example.demo.daily.DailyProgress
import com.example.demo.daily.DailyProgressRepository
import com.example.demo.daily.StreakRewardClaim
import com.example.demo.daily.StreakRewardClaimRepository
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.test.context.ActiveProfiles
import java.time.LocalDate
import java.util.UUID

@ActiveProfiles("dev")
@SpringBootTest(properties = [
    "spring.datasource.url=jdbc:h2:mem:adminplayertests;DB_CLOSE_DELAY=-1;NON_KEYWORDS=DAY",
    "learning.gemini.key=",
    "learning.generation-delay-ms=3600000"
])
class AdminPlayerControllerIntegrationTests {
    @Autowired lateinit var controller: AdminPlayerController
    @Autowired lateinit var players: PlayerRepository
    @Autowired lateinit var daily: DailyProgressRepository
    @Autowired lateinit var claims: StreakRewardClaimRepository

    @Test
    fun `admin can inspect edit and advance player state`() {
        val player = players.save(Player(
            email = "${UUID.randomUUID()}@test.invalid",
            passwordHash = "protected-hash",
            nickname = "До изменения"
        ))
        daily.save(DailyProgress(
            playerId = player.id,
            activityDate = LocalDate.now(),
            currentStreak = 2,
            bestStreak = 3
        ))

        val updated = controller.updatePlayer(player.id, AdminUpdatePlayerRequest(
            nickname = "После изменения",
            role = "ADMIN",
            interests = listOf("игры", "накопления"),
            cash = 12_500.0,
            coins = 9,
            level = 4,
            finLiteracyScore = 77,
            testsCompleted = 1,
            miniGamesPlayed = 1
        ))
        assertEquals("После изменения", updated.nickname)
        assertEquals("ADMIN", updated.role)
        assertEquals(9, updated.coins)
        assertEquals(listOf("игры", "накопления"), updated.interests)
        assertEquals(1, updated.testsCompleted)

        val advanced = controller.advanceStreak(player.id, AdminAdvanceDaysRequest(5))
        assertEquals(7, advanced.currentStreak)
        assertTrue(advanced.bestStreak >= 7)

        claims.save(StreakRewardClaim(
            id = "${player.id}:7",
            playerId = player.id,
            milestoneDays = 7
        ))
        assertEquals(listOf(7), controller.listPlayers().single { it.id == player.id }.claimedStreakRewards)
        assertTrue(controller.resetStreakRewards(player.id).claimedStreakRewards.isEmpty())
    }
}
