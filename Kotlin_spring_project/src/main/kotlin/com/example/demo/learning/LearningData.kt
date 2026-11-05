package com.example.demo.learning

import jakarta.persistence.*
import org.springframework.data.jpa.repository.JpaRepository
import java.time.Instant
import java.util.UUID

@Entity
@Table(name = "learning_attempts", indexes = [Index(columnList = "playerId")])
class LearningAttempt(
    @Id var id: String = UUID.randomUUID().toString(),
    var playerId: String = "",
    var diagnostic: Boolean = false,
    var parentLessonId: String = "",
    @Column(columnDefinition = "text") var questionsJson: String = "[]",
    var finished: Boolean = false,
    var awardedScore: Int = 0,
    var lessonId: String = "",
    var createdAt: Instant = Instant.now()
)

@Entity
@Table(name = "personal_lessons", indexes = [Index(columnList = "playerId")])
class PersonalLesson(
    @Id var id: String = UUID.randomUUID().toString(),
    var playerId: String = "",
    @Column(unique = true) var attemptId: String = "",
    var topicId: String = "",
    var status: String = "FALLBACK",
    var completed: Boolean = false,
    @Column(columnDefinition = "text") var contentJson: String = "{}",
    var createdAt: Instant = Instant.now()
)

interface LearningAttemptRepository : JpaRepository<LearningAttempt, String> {
    fun findByPlayerIdOrderByCreatedAtDesc(playerId: String): List<LearningAttempt>
}
interface PersonalLessonRepository : JpaRepository<PersonalLesson, String> {
    fun findByPlayerIdOrderByCreatedAtDesc(playerId: String): List<PersonalLesson>
    fun findTop3ByStatusOrderByCreatedAtAsc(status: String): List<PersonalLesson>

    @org.springframework.transaction.annotation.Transactional
    @org.springframework.data.jpa.repository.Modifying
    @org.springframework.data.jpa.repository.Query("update PersonalLesson l set l.contentJson = :content, l.status = :status where l.id = :id and l.status = 'PENDING'")
    fun storeGeneration(@org.springframework.data.repository.query.Param("id") id: String,
        @org.springframework.data.repository.query.Param("content") content: String,
        @org.springframework.data.repository.query.Param("status") status: String): Int
}

data class LearningQuestion(val id: String, val topicId: String, val text: String,
    val answers: List<String>, val correctIndex: Int, val explanation: String,
    var selectedIndex: Int? = null)
data class PublicQuestion(val id: String, val text: String, val answers: List<String>, val topicId: String)
data class StartRequest(val topicId: String = "", val lessonId: String = "", val diagnostic: Boolean = false)
data class StartResponse(val id: String, val questions: List<PublicQuestion>, val diagnostic: Boolean)
data class AnswerRequest(val questionId: String = "", val selectedIndex: Int = -1)
data class AnswerResponse(val correct: Boolean, val correctIndex: Int, val explanation: String)
data class FinishResponse(val score: Int, val awardedScore: Int, val correctCount: Int, val total: Int,
    val passed: Boolean, val lessonId: String)
data class LessonCharacter(val name: String = "", val age: Int = 0, val personality: String = "")
data class LessonTerm(val term: String = "", val text: String = "", val definition: String = "", val example: String = "")
data class LessonContent(val title: String = "", val explanation: String = "", val example: String = "",
    val cheatSheet: List<String> = emptyList(), val story: String = "", val takeaway: String = "",
    val character: LessonCharacter? = null, val terms: List<LessonTerm> = emptyList(),
    val source: String = "fallback", val modelVersion: String = "")
data class LessonResponse(val id: String, val topicId: String, val status: String, val completed: Boolean,
    val title: String, val explanation: String, val example: String, val cheatSheet: List<String>,
    val story: String, val takeaway: String, val character: LessonCharacter?, val terms: List<LessonTerm>,
    val source: String, val modelVersion: String)
data class TopicProgress(val topicId: String, val title: String, val correct: Int, val total: Int)
data class LearningProfile(val diagnosticCompleted: Boolean, val topics: List<TopicProgress>)
