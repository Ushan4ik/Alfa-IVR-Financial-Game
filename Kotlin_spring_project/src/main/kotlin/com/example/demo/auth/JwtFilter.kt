package com.example.demo.auth

import jakarta.servlet.FilterChain
import jakarta.servlet.http.HttpServletRequest
import jakarta.servlet.http.HttpServletResponse
import com.example.demo.player.PlayerRepository
import org.slf4j.LoggerFactory
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken
import org.springframework.security.core.authority.SimpleGrantedAuthority
import org.springframework.security.core.context.SecurityContextHolder
import org.springframework.stereotype.Component
import org.springframework.web.filter.OncePerRequestFilter

@Component
class JwtFilter(
    private val jwtService: JwtService,
    private val players: PlayerRepository
) : OncePerRequestFilter() {

    private val log = LoggerFactory.getLogger(JwtFilter::class.java)

    override fun doFilterInternal(req: HttpServletRequest, res: HttpServletResponse, chain: FilterChain) {
        val header = req.getHeader("Authorization")
        log.debug(">>> ${req.method} ${req.requestURI} | bearerTokenPresent=${header?.startsWith("Bearer ") == true}")

        if (header != null && header.startsWith("Bearer ")) {
            try {
                val playerId = jwtService.extractPlayerId(header.substring(7))
                val player = players.findById(playerId).orElseThrow()
                val authorities = listOf(SimpleGrantedAuthority("ROLE_${player.role.name}"))
                val auth = UsernamePasswordAuthenticationToken(playerId, null, authorities)
                SecurityContextHolder.getContext().authentication = auth
                log.debug(">>> Authenticated playerId=$playerId role=${player.role}")
            } catch (e: Exception) {
                log.warn(">>> JWT invalid: ${e.message}")
            }
        } else {
            log.debug(">>> No JWT token found")
        }
        chain.doFilter(req, res)
    }
}
