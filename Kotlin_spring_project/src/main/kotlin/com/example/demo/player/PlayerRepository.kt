package com.example.demo.player

import org.springframework.data.jpa.repository.JpaRepository

interface PlayerRepository : JpaRepository<Player, String> {
    fun findByEmail(email: String): Player?

    @org.springframework.data.jpa.repository.Lock(jakarta.persistence.LockModeType.PESSIMISTIC_WRITE)
    @org.springframework.data.jpa.repository.Query("select p from Player p where p.id = :id")
    fun lockById(@org.springframework.data.repository.query.Param("id") id: String): Player?
}

interface PlayerProgressRepository : JpaRepository<PlayerProgress, String>
