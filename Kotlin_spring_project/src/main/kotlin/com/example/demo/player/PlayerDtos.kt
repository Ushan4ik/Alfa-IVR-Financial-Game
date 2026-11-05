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
    val cash: Double,
    val coins: Int,
    val level: Int,
    val finLiteracyScore: Int,
    val interests: List<String>,
    val totalValue: Double
)

data class AdminUpdatePlayerRequest(
    val cash: Double? = null,
    val coins: Int? = null,
    val level: Int? = null,
    val finLiteracyScore: Int? = null,
    val nickname: String? = null
)
