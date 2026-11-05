package com.example.demo.learning

import com.example.demo.auth.JwtService
import com.example.demo.daily.DailyProgressRepository
import com.example.demo.player.*
import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper
import org.junit.jupiter.api.Assertions.*
import org.junit.jupiter.api.Test
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ActiveProfiles
import org.springframework.web.server.ResponseStatusException
import java.net.URI
import java.net.http.*
import java.util.UUID
import java.util.concurrent.Executors
import java.util.concurrent.Callable

@ActiveProfiles("dev")
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT, properties = [
    "spring.datasource.url=jdbc:h2:mem:learningtests;DB_CLOSE_DELAY=-1;NON_KEYWORDS=DAY",
    "learning.gemini.key=", "learning.gemini.model=", "learning.generation-delay-ms=3600000"
])
class LearningIntegrationTests {
    @Autowired lateinit var service: LearningService
    @Autowired lateinit var players: PlayerRepository
    @Autowired lateinit var progress: PlayerProgressRepository
    @Autowired lateinit var lessons: PersonalLessonRepository
    @Autowired lateinit var attempts: LearningAttemptRepository
    @Autowired lateinit var dailyProgress: DailyProgressRepository
    @Autowired lateinit var generator: LessonGenerator
    @Autowired lateinit var gemini: GeminiLessonProvider
    @Autowired lateinit var jwt: JwtService
    @LocalServerPort var port: Int = 0
    private val json = jacksonObjectMapper()
    private fun player() = players.save(Player(email = "${UUID.randomUUID()}@test.invalid", passwordHash = "unused")).id
    private fun right(id: String) = LearningCatalog.questions.first { it.id == id }.correctIndex
    private fun answerAll(player: String, attempt: StartResponse, correct: Boolean = true) {
        attempt.questions.forEach { q -> service.answer(player, attempt.id,
            AnswerRequest(q.id, if (correct) right(q.id) else (right(q.id) + 1) % q.answers.size)) }
    }
    private fun status(code: Int, action: () -> Unit) {
        assertEquals(code, assertThrows(ResponseStatusException::class.java) { action() }.statusCode.value())
    }
    private fun request(path: String, player: String?, method: String = "GET", body: String = "{}"): HttpResponse<String> {
        val b = HttpRequest.newBuilder(URI("http://localhost:$port$path")).header("Content-Type", "application/json")
        if (player != null) b.header("Authorization", "Bearer " + jwt.generateToken(player))
        b.method(method, if (method == "GET") HttpRequest.BodyPublishers.noBody() else HttpRequest.BodyPublishers.ofString(body))
        return HttpClient.newHttpClient().send(b.build(), HttpResponse.BodyHandlers.ofString())
    }

    @Test fun `diagnostic covers every topic without leaking answers and requires authentication`() {
        assertTrue(request("/learning/profile", null).statusCode() in listOf(401,403))
        val p = player()
        val response = request("/learning/attempts", p, "POST", "{\"diagnostic\":true}")
        assertEquals(200, response.statusCode())
        val data = json.readTree(response.body())
        assertEquals(11, LearningCatalog.topics.size)
        assertTrue(LearningCatalog.topics.all { topic ->
            LearningCatalog.questions.count { it.topicId == topic.id } >= 4
        })
        assertEquals(LearningCatalog.topics.size, data.path("questions").size())
        assertEquals(LearningCatalog.topics.size,
            data.path("questions").map { it.path("topicId").asText() }.toSet().size)
        assertFalse(response.body().contains("correctIndex"))
        assertFalse(response.body().contains("explanation"))
        assertEquals(409, request("/player/progress", p, "PUT", "{\"finLiteracyScore\":999999}").statusCode())
    }

    @Test fun `answers cannot be replaced and incomplete attempts cannot finish`() {
        val p = player(); val a = service.start(p, StartRequest(diagnostic = true)); val q = a.questions.first()
        status(400) { service.finish(p, a.id) }
        status(400) { service.answer(p, a.id, AnswerRequest(q.id, 99)) }
        status(400) { service.answer(p, a.id, AnswerRequest("not-in-attempt", 0)) }
        val first = service.answer(p, a.id, AnswerRequest(q.id, right(q.id)))
        assertEquals(first, service.answer(p, a.id, AnswerRequest(q.id, right(q.id))))
        status(409) { service.answer(p, a.id, AnswerRequest(q.id, (right(q.id)+1)%q.answers.size)) }
        assertFalse(progress.findById(p).isPresent)
    }

    @Test fun `mistakes create owned persistent lesson and fresh retry completes it`() {
        val p = player(); val stranger = player(); val a = service.start(p, StartRequest(diagnostic = true))
        answerAll(p, a, false)
        val result = service.finish(p, a.id)
        assertEquals(0, result.awardedScore)
        val lesson = service.list(p).single()
        assertEquals("FALLBACK", lesson.status)
        assertTrue(lesson.explanation.contains("Твой ответ:"))
        assertTrue(lesson.cheatSheet.isNotEmpty())
        assertTrue(service.list(stranger).isEmpty())
        status(404) { service.answer(stranger, a.id, AnswerRequest(a.questions[0].id, 0)) }
        status(404) { service.finish(stranger, a.id) }
        status(404) { service.start(stranger, StartRequest(lessonId = lesson.id)) }
        val retry = service.start(p, StartRequest(lessonId = lesson.id))
        assertTrue(retry.questions.all { it.topicId == lesson.topicId })
        assertTrue(retry.questions.none { q -> a.questions.any { it.id == q.id } })
        answerAll(p, retry)
        assertTrue(service.finish(p, retry.id).passed)
        assertTrue(lessons.findById(lesson.id).orElseThrow().completed)
        assertTrue(service.profile(p).diagnosticCompleted)
        status(400) { service.start(p, StartRequest(diagnostic = true)) }
    }

    @Test fun `concurrent finish is idempotent and correct questions are rewarded once`() {
        val p = player(); val a = service.start(p, StartRequest(topicId = "lesson_savings")); answerAll(p, a)
        val pool = Executors.newFixedThreadPool(2)
        try {
            val results = pool.invokeAll(List(2) { Callable { service.finish(p, a.id) } }).map { it.get() }
            assertEquals(results[0], results[1])
            assertEquals(30, progress.findById(p).orElseThrow().finLiteracyScore)
            assertEquals(1, dailyProgress.findById(p).orElseThrow().testsCompleted)
            assertEquals(1, service.list(p).size)
            val b = service.start(p, StartRequest(topicId = "lesson_savings")); answerAll(p, b)
            service.finish(p, b.id)
            assertTrue(progress.findById(p).orElseThrow().finLiteracyScore <= 40)
        } finally { pool.shutdownNow() }
    }

    @Test fun `pending fallback generation preserves completion state`() {
        val p = player(); val a = service.start(p, StartRequest()); answerAll(p, a)
        service.finish(p, a.id)
        val l = lessons.findByPlayerIdOrderByCreatedAtDesc(p).single()
        l.status = "PENDING"; l.completed = true; lessons.save(l)
        generator.generatePending()
        val stored = lessons.findById(l.id).orElseThrow()
        assertEquals("FALLBACK", stored.status)
        assertTrue(stored.completed)
        assertTrue(stored.contentJson.contains("cheatSheet"))
    }
    @Test fun `HTTP flow returns saved personalized lesson and rejects access by another player`() {
        val p = player()
        val start = json.readTree(request("/learning/attempts", p, "POST", "{\"diagnostic\":true}").body())
        val id = start.path("id").asText()
        for (q in start.path("questions")) {
            val questionId = q.path("id").asText()
            val wrong = (right(questionId) + 1) % q.path("answers").size()
            val answer = request("/learning/attempts/$id/answers", p, "POST",
                json.writeValueAsString(AnswerRequest(questionId, wrong)))
            assertEquals(200, answer.statusCode())
            assertFalse(json.readTree(answer.body()).path("correct").asBoolean())
        }
        val finish = request("/learning/attempts/$id/finish", p, "POST")
        assertEquals(200, finish.statusCode())
        assertEquals(finish.body(), request("/learning/attempts/$id/finish", p, "POST").body())
        val personal = json.readTree(request("/learning/lessons", p).body())
        assertEquals(1, personal.size())
        assertTrue(personal[0].path("explanation").asText().contains("Твой ответ:"))
        assertEquals(404, request("/learning/attempts/$id/finish", player(), "POST").statusCode())
    }

    @Test fun `Gemini content must be complete valid and bounded`() {
        fun response(text: String, reason: String = "STOP") = json.writeValueAsString(mapOf(
            "candidates" to listOf(mapOf("finishReason" to reason, "content" to mapOf("parts" to listOf(mapOf("text" to text)))))))
        val content = LessonContent("Вклады", "Короткий разбор", "Учебный пример", listOf("Проверяй условия"))
        assertEquals(content.copy(source = "gemini"), gemini.parseResponse(response(json.writeValueAsString(content))))
        assertThrows(Exception::class.java) { gemini.parseResponse(response("{}")) }
        assertThrows(Exception::class.java) { gemini.parseResponse(response("not JSON")) }
        assertThrows(Exception::class.java) { gemini.parseResponse(response(json.writeValueAsString(content), "MAX_TOKENS")) }
        assertThrows(Exception::class.java) { gemini.parseResponse(response(json.writeValueAsString(content.copy(title = "x".repeat(101))))) }
    }

    @Test fun `player cannot use admin APIs while admin can manage content`() {
        val regular = player()
        val admin = players.save(Player(
            email = "${UUID.randomUUID()}@test.invalid",
            passwordHash = "unused",
            role = PlayerRole.ADMIN
        )).id

        assertEquals(403, request("/admin/players", regular).statusCode())
        assertEquals(200, request("/admin/players", admin).statusCode())

        val question = """
            {
              "text":"Какой ответ проверяет роль?",
              "answers":["A","B","C","D"],
              "correctIndex":0,
              "category":"security",
              "level":1,
              "lessonId":""
            }
        """.trimIndent()
        assertEquals(403, request("/questions", regular, "POST", question).statusCode())
        assertEquals(200, request("/questions", admin, "POST", question).statusCode())
    }

}
