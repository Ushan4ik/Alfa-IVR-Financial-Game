package com.example.demo.player

import com.example.demo.market.HoldingRepository
import com.example.demo.market.StockRepository
import org.springframework.transaction.annotation.Transactional
import org.springframework.web.bind.annotation.*

@RestController
@RequestMapping("/admin/players")
class AdminPlayerController(
    private val playerRepository: PlayerRepository,
    private val progressRepository: PlayerProgressRepository,
    private val holdingRepository: HoldingRepository,
    private val stockRepository: StockRepository
) {

    @GetMapping
    fun listPlayers(): List<AdminPlayerResponse> {
        val players = playerRepository.findAll()
        val progressMap = progressRepository.findAll().associateBy { it.playerId }
        val allHoldings = holdingRepository.findAll().filter { it.quantity != 0.0 }.groupBy { it.playerId }
        val stocks = stockRepository.findAll().associateBy { it.id }

        return players.map { player ->
            val progress = progressMap[player.id] ?: PlayerProgress(playerId = player.id)
            val holdings = allHoldings[player.id] ?: emptyList()
            val holdingsValue = holdings.sumOf { h -> (stocks[h.stockId]?.currentPrice ?: 0.0) * h.quantity }
            AdminPlayerResponse(
                id = player.id,
                email = player.email,
                nickname = player.nickname,
                cash = player.cash,
                coins = player.coins,
                level = progress.level,
                finLiteracyScore = progress.finLiteracyScore,
                interests = player.interests.split(",").filter { it.isNotBlank() },
                totalValue = player.cash + holdingsValue
            )
        }.sortedByDescending { it.totalValue }
    }

    @PutMapping("/{id}")
    @Transactional
    fun updatePlayer(@PathVariable id: String, @RequestBody req: AdminUpdatePlayerRequest): AdminPlayerResponse {
        val player = playerRepository.findById(id).orElseThrow { IllegalArgumentException("Игрок не найден") }
        val updatedPlayer = player.copy(
            nickname = req.nickname ?: player.nickname,
            cash = req.cash ?: player.cash,
            coins = req.coins ?: player.coins
        )
        playerRepository.save(updatedPlayer)

        val progress = progressRepository.findById(id).orElseGet { PlayerProgress(playerId = id) }
        progressRepository.save(progress.copy(
            level = req.level ?: progress.level,
            finLiteracyScore = req.finLiteracyScore ?: progress.finLiteracyScore
        ))

        val holdings = holdingRepository.findByPlayerId(id).filter { it.quantity != 0.0 }
        val stocks = stockRepository.findAll().associateBy { it.id }
        val holdingsValue = holdings.sumOf { h -> (stocks[h.stockId]?.currentPrice ?: 0.0) * h.quantity }

        return AdminPlayerResponse(
            id = updatedPlayer.id,
            email = updatedPlayer.email,
            nickname = updatedPlayer.nickname,
            cash = updatedPlayer.cash,
            coins = updatedPlayer.coins,
            level = req.level ?: progress.level,
            finLiteracyScore = req.finLiteracyScore ?: progress.finLiteracyScore,
            interests = updatedPlayer.interests.split(",").filter { it.isNotBlank() },
            totalValue = updatedPlayer.cash + holdingsValue
        )
    }
}
