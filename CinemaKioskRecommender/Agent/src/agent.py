import logging
import os
import re
import uuid
from collections.abc import AsyncGenerator, AsyncIterable
from typing import Any

from dotenv import load_dotenv
import aiohttp

from livekit import agents, rtc
from livekit.agents import Agent, AgentSession, JobContext, cli, room_io
from livekit.agents import tts as agents_tts
from livekit.agents.voice import ModelSettings
from livekit.plugins import deepgram, cartesia, silero, openai
from livekit.plugins.turn_detector.multilingual import MultilingualModel

from datetime import date

from cinema_matching import (
    find_best_movie,
    format_session_for_agent,
    group_sessions_by_date,
    build_date_hints,
    match_seat_codes,
    process_ask_day,
    process_ask_time,
)

load_dotenv(".env.local")

logger = logging.getLogger("kinobot")
logger.setLevel(logging.INFO)

API_URL = os.getenv("API_URL", "http://localhost:5146")
GROQ_BASE_URL = "https://api.groq.com/openai/v1"
PRIMARY_GROQ_MODEL = "meta-llama/llama-4-scout-17b-16e-instruct"
FALLBACK_GROQ_MODEL = "llama-3.3-70b-versatile"

def _create_groq_llm(model: str) -> openai.LLM:
    return openai.LLM(
        model=model,
        base_url=GROQ_BASE_URL,
        api_key=os.getenv("GROQ_API_KEY"),
        temperature=0.3,
        max_completion_tokens=220,
    )


def _sanitize_tts_text(text: str) -> str:
    if not text:
        return text

    value = text.replace("—", ", ").replace("–", ", ").replace("−", ", ")
    value = re.sub(r"\s+", " ", value).strip()
    return value


def _create_tts() -> agents_tts.TTS:
    providers: list[agents_tts.TTS] = []

    if os.getenv("CARTESIA_API_KEY"):
        providers.append(
            cartesia.TTS(
                voice=os.getenv(
                    "CARTESIA_VOICE_ID",
                    "05ffab9c-d380-4909-8375-cd12f59238c3",
                ),
                api_key=os.getenv("CARTESIA_API_KEY"),
                language="uk",
                speed=1.2,
                word_timestamps=False,
            )
        )

    if os.getenv("OPENAI_API_KEY"):
        providers.append(
            openai.TTS(
                model=os.getenv("OPENAI_TTS_MODEL", "gpt-4o-mini-tts"),
                voice=os.getenv("OPENAI_TTS_VOICE", "alloy"),
                api_key=os.getenv("OPENAI_API_KEY"),
            )
        )

    if not providers:
        raise RuntimeError(
            "Для голосу потрібен CARTESIA_API_KEY або OPENAI_API_KEY у .env.local"
        )

    if len(providers) == 1:
        return providers[0]

    return agents_tts.FallbackAdapter(providers)

_PHONE_WORDS = {
    "нуль": "0",
    "один": "1",
    "одна": "1",
    "два": "2",
    "дві": "2",
    "три": "3",
    "чотири": "4",
    "пять": "5",
    "п'ять": "5",
    "шість": "6",
    "сім": "7",
    "вісім": "8",
    "девять": "9",
    "дев'ять": "9",
}

_PHONE_TENS = {
    "двадцять": "2",
    "тридцять": "3",
    "сорок": "4",
    "пятдесят": "5",
    "п'ятдесят": "5",
    "шістдесят": "6",
    "сімдесят": "7",
    "вісімдесят": "8",
    "девяносто": "9",
    "дев'яносто": "9",
}


def _phone_fragment_to_digits(value: str) -> str:
    if not value:
        return ""

    direct_digits = re.sub(r"\D", "", value)
    if re.search(r"[а-яіїєґ]", value.lower()):
        direct_digits = ""

    normalized = value.lower()
    normalized = re.sub(r"['`´ʼ]", "'", normalized)
    normalized = re.sub(r"[^\w'\s+]", " ", normalized, flags=re.UNICODE)
    normalized = re.sub(r"\s+", " ", normalized).strip()

    tokens = normalized.split()
    spoken_digits: list[str] = []
    i = 0
    while i < len(tokens):
        token = tokens[i]
        if token in {"плюс", "+"}:
            i += 1
            continue

        if token in _PHONE_TENS:
            if i + 1 < len(tokens) and tokens[i + 1] in _PHONE_WORDS:
                spoken_digits.append(_PHONE_TENS[token] + _PHONE_WORDS[tokens[i + 1]])
                i += 2
                continue
            spoken_digits.append(_PHONE_TENS[token] + "0")
        elif token in _PHONE_WORDS:
            spoken_digits.append(_PHONE_WORDS[token])
        i += 1

    return "".join(spoken_digits) or direct_digits


def _format_phone_for_ui(digits: str) -> str:
    if digits.startswith("0") and len(digits) == 10:
        digits = "38" + digits
    if digits.startswith("380") and len(digits) >= 12:
        return "+" + digits[:12]
    if digits.startswith("38"):
        return "+" + digits[:12]
    return digits[:12]


def _is_complete_ua_phone(digits: str) -> bool:
    normalized = re.sub(r"\D", "", digits)
    if normalized.startswith("0") and len(normalized) == 10:
        normalized = "38" + normalized
    return len(normalized) == 12 and normalized.startswith("380")


class CinemaTools:

    def __init__(self, session_id: uuid.UUID):
        self.session_id = session_id
        self.recommendations_cache: list[dict] = []
        self.sessions_cache: list[dict] = []
        self.selected_movie_id: str | None = None
        self.selected_date: str | None = None
        self.selected_session_id: str | None = None
        self.selected_genres: list[str] = []
        self.phone_digits: str = ""
        self.available_seats_cache: list[str] = []
        self.pending_seats: list[str] = []
        self.seats_confirmed: bool = False

    async def _send_command(self, command: str, payload: dict | None = None):
        body = {
            "command": command,
            "payload": payload or {},
        }

        try:
            async with aiohttp.ClientSession() as http:
                async with http.post(
                    f"{API_URL}/api/ai/{self.session_id}/command",
                    json=body,
                ) as resp:
                    text = await resp.text()
                    logger.info(f"POST /command [{command}] => {resp.status} | {text}")
                    return {"success": resp.status == 200, "status": resp.status}
        except Exception as ex:
            logger.exception(f"{command} failed: {ex}")
            return {"success": False, "error": str(ex)}

    async def _ensure_recommendations_loaded(self) -> None:
        if self.recommendations_cache:
            return

        await self.get_recommendations()

    async def _find_movie(self, movie_value: str) -> tuple[str | None, str | None]:
        await self._ensure_recommendations_loaded()

        if not self.recommendations_cache:
            return None, None

        try:
            uuid.UUID(movie_value)
            for movie in self.recommendations_cache:
                movie_id = str(movie.get("id") or movie.get("Id"))
                if movie_id == movie_value:
                    title = movie.get("title") or movie.get("Title")
                    return movie_id, title
        except ValueError:
            pass

        cached = find_best_movie(movie_value, self.recommendations_cache, threshold=0.38)
        if cached:
            movie_id = str(cached.get("id") or cached.get("Id"))
            title = cached.get("title") or cached.get("Title")
            logger.info(f"✅ Matched from recommendations cache: {title} ({movie_id})")
            return movie_id, title

        return None, None

    async def _load_sessions(self) -> dict | None:
        if not self.selected_movie_id:
            return {
                "error": "Спочатку обери фільм через select_movie.",
            }

        if self.sessions_cache:
            return None

        try:
            async with aiohttp.ClientSession() as http:
                async with http.get(
                    f"{API_URL}/api/sessions/movie/{self.selected_movie_id}"
                ) as resp:
                    if resp.status != 200:
                        return {"error": f"Не вдалося завантажити сеанси: {resp.status}"}

                    data = await resp.json()
                    self.sessions_cache = [
                        format_session_for_agent(session) for session in data
                    ]
                    logger.info(
                        "✅ Sessions loaded: %s items for movie %s",
                        len(self.sessions_cache),
                        self.selected_movie_id,
                    )
                    return None
        except Exception as ex:
            logger.exception(ex)
            return {"error": str(ex)}

    def _session_date_hints(self) -> dict:
        grouped = group_sessions_by_date(self.sessions_cache)
        dates = [d for d in grouped if d]
        hints = build_date_hints(dates)
        return {
            "date_hints": hints,
            "available_dates": dates,
            "message": (
                "Запитай: «На який день?» і озвуч date_hints. "
                "Після відповіді клієнта виклич ask_day."
            ),
        }

    def _ensure_selected_date(self) -> str | None:
        if self.selected_date:
            return self.selected_date

        dates = sorted({s.get("date") for s in self.sessions_cache if s.get("date")})
        if not dates:
            return None

        today_label = date.today().strftime("%d.%m.%Y")
        if today_label in dates:
            self.selected_date = today_label
            return self.selected_date

        if len(dates) == 1:
            self.selected_date = dates[0]
            return self.selected_date

        return None

    @agents.llm.function_tool
    async def select_genres(self, genres: list[str]):
        logger.info(f"AI COMMAND => select_genres | session_id={self.session_id} | genres={genres}")

        for genre in genres:
            if genre and genre not in self.selected_genres:
                self.selected_genres.append(genre)

        return await self._send_command("select_genres", {"genres": genres})

    @agents.llm.function_tool
    async def proceed_to_recommendations(self):
        logger.info(f"AI COMMAND => proceed_to_recommendations | session_id={self.session_id}")

        ui_result = await self._send_command("proceed_to_recommendations")
        recommendations = await self.get_recommendations()

        if recommendations.get("error"):
            return {
                "success": False,
                "ui_updated": ui_result.get("success", False),
                **recommendations,
            }

        return {
            "success": True,
            "ui_updated": ui_result.get("success", False),
            **recommendations,
        }

    @agents.llm.function_tool
    async def get_recommendations(self, genres: list[str] | None = None):
        """Отримати рекомендації фільмів за обраними жанрами."""
        genre_list = genres or self.selected_genres
        logger.info(f"AI COMMAND => get_recommendations | genres={genre_list}")

        try:
            params = {}
            if genre_list:
                params["genres"] = ",".join(genre_list)

            async with aiohttp.ClientSession() as http:
                async with http.get(
                    f"{API_URL}/api/movies/recommendations",
                    params=params,
                ) as resp:
                    if resp.status == 200:
                        data = await resp.json()
                        raw_list = data if isinstance(data, list) else []
                        deduped: list[dict] = []
                        seen_ids: set[str] = set()
                        for movie in raw_list:
                            movie_id = str(movie.get("id") or movie.get("Id") or "")
                            if not movie_id or movie_id in seen_ids:
                                continue
                            seen_ids.add(movie_id)
                            deduped.append(movie)

                        self.recommendations_cache = deduped
                        logger.info(
                            f"✅ Recommendations received: {len(self.recommendations_cache)} items (cached)"
                        )
                        titles = [
                            m.get("title") or m.get("Title") or ""
                            for m in self.recommendations_cache[:3]
                        ]
                        selection_titles = [
                            m.get("title") or m.get("Title") or ""
                            for m in self.recommendations_cache
                            if m.get("title") or m.get("Title")
                        ]
                        await self._send_command(
                            "sync_recommendations",
                            {
                                "movie_ids": [
                                    str(m.get("id") or m.get("Id"))
                                    for m in self.recommendations_cache
                                ],
                                "titles": titles,
                            },
                        )
                        return {
                            "titles_for_speech": titles,
                            "titles_for_selection": selection_titles,
                            "recommendations_count": len(self.recommendations_cache),
                            "message": (
                                "Озвуч лише titles_for_speech. "
                                "Для вибору користувача дозволені всі titles_for_selection. "
                                "Якщо користувач просить ще варіанти, назви наступні з titles_for_selection."
                            ),
                        }

                    text = await resp.text()
                    logger.error(f"Recommendations failed. Status={resp.status} | {text}")
                    return {"error": f"Сервер повернув {resp.status}", "fallback": True}
        except Exception as ex:
            logger.exception(ex)
            return {"error": str(ex)}

    @agents.llm.function_tool
    async def select_movie(self, movie_value: str):
        """Вибрати фільм за назвою зі списку рекомендацій або за ID."""
        logger.info(f"select_movie: {movie_value}")

        try:
            movie_id, title = await self._find_movie(movie_value)

            if movie_id is None:
                logger.info(f"⚠️ Фільм не знайдено: {movie_value}")
                available_titles = [
                    m.get("title") or m.get("Title")
                    for m in self.recommendations_cache
                    if m.get("title") or m.get("Title")
                ]
                return {
                    "success": False,
                    "movie_value": movie_value,
                    "available_titles": available_titles,
                    "message": (
                        "Фільм не знайдено. Не переходь до сеансів. "
                        "Попроси повторити назву або вибрати одну з available_titles."
                    ),
                }

            self.selected_movie_id = movie_id
            self.sessions_cache = []
            self.selected_date = None
            self.selected_session_id = None
            await self._load_sessions()

            await self._send_command(
                "select_movie",
                {
                    "movie_id": movie_id,
                    "title": title,
                    "movie_name": title,
                    "original_input": movie_value,
                },
            )

            session_hints = self._session_date_hints()

            return {
                "success": movie_id is not None,
                "movie_id": movie_id,
                "title": title,
                **session_hints,
                "message": (
                    f"Вибрано: {title}. Запитай день і озвуч date_hints. "
                    "Після відповіді виклич ask_day."
                    if movie_id
                    else f"Не знайдено: {movie_value}"
                ),
            }

        except Exception as e:
            logger.exception(e)
            return {
                "success": False,
                "error": str(e),
                "title": movie_value,
            }

    @agents.llm.function_tool
    async def ask_day(self, date_text: str):
        """Зафіксувати день перегляду. Один аргумент: сьогодні, завтра або дата."""
        logger.info(f"AI COMMAND => ask_day | date={date_text}")

        error = await self._load_sessions()
        if error:
            return error

        response = process_ask_day(self.sessions_cache, date_text)

        if response.get("step") == "ask_time":
            self.selected_date = response.get("date")
        elif response.get("step") in {"date_not_found", "suggest_tomorrow"}:
            self.selected_date = None

        return response

    @agents.llm.function_tool
    async def ask_time(self, time_text: str):
        """Обрати час сеансу. Один аргумент: наприклад 17:00 або п'ятнадцята."""
        logger.info(f"AI COMMAND => ask_time | time={time_text}")

        error = await self._load_sessions()
        if error:
            return error

        if not self._ensure_selected_date():
            return {
                "success": False,
                "step": "need_day_first",
                **self._session_date_hints(),
            }

        result = process_ask_time(
            self.sessions_cache,
            self.selected_date,
            time_text,
        )

        if not result.get("success"):
            return result

        resolved = result["session"]
        self.selected_session_id = str(resolved["id"])
        self.selected_date = resolved["date"]

        payload = {
            "session_id": self.selected_session_id,
            "time": resolved["time"],
            "start_time": resolved["time"],
            "date": resolved["date"],
            "hall_number": resolved.get("hallNumber"),
        }

        ui_result = await self._send_command("select_session", payload)

        return {
            "success": True,
            "session_id": self.selected_session_id,
            "date": resolved["date"],
            "time": resolved["time"],
            "ui_updated": ui_result.get("success", False),
            "message": result["message"],
        }

    @agents.llm.function_tool
    async def get_available_seats(self):
        """Отримати вільні місця для обраного сеансу. Без параметрів."""
        logger.info("AI COMMAND => get_available_seats")

        if not self.selected_session_id:
            return {"error": "Спочатку обери час через ask_time."}

        try:
            async with aiohttp.ClientSession() as http:
                async with http.get(
                    f"{API_URL}/api/sessions/{self.selected_session_id}/seats"
                ) as resp:
                    if resp.status == 200:
                        seats = await resp.json()
                        available = [
                            s.get("seatNumber") or s.get("SeatNumber") or s.get("number") or s.get("Number")
                            for s in seats
                            if not (
                                s.get("isBooked")
                                or s.get("IsBooked")
                                or s.get("isOccupied")
                                or s.get("IsOccupied")
                            )
                        ]
                        available = [seat for seat in available if seat]
                        self.available_seats_cache = available
                        return {
                            "available_seat_count": len(available),
                            "available_seats": available[:30],
                            "sample_available_seats": available[:20],
                            "message": (
                                "Скажи кількість вільних місць і попроси назвати місця з екрана "
                                "(наприклад B10, C4). Не вигадуй місця."
                            ),
                        }
                    return {"error": "Не вдалося отримати місця"}
        except Exception as ex:
            logger.exception(ex)
            return {"error": str(ex)}

    async def _ensure_available_seats(self) -> list[str]:
        if self.available_seats_cache:
            return self.available_seats_cache

        response = await self.get_available_seats()
        if response.get("error"):
            return []
        return self.available_seats_cache

    @agents.llm.function_tool
    async def select_seats(self, seats: list[str]):
        """Вибрати одне або кілька місць (наприклад: A1, B10 або «C1, B4 і E10»)."""
        if isinstance(seats, str):
            seats = [seats]

        logger.info(f"AI COMMAND => select_seats | seats={seats}")

        available = await self._ensure_available_seats()
        if not available:
            return {
                "success": False,
                "message": "Спочатку виклич get_available_seats.",
            }

        matched, rejected = match_seat_codes(seats, available)
        if not matched:
            return {
                "success": False,
                "requested_seats": seats,
                "rejected_seats": rejected,
                "available_seats": available[:30],
                "message": (
                    "Не вдалося розпізнати місця. Попроси повторити коди з екрана "
                    f"або обрати з available_seats."
                ),
            }

        for seat in matched:
            if seat not in self.pending_seats:
                self.pending_seats.append(seat)
        self.pending_seats = self.pending_seats[:8]
        self.seats_confirmed = False

        result = await self._send_command("select_seats", {"seats": self.pending_seats})
        return {
            "success": result.get("success", False),
            "selected_seats": self.pending_seats,
            "rejected_seats": rejected,
            "message": (
                f"Обрано: {', '.join(self.pending_seats)}. "
                "Озвуч ці місця і спитай: «Підтверджуєте ці місця?» "
                "Після явного «так» виклич confirm_seats."
            ),
        }

    @agents.llm.function_tool
    async def confirm_seats(self):
        """Підтвердити вибрані місця після згоди користувача."""
        logger.info("AI COMMAND => confirm_seats")

        if not self.pending_seats:
            return {
                "success": False,
                "message": "Спочатку виклич select_seats і дочекайся назв місць.",
            }

        self.seats_confirmed = True
        return {
            "success": True,
            "confirmed_seats": self.pending_seats,
            "message": (
                f"Місця підтверджено: {', '.join(self.pending_seats)}. "
                "Тепер спитай: «Переходимо до оплати?»"
            ),
        }

    @agents.llm.function_tool
    async def proceed_to_payment(self):
        """Перейти до оплати після підтвердження місць."""
        logger.info("AI COMMAND => proceed_to_payment")

        if not self.seats_confirmed or not self.pending_seats:
            return {
                "success": False,
                "need_seat_confirmation": True,
                "pending_seats": self.pending_seats,
                "message": (
                    "Спочатку select_seats, озвуч місця і виклич confirm_seats "
                    "після явного «так» від користувача."
                ),
            }

        return await self._send_command("proceed_to_payment")

    @agents.llm.function_tool
    async def set_phone_number(self, phone: str):
        """Додати або встановити номер телефону для квитка. Можна викликати кілька разів частинами."""
        logger.info(f"AI COMMAND => set_phone_number | phone={phone}")
        fragment = _phone_fragment_to_digits(phone)
        if not fragment:
            return {
                "success": False,
                "complete": False,
                "phone_digits_count": len(self.phone_digits),
                "message": "Не розпізнав цифри. Попроси повторити цю частину номера.",
            }

        if fragment in {"", "+"}:
            return {
                "success": False,
                "complete": False,
                "phone_digits_count": len(self.phone_digits),
                "message": "Не розпізнав цифри. Попроси повторити цю частину номера.",
            }

        if not self.phone_digits:
            self.phone_digits = fragment
        elif fragment.startswith(self.phone_digits):
            self.phone_digits = fragment
        elif self.phone_digits in {"38", "380"} and fragment.startswith("38"):
            self.phone_digits = fragment
        else:
            self.phone_digits += fragment

        if self.phone_digits.startswith("380") and len(self.phone_digits) > 12:
            self.phone_digits = self.phone_digits[:12]
        elif self.phone_digits.startswith("38") and len(self.phone_digits) > 12:
            self.phone_digits = self.phone_digits[:12]
        elif len(self.phone_digits) > 10 and self.phone_digits.startswith("0"):
            self.phone_digits = self.phone_digits[:10]

        phone_for_ui = _format_phone_for_ui(self.phone_digits)
        complete = _is_complete_ua_phone(self.phone_digits)
        if not complete and len(re.sub(r"\D", "", phone_for_ui)) < 4:
            return {
                "success": False,
                "complete": False,
                "phone_digits_count": len(re.sub(r"\D", "", phone_for_ui)),
                "message": "Номер ще неповний. Попроси продиктувати цифри по одній або групами.",
            }

        result = await self._send_command("set_phone_number", {"phone": phone_for_ui})

        return {
            "success": result.get("success", False),
            "complete": complete,
            "phone_digits_count": len(re.sub(r"\D", "", phone_for_ui)),
            "phone_masked": phone_for_ui[:-4] + "****" if complete else phone_for_ui,
            "message": (
                "Номер повний. Попроси підтвердити оплату."
                if complete
                else "Номер ще неповний. Попроси продиктувати решту цифр."
            ),
        }

    @agents.llm.function_tool
    async def confirm_purchase(self):
        """Підтвердити оплату та завершити покупку квитка."""
        logger.info("AI COMMAND => confirm_purchase")
        phone_for_ui = _format_phone_for_ui(self.phone_digits)
        if not _is_complete_ua_phone(self.phone_digits):
            return {
                "success": False,
                "complete_phone_required": True,
                "phone_digits_count": len(re.sub(r"\D", "", phone_for_ui)),
                "message": (
                    "Спочатку потрібен повний номер у форматі +380XXXXXXXXX. "
                    "Не вигадуй цифри."
                ),
            }

        result = await self._send_command("confirm_purchase")
        return {
            "success": result.get("success", False),
            "message": (
                "Квиток оформлено! Запитай: «Чи потрібно роздрукувати квиток?»"
            ),
        }

    @agents.llm.function_tool
    async def print_ticket(self):
        """Роздрукувати квиток на кіоску після підтвердження користувача."""
        logger.info("AI COMMAND => print_ticket")
        result = await self._send_command("print_ticket")
        return {
            "success": result.get("success", False),
            "message": "Друк квитка розпочато.",
        }


class KinoBot(Agent):

    def __init__(self, tools) -> None:
        super().__init__(
            instructions="""Ти — КіноБот, голосовий помічник кінотеатру.
Говори тільки українською, 1-2 короткі речення. Став крапки між діями, щоб у мовленні були невеликі паузи.
Не озвучуй ID, JSON, ключі полів, англійські службові назви, "17:00" чи "07.06.2026". Для дат і часу кажи природно: сьогодні, завтра, сьомого червня, о сімнадцятій.
Якщо tool повертає date_speech, time_speech, label_for_speech або times_for_speech — для голосу використовуй тільки ці поля.

Старт: "Привіт! Я КіноБот, ваш помічник у кінотеатрі. Які жанри фільмів вам подобаються?"
Жанри передавай англійською: Action, Adventure, Comedy, Drama, SciFi, Horror, Thriller, Romance, Animation, Documentary, Fantasy.

Сценарій:
1. Жанри. Коли користувач називає жанри, переклади їх і виклич select_genres. Нові жанри додавай до вже обраних. Після цього коротко підтвердь українською і спитай, чи це все. proceed_to_recommendations викликай тільки на явне "це все", "достатньо", "ні", "далі", "готово".
2. Рекомендації. proceed_to_recommendations сам завантажує рекомендації. Озвуч ТІЛЬКИ titles_for_speech або titles_for_selection — ніколи не вигадуй інші фільми. На прохання "ще" назви наступні з titles_for_selection. Коли користувач назве фільм, виклич select_movie. Якщо success=false, попроси повторити або вибрати з available_titles.
3. Сеанс. Після select_movie запитай день і озвуч date_hints. Потім ask_day(date_text). Якщо step=suggest_tomorrow, запропонуй suggested_date_speech і після згоди виклич ask_day із suggested_date. Озвуч ТІЛЬКИ times_for_speech. Потім ask_time(time_text) — передавай час цифрами як сказав користувач: 6:37, 9:37, 12:37, 937, 1237. Не перекладай час українськими словами сам. Якщо ask_time неуспішний, повтори times_for_speech і не вигадуй інший час. Якщо успішний, виклич get_available_seats.
4. Місця. Скажи кількість вільних місць і попроси назвати місця з екрана (формат: B10, C4). Якщо користувач називає кілька місць одразу — передай їх у select_seats одним списком (наприклад ["C1", "B4", "E10"]). Виклич select_seats лише з тими кодами, що сказав користувач — не вигадуй. Озвуч selected_seats і спитай: «Підтверджуєте ці місця?» Після явного «так» виклич confirm_seats. proceed_to_payment — тільки після confirm_seats і ще одного «так» на оплату.
5. Оплата. Спочатку proceed_to_payment. Потім запитай номер телефону цифрами (+380...). Кожну фразу передавай у set_phone_number без вигаданих цифр. Якщо complete=false, проси решту. confirm_purchase — тільки коли complete=true і користувач явно сказав «так» на оплату. Ніколи не викликай confirm_purchase до повного номера.
6. Після покупки. Після успішного confirm_purchase спитай, чи друкувати квиток. На "так" виклич print_ticket. На "ні" подякуй і заверши.

Веди користувача послідовно. Не перескакуй через кроки.""",
            tools=tools
        )

    def tts_node(
        self, text: AsyncIterable[str], model_settings: ModelSettings
    ) -> (
        AsyncIterable[rtc.AudioFrame]
        | Any
    ):
        async def sanitized() -> AsyncGenerator[str, None]:
            async for chunk in text:
                if chunk:
                    yield _sanitize_tts_text(chunk)

        return Agent.default.tts_node(self, sanitized(), model_settings)


async def entrypoint(ctx: JobContext):
    await ctx.connect()

    room_name = ctx.room.name
    try:
        session_id = uuid.UUID(room_name)
        logger.info(f"Using session_id from LiveKit room: {session_id}")
    except ValueError:
        session_id = uuid.uuid4()
        logger.warning(f"Could not parse room name as UUID, generated new: {session_id}")

    logger.info(f"Starting new KinoBot session with id: {session_id}")

    cinema_tools = CinemaTools(session_id)

    agent_session = AgentSession(
        stt=deepgram.STT(
            model="nova-3",
            language="uk",
            api_key=os.getenv("DEEPGRAM_API_KEY")
        ),
        llm=agents.llm.FallbackAdapter(
            llm=[               
                _create_groq_llm(PRIMARY_GROQ_MODEL),
                _create_groq_llm(FALLBACK_GROQ_MODEL),
            ],
            attempt_timeout=8.0,
            max_retry_per_llm=0,
            retry_interval=0.5,
        ),
        tts=_create_tts(),
        vad=silero.VAD.load(),
        turn_detection=MultilingualModel(),
        preemptive_generation=False,
    )

    await agent_session.start(
        agent=KinoBot(tools=[
            cinema_tools.select_genres,
            cinema_tools.proceed_to_recommendations,
            cinema_tools.select_movie,
            cinema_tools.ask_day,
            cinema_tools.ask_time,
            cinema_tools.get_available_seats,
            cinema_tools.select_seats,
            cinema_tools.confirm_seats,
            cinema_tools.proceed_to_payment,
            cinema_tools.set_phone_number,
            cinema_tools.confirm_purchase,
            cinema_tools.print_ticket,
        ]),
        room=ctx.room,
        room_options=room_io.RoomOptions(
            audio_input=room_io.AudioInputOptions(
                noise_cancellation=True,
            ),
        ),
    )

    await agent_session.generate_reply(
        instructions=(
            "Привіт! Представся як КіноБот одним коротким реченням "
            "і запитай про жанри, як у стартовій інструкції."
        )
    )


if __name__ == "__main__":
    cli.run_app(
        agents.WorkerOptions(entrypoint_fnc=entrypoint)
    )
