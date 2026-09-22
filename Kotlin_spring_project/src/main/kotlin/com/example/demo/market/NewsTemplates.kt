package com.example.demo.market

// статичный пул новостей на каждый сектор. sentiment: -2 (очень плохо) .. +2 (очень хорошо)
data class NewsTemplate(val sector: Sector, val headline: String, val sentiment: Int)

object NewsTemplates {
    val pool = listOf(
        NewsTemplate(Sector.MINING, "Цены на металлы резко выросли на фоне спроса в Азии", 2),
        NewsTemplate(Sector.MINING, "Авария на крупном руднике остановила добычу", -2),
        NewsTemplate(Sector.MINING, "Новое месторождение обещает рост добычи в следующем году", 1),
        NewsTemplate(Sector.MINING, "Экологические штрафы ударили по горнодобывающим компаниям", -1),

        NewsTemplate(Sector.TECH, "Технологический стартап представил прорывной продукт", 2),
        NewsTemplate(Sector.TECH, "Крупная утечка данных подкосила доверие к IT-компаниям", -2),
        NewsTemplate(Sector.TECH, "Спрос на облачные сервисы продолжает расти", 1),
        NewsTemplate(Sector.TECH, "Новые ограничения на экспорт чипов ударили по сектору", -1),

        NewsTemplate(Sector.ENERGY, "Цены на нефть взлетели из-за сокращения добычи", 2),
        NewsTemplate(Sector.ENERGY, "Авария на электростанции привела к перебоям поставок", -2),
        NewsTemplate(Sector.ENERGY, "Инвестиции в зелёную энергетику набирают обороты", 1),
        NewsTemplate(Sector.ENERGY, "Аномально тёплая зима снизила спрос на энергоносители", -1),

        NewsTemplate(Sector.FINANCE, "Центробанк снизил ключевую ставку — кредиты дешевеют", 2),
        NewsTemplate(Sector.FINANCE, "Крупный банк отчитался об убытках за квартал", -2),
        NewsTemplate(Sector.FINANCE, "Рост доходов населения поддержал банковский сектор", 1),
        NewsTemplate(Sector.FINANCE, "Ужесточение требований к капиталу банков напугало инвесторов", -1),

        NewsTemplate(Sector.RETAIL, "Сезон распродаж принёс ритейлерам рекордную выручку", 2),
        NewsTemplate(Sector.RETAIL, "Падение потребительского спроса ударило по магазинам", -2),
        NewsTemplate(Sector.RETAIL, "Онлайн-продажи ритейлеров продолжают расти", 1),
        NewsTemplate(Sector.RETAIL, "Рост цен на логистику снизил маржу ритейлеров", -1),

        NewsTemplate(Sector.AGRICULTURE, "Богатый урожай обрушил издержки агрокомпаний", 1),
        NewsTemplate(Sector.AGRICULTURE, "Засуха уничтожила часть посевов в этом сезоне", -2),
        NewsTemplate(Sector.AGRICULTURE, "Экспорт зерна вырос на фоне роста мирового спроса", 2),
        NewsTemplate(Sector.AGRICULTURE, "Рост цен на удобрения увеличил издержки фермеров", -1)
    )
}
