package com.example.demo.quiz

import com.example.demo.player.PlayerRepository
import org.springframework.security.core.Authentication
import org.springframework.web.bind.annotation.*

@RestController
@RequestMapping("/questions")
class QuestionController(
    private val questionRepository: QuestionRepository,
    private val playerRepository: PlayerRepository
) {

    // Получить вопросы под интересы и уровень текущего игрока
    @GetMapping
    fun getForPlayer(authentication: Authentication): List<QuestionResponse> {
        val playerId = authentication.principal as String
        val player = playerRepository.findById(playerId).orElseThrow()

        val interests = player.interests.split(",").filter { it.isNotBlank() }
        val level = 1 // TODO: брать из PlayerProgress когда будет нужно

        // берём вопросы по первому интересу, или все если интересов нет
        val category = interests.firstOrNull() ?: ""
        val questions = if (category.isNotEmpty())
            questionRepository.findByCategoryAndLevel(category, level)
        else
            questionRepository.findAll()

        return questions.shuffled().take(5).map { it.toResponse() }
    }

    // Получить вопросы по конкретной категории
    @GetMapping("/category/{category}")
    fun getByCategory(@PathVariable category: String): List<QuestionResponse> =
        questionRepository.findByCategory(category).map { it.toResponse() }

    // Получить вопросы конкретного урока — вызывается после прохождения урока
    @GetMapping("/lesson/{lessonId}")
    fun getByLesson(@PathVariable lessonId: String): List<QuestionResponse> =
        questionRepository.findByLessonId(lessonId).shuffled().map { it.toResponse() }

    // Добавить один вопрос
    @PostMapping
    fun create(@RequestBody req: CreateQuestionRequest): QuestionResponse =
        questionRepository.save(req.toEntity()).toResponse()

    // Загрузить сразу много вопросов из JSON массива
    @PostMapping("/bulk")
    fun createBulk(@RequestBody questions: List<CreateQuestionRequest>): Map<String, Int> {
        val saved = questionRepository.saveAll(questions.map { it.toEntity() })
        return mapOf("added" to saved.count())
    }

    // Удалить все вопросы категории (для пересоздания)
    @DeleteMapping("/category/{category}")
    fun deleteByCategory(@PathVariable category: String): Map<String, Int> {
        val questions = questionRepository.findByCategory(category)
        questionRepository.deleteAll(questions)
        return mapOf("deleted" to questions.count())
    }
}
