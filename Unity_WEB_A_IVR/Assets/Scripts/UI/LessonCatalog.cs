using System.Collections.Generic;
using UnityEngine;

namespace IVR.UI
{
    public sealed class LessonDefinition
    {
        public string Key { get; }
        public string Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public string Body { get; }

        public LessonDefinition(string key, string id, string title, string summary, string body)
        {
            Key = key;
            Id = id;
            Title = title;
            Summary = summary;
            Body = body;
        }
    }

    // Контент отделён от UI: списки кнопок строятся автоматически из этого каталога.
    public static class LessonCatalog
    {
        public static readonly IReadOnlyList<LessonDefinition> All = new List<LessonDefinition>
        {
            L("savings", "lesson_savings", "Накопления", "Как приблизиться к денежной цели",
                "Определи сумму и срок цели. Раздели остаток до цели на регулярные взносы. " +
                "Отделяй необходимые расходы от необязательных и храни накопления отдельно."),
            L("deposits", "lesson_deposits", "Вклады", "Срок, проценты и доступ к деньгам",
                "Сравнивай ставку, срок, пополнение, снятие и условия досрочного возврата. " +
                "Перед выбором продукта прочитай договор и ограничения."),
            L("insurance", "lesson_insurance", "Страхование", "Как работает защита от риска",
                "Страхование покрывает только события из договора. Проверь исключения, лимиты выплаты " +
                "и порядок обращения. Страховой взнос не равен гарантированной выплате."),
            L("credits", "lesson_credits", "Кредиты", "Заёмные деньги и ответственность",
                "Кредит возвращают по графику. Оценивай общую сумму выплат, проценты, комиссии и " +
                "посильность платежа, а не только размер первого взноса."),
            L("debit-card", "lesson_debit_card", "Дебетовая карта", "Оплата собственными деньгами",
                "Дебетовая карта связана с банковским счётом. При обычной оплате расходуются собственные " +
                "деньги со счёта, поэтому проверяй сумму операции и остаток."),
            L("inflation", "lesson_inflation", "Инфляция", "Почему на те же деньги покупают меньше",
                "Инфляция — общий рост уровня цен. Если сумма денег не меняется, а цены растут, " +
                "её покупательная способность обычно снижается."),
            L("compound-interest", "lesson_compound_interest", "Сложный процент", "Как проценты приносят проценты",
                "При капитализации начисленные проценты добавляются к основной сумме. В следующем периоде " +
                "процент может начисляться и на ранее начисленный доход."),
            L("investments", "lesson_investments", "Инвестиции", "Доход всегда связан с риском",
                "Инвестиции делают с расчётом на возможный будущий доход, но результат не гарантирован. " +
                "До решения оцени риск, срок и возможные потери."),
            L("shares", "lesson_shares", "Акции", "Доля компании и изменение цены",
                "Акция подтверждает долю участия в компании. Её рыночная цена может расти и снижаться, " +
                "а покупка не гарантирует прибыль или дивиденды."),
            L("bonds", "lesson_bonds", "Облигации", "Долговой инструмент и условия выпуска",
                "Облигация — долговая ценная бумага. До покупки проверь срок возврата, купон, доходность " +
                "и риск эмитента."),
            L("diversification", "lesson_diversification", "Диверсификация", "Зачем распределять вложения",
                "Распределение между разными активами снижает зависимость от результата одного из них. " +
                "Диверсификация не устраняет все риски и не гарантирует доход.")
        };

        private static LessonDefinition L(string key, string id, string title, string summary, string body) =>
            new LessonDefinition(key, id, title, summary, body);

        public static bool IsCompleted(string playerId, string lessonId) =>
            PlayerPrefs.GetInt("ivr_lesson_" + playerId + "_" + lessonId, 0) == 1;

        public static void MarkCompleted(string playerId, string lessonId)
        {
            PlayerPrefs.SetInt("ivr_lesson_" + playerId + "_" + lessonId, 1);
            PlayerPrefs.Save();
        }

        public static int CompletedCount(string playerId)
        {
            int count = 0;
            foreach (var lesson in All)
                if (IsCompleted(playerId, lesson.Id)) count++;
            return count;
        }
    }
}
