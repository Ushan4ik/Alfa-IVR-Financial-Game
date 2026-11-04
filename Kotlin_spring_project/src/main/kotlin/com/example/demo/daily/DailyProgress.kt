package com.example.demo.daily

import jakarta.persistence.Entity
import jakarta.persistence.Id
import jakarta.persistence.Table
import org.springframework.data.jpa.repository.JpaRepository
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
    val quests: List<DailyQuestResponse>
)
