package com.example.demo.quiz

import jakarta.persistence.*
import java.util.UUID

@Entity
@Table(name = "questions")
data class Question(
    @Id
    val id: String = UUID.randomUUID().toString(),

    val text: String = "",
    val answers: String = "",   // варианты через |
    val correctIndex: Int = 0,
    val category: String = "",  // "учёба", "финансы"
    val level: Int = 1,
    val lessonId: String = ""   // "lesson_budget", "lesson_inflation" — пусто если общий
)
