package com.example.demo.daily

import jakarta.persistence.Entity
import jakarta.persistence.Id
import jakarta.persistence.Table
import jakarta.persistence.UniqueConstraint
import org.springframework.data.jpa.repository.JpaRepository
import java.time.Instant
import java.time.LocalDate

@Entity
@Table(name = "daily_progress")
data class DailyProgress(
    @Id
    val playerId: String = "",
    val activityDate: LocalDate = LocalDate.of(1970, 1, 1),
    val currentStreak: Int = 0,
    val bestStreak: Int = 0,
    val testsCompleted: Int = 0,
    val miniGamesPlayed: Int = 0
)

interface DailyProgressRepository : JpaRepository<DailyProgress, String>

@Entity
@Table(
    name = "streak_reward_claims",
    uniqueConstraints = [UniqueConstraint(columnNames = ["player_id", "milestone_days"])]
)
data class StreakRewardClaim(
    @Id val id: String = "",
    val playerId: String = "",
    val milestoneDays: Int = 0,
    val claimedAt: Instant = Instant.now()
)

interface StreakRewardClaimRepository : JpaRepository<StreakRewardClaim, String> {
    fun findByPlayerId(playerId: String): List<StreakRewardClaim>
    fun deleteByPlayerId(playerId: String)
}

data class DailyQuestResponse(
    val id: String,
    val title: String,
    val progress: Int,
    val target: Int,
    val completed: Boolean,
    val available: Boolean
)

data class DailyProgressResponse(
    val date: String,
    val currentStreak: Int,
    val bestStreak: Int,
    val completedCount: Int,
    val totalCount: Int,
    val quests: List<DailyQuestResponse>,
    val coins: Int,
    val marketCash: Double,
    val streakRewards: List<StreakRewardResponse>
)

data class StreakRewardResponse(
    val days: Int,
    val coins: Int,
    val marketCash: Int,
    val unlocked: Boolean,
    val claimed: Boolean
)
