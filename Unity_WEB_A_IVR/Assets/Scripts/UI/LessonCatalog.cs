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

    // Контент отделён от UI: новые уроки добавляются одной записью.
    public static class LessonCatalog
    {
        public static readonly IReadOnlyList<LessonDefinition> All = new List<LessonDefinition>
        {
            new LessonDefinition(
                "savings", "lesson_savings", "Накопления", "Как приблизиться к денежной цели",
                "Накопление начинается с конкретной цели: что нужно купить и сколько для этого требуется. " +
                "Раздели нужную сумму на несколько небольших регулярных взносов. " +
                "Так проще видеть прогресс и не тратить всё сразу. " +
                "Перед покупкой полезно сравнить цену с целью: действительно ли она важнее запланированного?"),
            new LessonDefinition(
                "deposits", "lesson_deposits", "Вклады", "Что означают срок и проценты",
                "Вклад — это деньги, размещённые в банке на определённых условиях. " +
                "При сравнении вкладов смотри на срок, процентную ставку и правила пополнения и снятия. " +
                "Итоговая сумма зависит от этих условий. Перед выбором продукта всегда читай его договор и ограничения."),
            new LessonDefinition(
                "insurance", "lesson_insurance", "Страхование", "Как работает защита от риска",
                "Страхование помогает покрыть последствия событий, которые перечислены в договоре. " +
                "Человек платит страховой взнос, а страховая компания принимает на себя указанные риски. " +
                "Важно понимать, какие события покрываются, какие исключены и как подать заявление при наступлении случая."),
            new LessonDefinition(
                "credits", "lesson_credits", "Кредиты", "Заёмные деньги и ответственность",
                "Кредит позволяет получить деньги сейчас и вернуть их позже по графику. " +
                "Обычно возврат включает не только взятую сумму, но и плату за пользование деньгами. " +
                "Перед оформлением важно оценить весь размер платежей, срок и способность платить вовремя.")
        };

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
