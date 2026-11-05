package com.example.demo.learning

import com.example.demo.player.PlayerRepository
import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import org.slf4j.LoggerFactory
import org.springframework.scheduling.annotation.Scheduled
import org.springframework.stereotype.Service
import java.util.concurrent.ConcurrentHashMap

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
    private val previewCache = ConcurrentHashMap<String, LessonContent>()
    private val latestPreview = ConcurrentHashMap<String, LessonContent>()

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

    fun preview(playerId: String, topicId: String): LessonContent? {
        val provider = providers.firstOrNull { it.enabled } ?: return null
        val topic = LearningCatalog.topic(topicId)
        val history = attempts.findByPlayerIdOrderByCreatedAtDesc(playerId).filter { it.finished }.take(10)
            .flatMap { json.readValue<List<LearningQuestion>>(it.questionsJson) }.filter { it.topicId == topic.id }
        val interests = players.findById(playerId).orElseThrow().interests
            .split(',').map { it.trim() }.filter { it.isNotBlank() }
        val previous = lessons.findByPlayerIdOrderByCreatedAtDesc(playerId).asSequence().mapNotNull {
            runCatching { json.readValue<LessonContent>(it.contentJson) }.getOrNull()
        }.firstOrNull { it.story.isNotBlank() && it.character != null } ?: latestPreview[playerId]
        val cacheKey = listOf(playerId, topicId, interests.joinToString(","),
            history.count { it.selectedIndex == it.correctIndex }, history.size).joinToString("|")
        previewCache[cacheKey]?.let { return it }
        val context = LessonGenerationContext(
            "preview-$playerId-$topicId", topic, emptyList(),
            history.count { it.selectedIndex == it.correctIndex }, history.size,
            interests, previous
        )
        return try {
            provider.generate(context).also {
                previewCache[cacheKey] = it
                latestPreview[playerId] = it
            }
        } catch (exception: Exception) {
            log.warn("{} topic preview generation failed", provider.providerName, exception)
            null
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
