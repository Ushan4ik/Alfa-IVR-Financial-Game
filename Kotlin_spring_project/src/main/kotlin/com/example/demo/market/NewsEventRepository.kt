package com.example.demo.market

import org.springframework.data.jpa.repository.JpaRepository

interface NewsEventRepository : JpaRepository<NewsEvent, String> {
    fun findByDayOrderByCreatedAtDesc(day: Int): List<NewsEvent>
    fun findTop20ByOrderByCreatedAtDesc(): List<NewsEvent>
}
