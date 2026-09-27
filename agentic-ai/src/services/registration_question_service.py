from src.agents.registration_question_agent import create_agent
from src.models.registration_question_models import QuestionSuggestionRequest, QuestionSuggestions
from src.services.guest_review_service import ReviewSettings, generate_structured, run_adk


async def generate_questions(context, settings):
    return await run_adk(context, settings, create_agent)


class RegistrationQuestionService:
    def __init__(self, settings=None, generate=generate_questions):
        self.settings = settings or ReviewSettings.from_environment()
        self.settings.validate()
        self.generate = generate

    async def suggest(self, context: QuestionSuggestionRequest) -> QuestionSuggestions:
        return await generate_structured(context, self.settings, self.generate, QuestionSuggestions)


def get_registration_question_service():
    return RegistrationQuestionService()
