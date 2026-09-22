package com.example.demo.auth

import org.springframework.context.annotation.Bean
import org.springframework.context.annotation.Configuration
import org.springframework.http.HttpMethod
import org.springframework.security.config.annotation.web.builders.HttpSecurity
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity
import org.springframework.security.config.http.SessionCreationPolicy
import org.springframework.security.web.SecurityFilterChain
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter

@Configuration
@EnableWebSecurity
class SecurityConfig(private val jwtFilter: JwtFilter) {

    @Bean
    fun securityFilterChain(http: HttpSecurity): SecurityFilterChain = http
        .csrf { csrf -> csrf.disable() }
        .cors { }
        .headers { it.frameOptions { f -> f.disable() } }
        .sessionManagement { it.sessionCreationPolicy(SessionCreationPolicy.STATELESS) }
        .authorizeHttpRequests { auth ->
            auth.requestMatchers("/auth/**").permitAll()
            auth.requestMatchers("/error").permitAll()
            auth.requestMatchers("/*.html", "/static/**", "/css/**", "/js/**").permitAll()
            auth.requestMatchers("/h2-console/**").permitAll()
            auth.requestMatchers("/admin/**").hasRole("ADMIN")
            auth.requestMatchers(HttpMethod.POST, "/questions", "/questions/bulk").hasRole("ADMIN")
            auth.requestMatchers(HttpMethod.DELETE, "/questions/**").hasRole("ADMIN")
            auth.anyRequest().authenticated()
        }
        .addFilterBefore(jwtFilter, UsernamePasswordAuthenticationFilter::class.java)
        .build()
}
