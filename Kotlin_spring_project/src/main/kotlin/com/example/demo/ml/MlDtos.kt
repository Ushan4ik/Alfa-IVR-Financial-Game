package com.example.demo.ml

data class MlClassifyRequest(val text: String)

data class MlClassifyResponse(val interests: List<String> = emptyList(),
    val primaryInterest: String = "", val weights: Map<String, Double> = emptyMap())
