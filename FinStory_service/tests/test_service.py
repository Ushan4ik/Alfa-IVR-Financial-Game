import os
import unittest
from unittest.mock import patch

from finstory.api import InterestRequest, LessonRequest, classify_interests, generate_lesson, health
from finstory.agent import StoryAgent
from finstory.interests import InterestModel


class FinStoryServiceTests(unittest.TestCase):
    def test_interest_classifier_is_deterministic(self):
        result = InterestModel().weights("Люблю гонки Formula 1 и музыку")
        self.assertIn("cars", result)
        self.assertIn("music", result)
        self.assertAlmostEqual(1.0, sum(result.values()))

    def test_demo_lesson_contract(self):
        request = LessonRequest(
            requestId="attempt-1",
            topicId="lesson_savings",
            title="Накопления",
            learningGoal="Научиться планировать накопления",
            verifiedMaterial="Раздели оставшуюся сумму на число месяцев.",
            verifiedExample="Для цели нужно откладывать по 800 рублей в месяц.",
            interestText="Люблю музыку",
            terms=["накопительный счёт", "несуществующий термин"],
        )
        with patch.dict(os.environ, {"FINSTORY_DEMO": "1"}, clear=False):
            result = generate_lesson(request)
        self.assertEqual("demo", result.source)
        self.assertEqual("Мира", result.character.name)
        self.assertEqual(1, len(result.terms))
        self.assertIn("800", result.story)
        self.assertGreaterEqual(len(result.story), 650)
        self.assertTrue(result.cheatSheet)

    def test_interest_endpoint_contract(self):
        response = classify_interests(InterestRequest(text="Люблю игры"))
        self.assertEqual("игры", response.primaryInterest)
        self.assertEqual(["игры"], response.interests)

    def test_yandex_failure_uses_long_personalized_fallback(self):
        request = LessonRequest(
            requestId="preview-player-1",
            topicId="lesson_savings",
            title="Накопления",
            learningGoal="Научиться планировать накопления",
            verifiedMaterial="Раздели оставшуюся сумму на число месяцев.",
            verifiedExample="Для цели нужно откладывать по 800 рублей в месяц.",
            interestText="Люблю игры",
        )
        env = {"FINSTORY_DEMO": "0", "YANDEX_API_KEY": "test", "YANDEX_FOLDER_ID": "test"}
        with patch.dict(os.environ, env, clear=False), patch.object(
            StoryAgent, "write", side_effect=RuntimeError("temporary failure")
        ):
            result = generate_lesson(request)
        self.assertEqual("local-personalized", result.source)
        self.assertGreaterEqual(len(result.story), 650)
        self.assertIn("игры", result.story)

    def test_health_does_not_expose_secrets(self):
        with patch.dict(os.environ, {"YANDEX_API_KEY": "secret", "YANDEX_FOLDER_ID": "folder"}, clear=False):
            response = health()
        self.assertTrue(response["yandexConfigured"])
        self.assertNotIn("secret", str(response))


if __name__ == "__main__":
    unittest.main()
