package com.example.demo.learning

import org.springframework.security.core.Authentication
import org.springframework.web.bind.annotation.*

@RestController
@RequestMapping("/learning")
class LearningController(private val service: LearningService) {
    @PostMapping("/attempts")
    fun start(auth: Authentication, @RequestBody req: StartRequest) = service.start(auth.principal as String, req)
    @PostMapping("/attempts/{id}/answers")
    fun answer(auth: Authentication, @PathVariable id: String, @RequestBody req: AnswerRequest) =
        service.answer(auth.principal as String, id, req)
    @PostMapping("/attempts/{id}/finish")
    fun finish(auth: Authentication, @PathVariable id: String) = service.finish(auth.principal as String, id)
    @GetMapping("/lessons")
    fun lessons(auth: Authentication) = service.list(auth.principal as String)
    @GetMapping("/profile")
    fun profile(auth: Authentication) = service.profile(auth.principal as String)
}
