package com.example.demo.auth

import com.example.demo.player.PlayerRepository
import com.example.demo.player.PlayerRole
import org.slf4j.LoggerFactory
import org.springframework.beans.factory.annotation.Value
import org.springframework.boot.context.event.ApplicationReadyEvent
import org.springframework.context.event.EventListener
import org.springframework.stereotype.Component
import org.springframework.transaction.annotation.Transactional

/**
 * Назначает роль только уже зарегистрированным аккаунтам из конфигурации.
 * Публичного endpoint повышения роли нет.
 */
@Component
class AdminRoleBootstrap(
    private val players: PlayerRepository,
    @Value("\${security.bootstrap-admin-emails:}") private val configuredEmails: String
) {
    private val log = LoggerFactory.getLogger(javaClass)

    @EventListener(ApplicationReadyEvent::class)
    @Transactional
    fun promoteConfiguredAccounts() {
        val emails = configuredEmails.split(",")
            .map { it.trim().lowercase() }
            .filter { it.isNotBlank() }
            .distinct()

        for (email in emails) {
            val player = players.findByEmail(email)
            if (player == null) {
                log.warn("Configured admin account does not exist: {}", email)
            } else if (player.role != PlayerRole.ADMIN) {
                players.save(player.copy(role = PlayerRole.ADMIN))
                log.info("Granted ADMIN role to configured account: {}", email)
            }
        }
    }
}
