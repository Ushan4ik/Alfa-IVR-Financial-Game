package com.example.demo.learning

import com.example.demo.player.*
import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import org.springframework.http.HttpStatus
import org.springframework.stereotype.Service
import org.springframework.transaction.annotation.Transactional
import org.springframework.web.server.ResponseStatusException

@Service
class LearningService(
    private val players: PlayerRepository,
    private val progress: PlayerProgressRepository,
    private val attempts: LearningAttemptRepository,
    private val lessons: PersonalLessonRepository,
    private val generator: LessonGenerator
) {
    private val json = jacksonObjectMapper()
    private fun missing(): Nothing = throw ResponseStatusException(HttpStatus.NOT_FOUND)
    private fun bad(message: String): Nothing = throw ResponseStatusException(HttpStatus.BAD_REQUEST, message)
    private fun lock(playerId: String) { players.lockById(playerId) ?: missing() }
    private fun owned(playerId: String, id: String): LearningAttempt =
        attempts.findById(id).orElse(null)?.takeIf { it.playerId == playerId } ?: missing()
    private fun questions(a: LearningAttempt): List<LearningQuestion> = json.readValue(a.questionsJson)
    private fun finished(playerId: String) = attempts.findByPlayerIdOrderByCreatedAtDesc(playerId).filter { it.finished }

    @Transactional
    fun start(playerId: String, req: StartRequest): StartResponse {
        lock(playerId)
        val lesson = if (req.lessonId.isNotBlank()) lessons.findById(req.lessonId).orElse(null)
            ?.takeIf { it.playerId == playerId } ?: missing() else null
        val topic = lesson?.topicId ?: req.topicId
        if (topic.isNotBlank() && LearningCatalog.topics.none { it.id == topic }) bad("Неизвестная тема")
        if (req.diagnostic && (topic.isNotBlank() || lesson != null)) bad("Вводный тест охватывает все темы")
        val history = finished(playerId)
        if (req.diagnostic && history.any { it.diagnostic }) bad("Вводный тест уже пройден")
        // Повторный тест сначала предлагает вопросы, которых не было в исходной попытке.
        val previous = lesson?.let { questions(owned(playerId, it.attemptId)).map { q -> q.id }.toSet() } ?: emptySet()
        val pool = LearningCatalog.questions.filter { topic.isBlank() || it.topicId == topic }.shuffled()
        val selected = if (req.diagnostic) LearningCatalog.topics.map { t -> pool.first { it.topicId == t.id } }
            else pool.sortedBy { it.id in previous }.take(if (topic.isBlank()) 5 else 3)
        val attempt = attempts.save(LearningAttempt(playerId = playerId, diagnostic = req.diagnostic,
            parentLessonId = lesson?.id ?: "", questionsJson = json.writeValueAsString(selected)))
        return StartResponse(attempt.id, selected.map { PublicQuestion(it.id, it.text, it.answers, it.topicId) }, attempt.diagnostic)
    }

    @Transactional
    fun answer(playerId: String, id: String, req: AnswerRequest): AnswerResponse {
        lock(playerId)
        val attempt = owned(playerId, id)
        val snapshot = questions(attempt)
        val q = snapshot.find { it.id == req.questionId } ?: bad("Вопрос не входит в попытку")
        if (req.selectedIndex !in q.answers.indices) bad("Некорректный вариант ответа")
        if (q.selectedIndex != null && q.selectedIndex != req.selectedIndex)
            throw ResponseStatusException(HttpStatus.CONFLICT, "Ответ уже принят")
        if (q.selectedIndex == null) {
            if (attempt.finished) bad("Попытка завершена")
            q.selectedIndex = req.selectedIndex
            attempt.questionsJson = json.writeValueAsString(snapshot)
            attempts.save(attempt)
        }
        return AnswerResponse(q.selectedIndex == q.correctIndex, q.correctIndex, q.explanation)
    }

    @Transactional
    fun finish(playerId: String, id: String): FinishResponse {
        lock(playerId)
        val attempt = owned(playerId, id)
        val snapshot = questions(attempt)
        if (snapshot.any { it.selectedIndex == null }) bad("Сначала ответь на все вопросы")
        val correct = snapshot.count { it.selectedIndex == it.correctIndex }
        val passed = correct * 2 >= snapshot.size
        if (!attempt.finished) {
            val history = finished(playerId)
            val alreadyRewarded = history.flatMap { questions(it) }
                .filter { it.selectedIndex == it.correctIndex }.map { it.id }.toSet()
            attempt.awardedScore = snapshot.count { it.selectedIndex == it.correctIndex && it.id !in alreadyRewarded } * 10
            val p = progress.findById(playerId).orElse(PlayerProgress(playerId = playerId))
            val score = p.finLiteracyScore + attempt.awardedScore
            progress.save(p.copy(finLiteracyScore = score, level = maxOf(p.level, 1 + score / 50)))
            val weakest = snapshot.groupBy { it.topicId }.minBy { (_, qs) ->
                qs.count { it.selectedIndex == it.correctIndex }.toDouble() / qs.size
            }.key
            val topic = LearningCatalog.topic(weakest)
            val topicAnswers = snapshot.filter { it.topicId == weakest }
            val mistakes = topicAnswers.filter { it.selectedIndex != it.correctIndex }
            val explanation = if (mistakes.isEmpty())
                "В этой попытке ты ответил верно на ${topicAnswers.size} вопросов по теме. Закрепи правило на новых примерах."
                else mistakes.joinToString("\n\n") { "${it.text}\nТвой ответ: ${it.answers[it.selectedIndex!!]}.\nВерный ответ: ${it.answers[it.correctIndex]}. ${it.explanation}" }
            val content = LessonContent("${topic.title}: ${if (mistakes.isEmpty()) "закрепление" else "разбор ошибок"}",
                explanation, topic.example, listOf(topic.rule))
            val lesson = lessons.save(PersonalLesson(playerId = playerId, attemptId = id, topicId = weakest,
                status = if (generator.enabled) "PENDING" else "FALLBACK", contentJson = json.writeValueAsString(content)))
            attempt.lessonId = lesson.id
            attempt.finished = true
            attempts.save(attempt)
            if (attempt.parentLessonId.isNotBlank() && passed) {
                val parent = lessons.findById(attempt.parentLessonId).orElseThrow()
                parent.completed = true
                lessons.save(parent)
            }
        }
        return FinishResponse(correct * 10, attempt.awardedScore, correct, snapshot.size, passed, attempt.lessonId)
    }

    @Transactional(readOnly = true)
    fun list(playerId: String) = lessons.findByPlayerIdOrderByCreatedAtDesc(playerId).map { l ->
        val c: LessonContent = json.readValue(l.contentJson)
        LessonResponse(l.id, l.topicId, l.status, l.completed, c.title, c.explanation, c.example, c.cheatSheet)
    }

    @Transactional(readOnly = true)
    fun profile(playerId: String): LearningProfile {
        val history = finished(playerId)
        val all = history.flatMap { questions(it) }
        return LearningProfile(history.any { it.diagnostic }, LearningCatalog.topics.map { t ->
            val recent = all.filter { it.topicId == t.id }.take(20)
            TopicProgress(t.id, t.title, recent.count { it.selectedIndex == it.correctIndex }, recent.size)
        })
    }
}
