package com.example.demo.learning

data class LessonGenerationContext(
    val lessonId: String,
    val topic: LearningCatalog.Topic,
    val answers: List<LearningQuestion>,
    val recentCorrect: Int,
    val recentTotal: Int,
    val interests: List<String>,
    val previousContent: LessonContent?
)

interface LessonContentProvider {
    val enabled: Boolean
    val providerName: String
    fun generate(context: LessonGenerationContext): LessonContent
}

