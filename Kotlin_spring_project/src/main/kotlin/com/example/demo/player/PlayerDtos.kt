package com.example.demo.player

data class PlayerProfileResponse(
    val id: String,
    val email: String,
    val nickname: String,
    val level: Int,
    val finLiteracyScore: Int,
    val interests: List<String>
)

data class SetInterestsRequest(val text: String)

data class UpdateProgressRequest(
    val finLiteracyScore: Int? = null,
    val level: Int? = null
)

data class AdminPlayerResponse(
    val id: String,
    val email: String,
    val nickname: String,
    val role: String,
    val createdAt: String,
    val cash: Double,
    val coins: Int,
    val level: Int,
    val finLiteracyScore: Int,
    val interests: List<String>,
    val totalValue: Double,
    val activityDate: String?,
    val currentStreak: Int,
    val bestStreak: Int,
    val testsCompleted: Int,
    val miniGamesPlayed: Int,
    val claimedStreakRewards: List<Int>,
    val attemptsCount: Int,
    val lessonsCount: Int,
    val holdingsCount: Int
)

data class AdminUpdatePlayerRequest(
    val email: String? = null,
    val nickname: String? = null,
    val role: String? = null,
    val interests: List<String>? = null,
    val cash: Double? = null,
    val coins: Int? = null,
    val level: Int? = null,
    val finLiteracyScore: Int? = null,
    val activityDate: String? = null,
    val currentStreak: Int? = null,
    val bestStreak: Int? = null,
    val testsCompleted: Int? = null,
    val miniGamesPlayed: Int? = null
)

data class AdminAdvanceDaysRequest(val days: Int = 1)
