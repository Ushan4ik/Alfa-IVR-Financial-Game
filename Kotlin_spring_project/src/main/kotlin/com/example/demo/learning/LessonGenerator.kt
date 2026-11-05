package com.example.demo.learning

import com.example.demo.player.PlayerRepository
import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import org.slf4j.LoggerFactory
import org.springframework.scheduling.annotation.Scheduled
import org.springframework.stereotype.Service

@Service
class LessonGenerator(
    private val lessons: PersonalLessonRepository,
    private val attempts: LearningAttemptRepository,
    private val players: PlayerRepository,
    private val providers: List<LessonContentProvider>
) {
    val enabled get() = providers.any { it.enabled }
    private val json = jacksonObjectMapper()
    private val log = LoggerFactory.getLogger(javaClass)

    // Fallback is stored before generation. The DB queue survives a server restart.
    @Scheduled(fixedDelayString = "\${learning.generation-delay-ms:5000}")
    fun generatePending() {
        for (lesson in lessons.findTop3ByStatusOrderByCreatedAtAsc("PENDING")) {
            val provider = providers.firstOrNull { it.enabled }
            val content = try { provider?.generate(context(lesson)) }
                catch (exception: Exception) {
                    log.warn("{} lesson generation failed; using curated fallback", provider?.providerName ?: "No", exception)
                    null
                }
            lessons.storeGeneration(
                lesson.id,
                content?.let { json.writeValueAsString(it) } ?: lesson.contentJson,
                if (content == null) "FALLBACK" else "READY"
            )
        }
    }

    private fun context(lesson: PersonalLesson): LessonGenerationContext {
        val topic = LearningCatalog.topic(lesson.topicId)
        val attempt = attempts.findById(lesson.attemptId).orElseThrow()
        val answers: List<LearningQuestion> = json.readValue(attempt.questionsJson)
        val history = attempts.findByPlayerIdOrderByCreatedAtDesc(lesson.playerId).filter { it.finished }.take(10)
            .flatMap { json.readValue<List<LearningQuestion>>(it.questionsJson) }.filter { it.topicId == topic.id }
        val previous = lessons.findByPlayerIdOrderByCreatedAtDesc(lesson.playerId)
            .asSequence().filter { it.id != lesson.id }.mapNotNull {
                runCatching { json.readValue<LessonContent>(it.contentJson) }.getOrNull()
            }.firstOrNull { it.story.isNotBlank() && it.character != null }
        val interests = players.findById(lesson.playerId).orElseThrow().interests
            .split(',').map { it.trim() }.filter { it.isNotBlank() }
        return LessonGenerationContext(
            lesson.id, topic, answers,
            history.count { it.selectedIndex == it.correctIndex }, history.size,
            interests, previous
        )
    }
}
