package com.example.demo.ml

import org.slf4j.LoggerFactory
import org.springframework.beans.factory.annotation.Value
import org.springframework.stereotype.Service
import org.springframework.web.client.RestTemplate

@Service
class MlClient(
    private val restTemplate: RestTemplate,
    @Value("\${ml.service.url}") private val mlUrl: String
) {
    private val log = LoggerFactory.getLogger(MlClient::class.java)

    fun classifyInterests(text: String): List<String> {
        return try {
            val response = restTemplate.postForObject(
                "$mlUrl/classify",
                MlClassifyRequest(text),
                MlClassifyResponse::class.java
            )
            response?.interests ?: listOf("другое")
        } catch (e: Exception) {
            log.warn("ML сервис недоступен: ${e.message}")
            listOf("другое")
        }
    }
}
