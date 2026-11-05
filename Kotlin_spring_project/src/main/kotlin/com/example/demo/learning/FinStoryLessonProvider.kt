package com.example.demo.learning

import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import org.springframework.beans.factory.annotation.Value
import org.springframework.core.annotation.Order
import org.springframework.stereotype.Component
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.time.Duration

data class FinStoryMistake(val question: String, val userAnswer: String, val correctAnswer: String,
    val explanation: String)
data class FinStoryRequest(val requestId: String, val topicId: String, val title: String,
    val learningGoal: String, val verifiedMaterial: String, val verifiedExample: String,
    val interestText: String, val mistakes: List<FinStoryMistake>, val recentCorrect: Int,
    val recentTotal: Int, val previousStory: String, val character: LessonCharacter?, val terms: List<String>)
data class FinStoryResponse(val title: String = "", val story: String = "", val takeaway: String = "",
    val character: LessonCharacter? = null, val terms: List<LessonTerm> = emptyList(),
    val cheatSheet: List<String> = emptyList(), val source: String = "finstory",
    val modelVersion: String = "")

@Component
@Order(10)
class FinStoryLessonProvider(
    @Value("\${learning.finstory.url:}") private val serviceUrl: String,
    @Value("\${learning.finstory.token:}") private val token: String
) : LessonContentProvider {
    override val enabled get() = serviceUrl.isNotBlank()
    override val providerName = "finstory"
    private val json = jacksonObjectMapper()
    private val http = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(4)).build()

    override fun generate(context: LessonGenerationContext): LessonContent {
        check(enabled)
        val mistakes = context.answers.filter { it.selectedIndex != it.correctIndex }.map {
            FinStoryMistake(it.text, it.answers[it.selectedIndex!!], it.answers[it.correctIndex], it.explanation)
        }
        val previous = context.previousContent
        val body = FinStoryRequest(
            requestId = context.lessonId,
            topicId = context.topic.id,
            title = context.topic.title,
            learningGoal = "Понять правило и применить его в похожей финансовой ситуации",
            verifiedMaterial = context.topic.rule,
            verifiedExample = context.topic.example,
            interestText = context.interests.joinToString(", "),
            mistakes = mistakes,
            recentCorrect = context.recentCorrect,
            recentTotal = context.recentTotal,
            previousStory = previous?.story.orEmpty(),
            character = previous?.character,
            terms = context.topic.terms
        )
        val requestBuilder = HttpRequest.newBuilder(
            URI(serviceUrl.trimEnd('/') + "/internal/v1/lessons/generate")
        ).timeout(Duration.ofSeconds(50)).header("Content-Type", "application/json")
        if (token.isNotBlank()) requestBuilder.header("X-Internal-Token", token)
        val response = http.send(
            requestBuilder.POST(HttpRequest.BodyPublishers.ofString(json.writeValueAsString(body))).build(),
            HttpResponse.BodyHandlers.ofString()
        )
        check(response.statusCode() == 200) { "FinStory returned ${response.statusCode()}" }
        check(response.body().length <= 100_000)
        val result: FinStoryResponse = json.readValue(response.body())
        require(result.title.isNotBlank() && result.title.length <= 120)
        require(result.story.length in 120..3000)
        require(result.takeaway.isNotBlank() && result.takeaway.length <= 700)
        require(result.cheatSheet.size in 1..6 && result.cheatSheet.all { it.length <= 500 })
        require(result.terms.size <= 8)
        return LessonContent(
            title = result.title,
            explanation = result.story,
            example = result.takeaway,
            cheatSheet = result.cheatSheet,
            story = result.story,
            takeaway = result.takeaway,
            character = result.character,
            terms = result.terms,
            source = result.source,
            modelVersion = result.modelVersion
        )
    }
}

