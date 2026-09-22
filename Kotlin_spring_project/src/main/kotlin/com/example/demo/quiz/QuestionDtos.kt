package com.example.demo.quiz

data class QuestionResponse(
    val id: String,
    val text: String,
    val answers: List<String>,
    val correctIndex: Int,
    val category: String,
    val level: Int,
    val lessonId: String
)

data class CreateQuestionRequest(
    val text: String,
    val answers: List<String>,
    val correctIndex: Int,
    val category: String,
    val level: Int = 1,
    val lessonId: String = ""   // необязательно
)

fun CreateQuestionRequest.toEntity() = Question(
    text = text,
    answers = answers.joinToString("|"),
    correctIndex = correctIndex,
    category = category,
    level = level,
    lessonId = lessonId
)

fun Question.toResponse() = QuestionResponse(
    id = id,
    text = text,
    answers = answers.split("|"),
    correctIndex = correctIndex,
    category = category,
    level = level,
    lessonId = lessonId
)
