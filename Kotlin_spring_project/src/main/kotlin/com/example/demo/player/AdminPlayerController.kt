package com.example.demo.player

import com.example.demo.daily.DailyProgress
import com.example.demo.daily.DailyProgressRepository
import com.example.demo.daily.StreakRewardClaimRepository
import com.example.demo.learning.LearningAttemptRepository
import com.example.demo.learning.PersonalLessonRepository
import com.example.demo.market.HoldingRepository
import com.example.demo.market.StockRepository
import org.springframework.http.HttpStatus
import org.springframework.transaction.annotation.Transactional
import org.springframework.web.bind.annotation.*
import org.springframework.web.server.ResponseStatusException
import java.time.LocalDate

@RestController
@RequestMapping("/admin/players")
class AdminPlayerController(
    private val playerRepository: PlayerRepository,
    private val progressRepository: PlayerProgressRepository,
    private val dailyProgressRepository: DailyProgressRepository,
    private val streakRewardClaimRepository: StreakRewardClaimRepository,
    private val learningAttemptRepository: LearningAttemptRepository,
    private val personalLessonRepository: PersonalLessonRepository,
    private val holdingRepository: HoldingRepository,
    private val stockRepository: StockRepository
) {
    @GetMapping
    fun listPlayers(): List<AdminPlayerResponse> = playerRepository.findAll()
        .map(::response)
        .sortedByDescending { it.totalValue }

    @PutMapping("/{id}")
    @Transactional
    fun updatePlayer(@PathVariable id: String, @RequestBody req: AdminUpdatePlayerRequest): AdminPlayerResponse {
        val player = requirePlayer(id)
        val email = req.email?.trim()?.lowercase() ?: player.email
        if (email.isBlank()) badRequest("Email не может быть пустым")
        val emailOwner = playerRepository.findByEmail(email)
        if (emailOwner != null && emailOwner.id != id) badRequest("Этот email уже используется")

        val role = req.role?.let {
            runCatching { PlayerRole.valueOf(it.uppercase()) }
                .getOrElse { badRequest("Роль должна быть PLAYER или ADMIN") }
        } ?: player.role
        val cash = req.cash ?: player.cash
        val coins = req.coins ?: player.coins
        if (!cash.isFinite() || cash < 0) badRequest("Баланс не может быть отрицательным")
        if (coins < 0) badRequest("Количество коинов не может быть отрицательным")

        playerRepository.save(player.copy(
            email = email,
            nickname = req.nickname?.trim() ?: player.nickname,
            role = role,
            interests = req.interests?.map(String::trim)?.filter(String::isNotBlank)?.joinToString(",")
                ?: player.interests,
            cash = cash,
            coins = coins
        ))

        val progress = progressRepository.findById(id).orElseGet { PlayerProgress(playerId = id) }
        val level = req.level ?: progress.level
        val score = req.finLiteracyScore ?: progress.finLiteracyScore
        if (level < 1) badRequest("Уровень должен быть не меньше 1")
        if (score < 0) badRequest("Очки не могут быть отрицательными")
        progressRepository.save(progress.copy(level = level, finLiteracyScore = score))

        if (listOf(req.activityDate, req.currentStreak, req.bestStreak,
                req.testsCompleted, req.miniGamesPlayed).any { it != null }) {
            val daily = dailyProgressRepository.findById(id).orElseGet { DailyProgress(playerId = id) }
            val activityDate = req.activityDate?.let {
                runCatching { LocalDate.parse(it) }
                    .getOrElse { badRequest("Дата должна быть в формате ГГГГ-ММ-ДД") }
            } ?: daily.activityDate
            val currentStreak = req.currentStreak ?: daily.currentStreak
            val bestStreak = req.bestStreak ?: daily.bestStreak
            val tests = req.testsCompleted ?: daily.testsCompleted
            val games = req.miniGamesPlayed ?: daily.miniGamesPlayed
            if (minOf(currentStreak, bestStreak, tests, games) < 0) {
                badRequest("Значения прогресса не могут быть отрицательными")
            }
            dailyProgressRepository.save(daily.copy(
                activityDate = activityDate,
                currentStreak = currentStreak,
                bestStreak = maxOf(bestStreak, currentStreak),
                testsCompleted = tests,
                miniGamesPlayed = games
            ))
        }
        return response(requirePlayer(id))
    }

    @PostMapping("/{id}/streak/advance")
    @Transactional
    fun advanceStreak(@PathVariable id: String, @RequestBody req: AdminAdvanceDaysRequest): AdminPlayerResponse {
        requirePlayer(id)
        if (req.days !in 1..365) badRequest("Можно пропустить от 1 до 365 дней")
        val daily = dailyProgressRepository.findById(id).orElseGet {
            DailyProgress(playerId = id, activityDate = LocalDate.now())
        }
        val nextStreak = daily.currentStreak + req.days
        dailyProgressRepository.save(daily.copy(
            activityDate = LocalDate.now(),
            currentStreak = nextStreak,
            bestStreak = maxOf(daily.bestStreak, nextStreak)
        ))
        return response(requirePlayer(id))
    }

    @DeleteMapping("/{id}/streak/rewards")
    @Transactional
    fun resetStreakRewards(@PathVariable id: String): AdminPlayerResponse {
        requirePlayer(id)
        streakRewardClaimRepository.deleteByPlayerId(id)
        return response(requirePlayer(id))
    }

    private fun response(player: Player): AdminPlayerResponse {
        val progress = progressRepository.findById(player.id).orElseGet { PlayerProgress(playerId = player.id) }
        val daily = dailyProgressRepository.findById(player.id).orElse(null)
        val holdings = holdingRepository.findByPlayerId(player.id).filter { it.quantity != 0.0 }
        val stocks = stockRepository.findAllById(holdings.map { it.stockId }).associateBy { it.id }
        val holdingsValue = holdings.sumOf { (stocks[it.stockId]?.currentPrice ?: 0.0) * it.quantity }
        return AdminPlayerResponse(
            id = player.id,
            email = player.email,
            nickname = player.nickname,
            role = player.role.name,
            createdAt = player.createdAt.toString(),
            cash = player.cash,
            coins = player.coins,
            level = progress.level,
            finLiteracyScore = progress.finLiteracyScore,
            interests = player.interests.split(",").map(String::trim).filter(String::isNotBlank),
            totalValue = player.cash + holdingsValue,
            activityDate = daily?.activityDate?.toString(),
            currentStreak = daily?.currentStreak ?: 0,
            bestStreak = daily?.bestStreak ?: 0,
            testsCompleted = daily?.testsCompleted ?: 0,
            miniGamesPlayed = daily?.miniGamesPlayed ?: 0,
            claimedStreakRewards = streakRewardClaimRepository.findByPlayerId(player.id)
                .map { it.milestoneDays }.sorted(),
            attemptsCount = learningAttemptRepository.findByPlayerIdOrderByCreatedAtDesc(player.id).size,
            lessonsCount = personalLessonRepository.findByPlayerIdOrderByCreatedAtDesc(player.id).size,
            holdingsCount = holdings.size
        )
    }

    private fun requirePlayer(id: String): Player = playerRepository.findById(id).orElseThrow {
        ResponseStatusException(HttpStatus.NOT_FOUND, "Игрок не найден")
    }

    private fun badRequest(message: String): Nothing =
        throw ResponseStatusException(HttpStatus.BAD_REQUEST, message)
}
