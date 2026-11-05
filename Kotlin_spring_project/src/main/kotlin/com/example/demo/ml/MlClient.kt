package com.example.demo.ml

import org.slf4j.LoggerFactory
import org.springframework.beans.factory.annotation.Value
import org.springframework.http.HttpEntity
import org.springframework.http.HttpHeaders
import org.springframework.http.MediaType
import org.springframework.stereotype.Service
import org.springframework.web.client.RestTemplate

@Service
class MlClient(
    private val restTemplate: RestTemplate,
    @Value("\${ml.service.url}") private val mlUrl: String,
    @Value("\${learning.finstory.token:}") private val token: String
) {
    private val log = LoggerFactory.getLogger(MlClient::class.java)

    fun classifyInterests(text: String): List<String> {
        return try {
            val headers = HttpHeaders()
            headers.contentType = MediaType.APPLICATION_JSON
            if (token.isNotBlank()) headers.set("X-Internal-Token", token)
            val response = restTemplate.postForObject("${mlUrl.trimEnd('/')}/internal/v1/interests/classify",
                HttpEntity(MlClassifyRequest(text), headers), MlClassifyResponse::class.java)
            response?.interests ?: listOf("другое")
        } catch (e: Exception) {
            log.warn("ML сервис недоступен: ${e.message}")
            listOf("другое")
        }
    }
}
