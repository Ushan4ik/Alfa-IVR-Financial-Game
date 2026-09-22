package com.example.demo.quiz

import org.springframework.data.jpa.repository.JpaRepository

interface QuestionRepository : JpaRepository<Question, String> {
    fun findByCategoryAndLevel(category: String, level: Int): List<Question>
    fun findByCategory(category: String): List<Question>
    fun findByLessonId(lessonId: String): List<Question>
}
