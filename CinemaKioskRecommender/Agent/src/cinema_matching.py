
from __future__ import annotations

import re
import unicodedata
from datetime import date, datetime, timedelta
from difflib import SequenceMatcher

_LATIN_TO_CYRILLIC = str.maketrans(
    {
        "a": "а",
        "b": "б",
        "c": "к",
        "e": "е",
        "h": "х",
        "i": "і",
        "k": "к",
        "m": "м",
        "o": "о",
        "p": "п",
        "r": "р",
        "t": "т",
        "u": "у",
        "x": "кс",
        "y": "и",
    }
)

_UA_MONTHS = {
    "січня": 1,
    "лютого": 2,
    "березня": 3,
    "квітня": 4,
    "травня": 5,
    "червня": 6,
    "липня": 7,
    "серпня": 8,
    "вересня": 9,
    "жовтня": 10,
    "листопада": 11,
    "грудня": 12,
}

_UA_MONTHS_BY_NUMBER = {
    1: "січня",
    2: "лютого",
    3: "березня",
    4: "квітня",
    5: "травня",
    6: "червня",
    7: "липня",
    8: "серпня",
    9: "вересня",
    10: "жовтня",
    11: "листопада",
    12: "грудня",
}

_UA_ORDINAL_DAYS = {
    1: "першого",
    2: "другого",
    3: "третього",
    4: "четвертого",
    5: "п'ятого",
    6: "шостого",
    7: "сьомого",
    8: "восьмого",
    9: "дев'ятого",
    10: "десятого",
    11: "одинадцятого",
    12: "дванадцятого",
    13: "тринадцятого",
    14: "чотирнадцятого",
    15: "п'ятнадцятого",
    16: "шістнадцятого",
    17: "сімнадцятого",
    18: "вісімнадцятого",
    19: "дев'ятнадцятого",
    20: "двадцятого",
    21: "двадцять першого",
    22: "двадцять другого",
    23: "двадцять третього",
    24: "двадцять четвертого",
    25: "двадцять п'ятого",
    26: "двадцять шостого",
    27: "двадцять сьомого",
    28: "двадцять восьмого",
    29: "двадцять дев'ятого",
    30: "тридцятого",
    31: "тридцять першого",
}

_UA_WEEKDAYS = {
    "понеділок": 0,
    "вівторок": 1,
    "середа": 2,
    "четвер": 3,
    "пятниця": 4,
    "п'ятниця": 4,
    "субота": 5,
    "неділя": 6,
}

_UA_WEEKDAYS_FOR_SPEECH = {
    0: "у понеділок",
    1: "у вівторок",
    2: "у середу",
    3: "у четвер",
    4: "у п'ятницю",
    5: "у суботу",
    6: "у неділю",
}

_UA_HOURS_FOR_SPEECH = {
    0: "нуль нуль",
    1: "першій",
    2: "другій",
    3: "третій",
    4: "четвертій",
    5: "п'ятій",
    6: "шостій",
    7: "сьомій",
    8: "восьмій",
    9: "дев'ятій",
    10: "десятій",
    11: "одинадцятій",
    12: "дванадцятій",
    13: "тринадцятій",
    14: "чотирнадцятій",
    15: "п'ятнадцятій",
    16: "шістнадцятій",
    17: "сімнадцятій",
    18: "вісімнадцятій",
    19: "дев'ятнадцятій",
    20: "двадцятій",
    21: "двадцять першій",
    22: "двадцять другій",
    23: "двадцять третій",
}

_UA_NUMBERS_FOR_SPEECH = {
    0: "нуль",
    1: "одна",
    2: "дві",
    3: "три",
    4: "чотири",
    5: "п'ять",
    6: "шість",
    7: "сім",
    8: "вісім",
    9: "дев'ять",
    10: "десять",
    11: "одинадцять",
    12: "дванадцять",
    13: "тринадцять",
    14: "чотирнадцять",
    15: "п'ятнадцять",
    16: "шістнадцять",
    17: "сімнадцять",
    18: "вісімнадцять",
    19: "дев'ятнадцять",
    20: "двадцять",
    30: "тридцять",
    40: "сорок",
    50: "п'ятдесят",
}

_UA_NUMBER_WORDS = {
    "нуль": 0,
    "один": 1,
    "одна": 1,
    "два": 2,
    "дві": 2,
    "три": 3,
    "чотири": 4,
    "пять": 5,
    "п'ять": 5,
    "шість": 6,
    "сім": 7,
    "вісім": 8,
    "девять": 9,
    "дев'ять": 9,
    "десять": 10,
    "одинадцять": 11,
    "дванадцять": 12,
    "тринадцять": 13,
    "чотирнадцять": 14,
    "пятнадцять": 15,
    "п'ятнадцять": 15,
    "шістнадцять": 16,
    "сімнадцять": 17,
    "вісімнадцять": 18,
    "девятнадцять": 19,
    "дев'ятнадцять": 19,
    "двадцять": 20,
    "тридцять": 30,
    "сорок": 40,
    "пятдесят": 50,
    "п'ятдесят": 50,
}

_UA_ORDINAL_DAY_TO_NUMBER = {
    normalize: number
    for number, ordinal in _UA_ORDINAL_DAYS.items()
    for normalize in {ordinal, ordinal.replace("'", "")}
}

_NORMALIZED_HOUR_WORDS: dict[str, int] | None = None

_UA_HOUR_WORDS = {
    "один": 1,
    "одна": 1,
    "два": 2,
    "дві": 2,
    "три": 3,
    "чотири": 4,
    "пять": 5,
    "п'ять": 5,
    "шість": 6,
    "сім": 7,
    "вісім": 8,
    "девять": 9,
    "дев'ять": 9,
    "десять": 10,
    "одинадцять": 11,
    "дванадцять": 12,
    "тринадцять": 13,
    "чотирнадцять": 14,
    "пятнадцять": 15,
    "пятнадцать": 15,
    "шістнадцять": 16,
    "сімнадцять": 17,
    "вісімнадцять": 18,
    "девятнадцять": 19,
    "дев'ятнадцять": 19,
    "двадцять": 20,
    "двадцять одна": 21,
    "двадцять дві": 22,
    "двадцять три": 23,
    "першій": 1,
    "другій": 2,
    "третій": 3,
    "четвертій": 4,
    "пятій": 5,
    "п'ятій": 5,
    "шостій": 6,
    "сьомій": 7,
    "восьмій": 8,
    "девятій": 9,
    "дев'ятій": 9,
    "десятій": 10,
    "одинадцятій": 11,
    "дванадцятій": 12,
    "тринадцятій": 13,
    "чотирнадцятій": 14,
    "пятнадцятій": 15,
    "п'ятнадцятій": 15,
    "шістнадцятій": 16,
    "сімнадцятій": 17,
    "вісімнадцятій": 18,
    "девятнадцятій": 19,
    "дев'ятнадцятій": 19,
    "двадцятій": 20,
    "двадцять першій": 21,
    "двадцять другій": 22,
    "двадцять третій": 23,
}

MATCH_THRESHOLD = 0.55


def _get_normalized_hour_words() -> dict[str, int]:
    global _NORMALIZED_HOUR_WORDS
    if _NORMALIZED_HOUR_WORDS is None:
        _NORMALIZED_HOUR_WORDS = {
            normalize_text(phrase): hour for phrase, hour in _UA_HOUR_WORDS.items()
        }
    return _NORMALIZED_HOUR_WORDS


def normalize_text(text: str) -> str:
    if not text:
        return ""

    value = unicodedata.normalize("NFKD", text.casefold())
    value = "".join(ch for ch in value if not unicodedata.combining(ch))
    value = value.translate(_LATIN_TO_CYRILLIC)
    value = re.sub(r"[''`´ʼ]", "", value)
    value = re.sub(r"[^\w\s]", " ", value, flags=re.UNICODE)
    value = re.sub(r"\s+", " ", value).strip()
    return value


def _number_to_ukrainian(value: int) -> str:
    if value in _UA_NUMBERS_FOR_SPEECH:
        return _UA_NUMBERS_FOR_SPEECH[value]

    tens = (value // 10) * 10
    ones = value % 10
    if tens in _UA_NUMBERS_FOR_SPEECH and ones in _UA_NUMBERS_FOR_SPEECH:
        return f"{_UA_NUMBERS_FOR_SPEECH[tens]} {_UA_NUMBERS_FOR_SPEECH[ones]}"

    return str(value)


def _parse_ukrainian_number_words(text: str) -> int | None:
    value = normalize_text(text)
    if not value:
        return None

    tokens = value.split()
    if not tokens:
        return None

    for size in range(min(3, len(tokens)), 1, -1):
        phrase = " ".join(tokens[:size])
        if phrase in _UA_NUMBER_WORDS:
            number = _UA_NUMBER_WORDS[phrase]
            if 0 <= number <= 59:
                return number

    if len(tokens) >= 2 and tokens[0] in _UA_NUMBER_WORDS and tokens[1] in _UA_NUMBER_WORDS:
        tens = _UA_NUMBER_WORDS[tokens[0]]
        ones = _UA_NUMBER_WORDS[tokens[1]]
        if tens in {20, 30, 40, 50} and 0 < ones < 10:
            return tens + ones

    if tokens[0] in _UA_NUMBER_WORDS:
        number = _UA_NUMBER_WORDS[tokens[0]]
        if 0 <= number <= 59:
            return number

    return None


def format_date_for_speech(value: date, reference: date | None = None) -> str:
    ref = reference or date.today()
    day = _UA_ORDINAL_DAYS[value.day]
    month = _UA_MONTHS_BY_NUMBER[value.month]
    weekday = _UA_WEEKDAYS_FOR_SPEECH[value.weekday()]

    if value == ref:
        return f"сьогодні, {day} {month}"
    if value == ref + timedelta(days=1):
        return f"завтра, {day} {month}"

    return f"{weekday}, {day} {month}"


def format_time_for_speech(value: str | tuple[int, int]) -> str:
    if isinstance(value, str):
        parsed = parse_ukrainian_time(value)
        if parsed is None:
            return value
        hour, minute = parsed
    else:
        hour, minute = value

    hour_text = _UA_HOURS_FOR_SPEECH.get(hour, str(hour))
    if minute == 0:
        return f"о {hour_text}"

    minute_text = _number_to_ukrainian(minute)
    return f"о {hour_text} {minute_text}"


def _similarity(a: str, b: str) -> float:
    if not a or not b:
        return 0.0
    if a in b or b in a:
        return 1.0

    seq = SequenceMatcher(None, a, b).ratio()
    a_tokens = set(a.split())
    b_tokens = set(b.split())
    if not a_tokens or not b_tokens:
        return seq

    overlap = len(a_tokens & b_tokens) / max(len(a_tokens), len(b_tokens))
    return max(seq, overlap)


def score_movie_match(user_input: str, title: str) -> float:
    query = normalize_text(user_input)
    candidate = normalize_text(title)
    if not query or not candidate:
        return 0.0

    direct = _similarity(query, candidate)
    query_compact = query.replace(" ", "")
    candidate_compact = candidate.replace(" ", "")
    compact = _similarity(query_compact, candidate_compact)
    return max(direct, compact)


def find_best_movie(
    user_input: str, movies: list[dict], threshold: float = MATCH_THRESHOLD
) -> dict | None:
    if not user_input or not movies:
        return None

    scored = []
    for movie in movies:
        title = movie.get("title") or movie.get("Title") or ""
        score = score_movie_match(user_input, title)
        if score >= threshold:
            scored.append((score, movie))

    if not scored:
        return None

    scored.sort(key=lambda item: item[0], reverse=True)
    best_score, best_movie = scored[0]

    if len(scored) > 1 and scored[1][0] >= best_score - 0.05:
        return None

    return best_movie


def parse_ukrainian_date(
    text: str | None, reference: date | None = None
) -> date | None:
    if not text:
        return None

    ref = reference or date.today()
    value = normalize_text(text)

    if value in {"сьогодні", "today"} or "сьогодні" in value:
        return ref
    if value in {"післязавтра"} or "післязавтра" in value:
        return ref + timedelta(days=2)
    if value in {"завтра", "tomorrow"} or "завтра" in value:
        return ref + timedelta(days=1)

    for weekday_name, weekday_idx in _UA_WEEKDAYS.items():
        if weekday_name in value:
            days_ahead = (weekday_idx - ref.weekday()) % 7
            if days_ahead == 0 and "наступ" not in value:
                days_ahead = 7
            return ref + timedelta(days=days_ahead)

    for fmt in ("%Y-%m-%d", "%d.%m.%Y", "%d.%m.%y", "%d.%m"):
        try:
            parsed = datetime.strptime(text.strip(), fmt)
            year = parsed.year if "%Y" in fmt or "%y" in fmt else ref.year
            return date(year, parsed.month, parsed.day)
        except ValueError:
            continue

    day_month = re.search(r"(\d{1,2})\s*(?:\.|/|-|\s)\s*(\d{1,2})", value)
    if day_month:
        day = int(day_month.group(1))
        month = int(day_month.group(2))
        return date(ref.year, month, day)

    for month_name, month_num in _UA_MONTHS.items():
        match = re.search(rf"(\d{{1,2}})\s+{month_name}", value)
        if match:
            return date(ref.year, month_num, int(match.group(1)))

        for ordinal_name, day_num in _UA_ORDINAL_DAY_TO_NUMBER.items():
            if f"{ordinal_name} {month_name}" in value:
                return date(ref.year, month_num, day_num)

    return None


def _apply_day_part(hour: int, text: str) -> int:
    if hour > 12:
        return hour

    if any(word in text for word in ("вечора", "вечір", "ночі", "ночi")):
        return hour + 12 if hour < 12 else hour
    if any(word in text for word in ("дня", "обід", "полудень")) and hour <= 6:
        return hour + 12
    return hour


def _parse_compact_clock_digits(value: str) -> tuple[int, int] | None:
    digits = re.sub(r"\D", "", value)
    if len(digits) not in {3, 4}:
        return None

    if len(digits) == 3:
        hour = int(digits[0])
        minute = int(digits[1:3])
    else:
        hour = int(digits[:2])
        minute = int(digits[2:4])

    if 0 <= hour <= 23 and 0 <= minute <= 59:
        return hour, minute

    return None


def parse_ukrainian_time(text: str | None) -> tuple[int, int] | None:
    if not text:
        return None

    raw = text.strip().lower().replace("години", "").replace("годину", "")
    normalized = normalize_text(raw)

    compact = None
    if not re.search(r"[а-яіїєґ]", normalized):
        compact = _parse_compact_clock_digits(raw)
    if compact is not None:
        return _apply_day_part(compact[0], normalized), compact[1]

    for fmt in ("%H:%M:%S", "%H:%M"):
        try:
            parsed = datetime.strptime(raw.strip(), fmt)
            return parsed.hour, parsed.minute
        except ValueError:
            continue

    clock = re.search(r"\b(\d{1,2})\s*[:.\-]\s*(\d{2})\b", raw)
    if clock:
        hour = int(clock.group(1))
        minute = int(clock.group(2))
        if 0 <= hour <= 23 and 0 <= minute <= 59:
            return _apply_day_part(hour, normalized), minute

    clock_space = re.search(r"\b(\d{1,2})\s+(\d{2})\b", raw)
    if clock_space:
        hour = int(clock_space.group(1))
        minute = int(clock_space.group(2))
        if 0 <= hour <= 23 and 0 <= minute <= 59:
            return _apply_day_part(hour, normalized), minute

    hour_phrases = sorted(
        _get_normalized_hour_words().items(),
        key=lambda item: len(item[0]),
        reverse=True,
    )
    for phrase, hour in hour_phrases:
        match = re.search(rf"(?<!\w){re.escape(phrase)}(?!\w)", normalized)
        if match:
            after_hour = normalized[match.end():].strip()
            minute = 0
            minute_match = re.search(r"(\d{1,2})\s*(хв|хвилин)", normalized)
            if minute_match:
                minute = int(minute_match.group(1))
            elif after_hour:
                parsed_minute = _parse_ukrainian_number_words(after_hour)
                if parsed_minute is not None:
                    minute = parsed_minute
            return _apply_day_part(hour, normalized), minute

    only_hour = re.search(r"\b(\d{1,2})\b", raw)
    if only_hour:
        hour = int(only_hour.group(1))
        if 0 <= hour <= 23:
            return _apply_day_part(hour, normalized), 0

    return None


def parse_datetime_filter(
    date_text: str | None = None, time_text: str | None = None
) -> tuple[date | None, tuple[int, int] | None]:
    parsed_date = parse_ukrainian_date(date_text)
    parsed_time = parse_ukrainian_time(time_text)

    if date_text and parsed_date is None:
        combined = parse_ukrainian_date(date_text.split()[0] if date_text else None)

        for fmt in ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M"):
            try:
                dt = datetime.strptime(date_text.strip(), fmt)
                return dt.date(), (dt.hour, dt.minute)
            except ValueError:
                continue

        parsed_date = combined

    if time_text and parsed_time is None and " " in time_text:
        maybe_date = parse_ukrainian_date(time_text)
        maybe_time = parse_ukrainian_time(time_text)
        if maybe_date:
            parsed_date = parsed_date or maybe_date
        if maybe_time:
            parsed_time = maybe_time

    return parsed_date, parsed_time


def format_session_for_agent(session: dict) -> dict:
    start_raw = session.get("startTime") or session.get("StartTime") or ""
    start_dt = None
    try:
        start_dt = datetime.fromisoformat(str(start_raw).replace("Z", "+00:00"))
        if start_dt.tzinfo is not None:
            start_dt = start_dt.replace(tzinfo=None)
    except ValueError:
        pass

    if start_dt:
        date_label = start_dt.strftime("%d.%m.%Y")
        time_label = start_dt.strftime("%H:%M")
        weekday_names = [
            "понеділок",
            "вівторок",
            "середа",
            "четвер",
            "п'ятниця",
            "субота",
            "неділя",
        ]
        weekday = weekday_names[start_dt.weekday()]
        date_speech = format_date_for_speech(start_dt.date())
        time_speech = format_time_for_speech((start_dt.hour, start_dt.minute))
    else:
        date_label = ""
        time_label = str(start_raw)
        weekday = ""
        date_speech = ""
        time_speech = str(start_raw)

    hall = session.get("hallNumber") or session.get("HallNumber")
    return {
        "id": session.get("id") or session.get("Id"),
        "movieTitle": session.get("movieTitle") or session.get("MovieTitle"),
        "date": date_label,
        "date_speech": date_speech,
        "weekday": weekday,
        "time": time_label,
        "time_speech": time_speech,
        "hallNumber": hall,
        "price": session.get("price") or session.get("Price"),
        "label": f"{weekday} {date_label}, зал {hall} о {time_label}",
        "label_for_speech": f"{date_speech}, зал {hall}, {time_speech}",
    }


def build_date_hints(date_labels: list[str], reference: date | None = None) -> list[str]:
    ref = reference or date.today()
    hints = []
    for label in date_labels:
        if not label:
            continue
        try:
            parsed = datetime.strptime(label, "%d.%m.%Y").date()
        except ValueError:
            hints.append(label)
            continue

        hints.append(format_date_for_speech(parsed, ref))
    return hints


def build_sessions_response(
    sessions: list[dict],
    date_text: str | None = None,
    time_text: str | None = None,
) -> dict:
    grouped = group_sessions_by_date(sessions)
    available_dates = [d for d in grouped if d]
    date_hints = build_date_hints(available_dates)

    if not date_text:
        return {
            "step": "ask_date",
            "available_dates": available_dates,
            "date_hints": date_hints,
            "message": (
                "Запитай день перегляду. Озвуч лише date_hints. "
                "Не називай час, поки клієнт не обере день."
            ),
        }

    if time_text and not date_text:
        return {
            "step": "need_date_first",
            "date_hints": date_hints,
            "message": "Спочатку уточни день, потім час.",
        }

    target_date, target_time = parse_datetime_filter(date_text, time_text)
    if target_date is None:
        return {
            "step": "clarify_date",
            "date_hints": date_hints,
            "message": f"День незрозумілий. Доступні дати: {', '.join(date_hints)}",
        }

    day_sessions = filter_sessions(sessions, target_date, None)
    if not day_sessions:
        ref = date.today()
        tomorrow = ref + timedelta(days=1)
        tomorrow_label = tomorrow.strftime("%d.%m.%Y")
        tomorrow_sessions = filter_sessions(sessions, tomorrow, None)
        if (
            target_date == ref
            and tomorrow_sessions
            and tomorrow_label in available_dates
        ):
            tomorrow_times = sorted(
                {
                    session.get("time", "")
                    for session in tomorrow_sessions
                    if session.get("time")
                }
            )
            tomorrow_speech = [
                format_time_for_speech(time_label) for time_label in tomorrow_times
            ]
            return {
                "step": "suggest_tomorrow",
                "available_dates": available_dates,
                "date_hints": date_hints,
                "suggested_date": tomorrow_label,
                "suggested_date_speech": format_date_for_speech(tomorrow, ref),
                "available_times": tomorrow_times,
                "times_for_speech": tomorrow_speech,
                "message": (
                    f"На сьогодні сеансів немає. Запропонуй {format_date_for_speech(tomorrow, ref)} "
                    f"і озвуч часи: {', '.join(tomorrow_speech)}. "
                    "Після згоди клієнта виклич ask_day із suggested_date."
                ),
            }

        return {
            "step": "date_not_found",
            "available_dates": available_dates,
            "date_hints": date_hints,
            "message": (
                f"На {format_date_for_speech(target_date)} сеансів немає. "
                f"Запропонуй: {', '.join(date_hints)}"
            ),
        }

    ordered_day_sessions = sorted(day_sessions, key=lambda s: s.get("time", ""))
    available_times = []
    times_for_speech = []
    seen_times = set()
    for session in ordered_day_sessions:
        time_label = session.get("time", "")
        if time_label in seen_times:
            continue
        seen_times.add(time_label)
        available_times.append(time_label)
        times_for_speech.append(session.get("time_speech") or format_time_for_speech(time_label))

    if not time_text:
        return {
            "step": "ask_time",
            "date": target_date.strftime("%d.%m.%Y"),
            "date_speech": format_date_for_speech(target_date),
            "available_times": available_times,
            "times_for_speech": times_for_speech,
            "session_count": len(day_sessions),
            "message": (
                "Озвуч ТІЛЬКИ людські формулювання з times_for_speech. "
                "Не вимовляй available_times і не читай час цифрами. "
                f"Доступно: {', '.join(times_for_speech)}"
            ),
        }

    matched = filter_sessions(sessions, target_date, target_time)
    if not matched:
        return {
            "step": "time_not_found",
            "date": target_date.strftime("%d.%m.%Y"),
            "date_speech": format_date_for_speech(target_date),
            "available_times": available_times,
            "times_for_speech": times_for_speech,
            "message": (
                f"Такого часу немає. Запропонуй лише: {', '.join(times_for_speech)}"
            ),
        }

    return {
        "step": "ready_to_select",
        "date": target_date.strftime("%d.%m.%Y"),
        "date_speech": format_date_for_speech(target_date),
        "available_times": available_times,
        "times_for_speech": times_for_speech,
        "session_count": len(matched),
        "message": "Виклич ask_time з обраним часом.",
    }


def process_ask_day(sessions: list[dict], date_text: str) -> dict:
    return build_sessions_response(sessions, date_text, None)


def process_ask_time(
    sessions: list[dict],
    selected_date: str,
    time_text: str,
) -> dict:
    if not selected_date:
        grouped = group_sessions_by_date(sessions)
        return {
            "success": False,
            "step": "need_day_first",
            "date_hints": build_date_hints([d for d in grouped if d]),
            "message": "Спочатку виклич ask_day з днем перегляду.",
        }

    day_response = build_sessions_response(sessions, selected_date, None)
    times_for_speech = day_response.get("times_for_speech", [])

    normalized_time_text = time_text.strip()
    compact = _parse_compact_clock_digits(normalized_time_text)
    if compact is not None:
        normalized_time_text = f"{compact[0]:02d}:{compact[1]:02d}"

    parsed_time = parse_ukrainian_time(normalized_time_text)
    if parsed_time is None and normalized_time_text != time_text:
        parsed_time = parse_ukrainian_time(time_text)
    if parsed_time is None:
        return {
            "success": False,
            "step": "invalid_time",
            "date": selected_date,
            "available_times": day_response.get("available_times", []),
            "times_for_speech": times_for_speech,
            "message": (
                f"Час не зрозуміло. Доступні сеанси: {', '.join(times_for_speech)}. "
                "Не вигадуй інший час."
            ),
        }

    resolved = resolve_session(
        sessions,
        date_text=selected_date,
        time_text=normalized_time_text,
    )
    if not resolved:
        return {
            "success": False,
            "step": "time_not_found",
            "date": selected_date,
            "times_for_speech": times_for_speech,
            "message": f"Такого часу немає. Доступно: {', '.join(times_for_speech)}",
        }

    return {
        "success": True,
        "step": "session_selected",
        "session": resolved,
        "message": f"Обрано сеанс: {resolved.get('label_for_speech')}",
    }


def _parse_session_clock(session: dict) -> tuple[int, int] | None:
    time_label = session.get("time")
    if not time_label:
        return None

    parsed = parse_ukrainian_time(str(time_label))
    if parsed is not None:
        return parsed

    parts = str(time_label).split(":")
    if len(parts) < 2:
        return None

    try:
        hour = int(parts[0])
        minute = int(parts[1])
    except ValueError:
        return None

    if 0 <= hour <= 23 and 0 <= minute <= 59:
        return hour, minute

    return None


def _session_matches_date(session: dict, target_date: date) -> bool:
    session_date = session.get("date")
    if not session_date:
        return False

    if session_date == target_date.strftime("%d.%m.%Y"):
        return True

    try:
        parsed = datetime.strptime(session_date, "%d.%m.%Y").date()
    except ValueError:
        return False

    return parsed == target_date


def filter_sessions(
    sessions: list[dict],
    target_date: date | None = None,
    target_time: tuple[int, int] | None = None,
) -> list[dict]:
    result = sessions

    if target_date:
        result = [s for s in result if _session_matches_date(s, target_date)]

    if target_time:
        hour, minute = target_time
        return [
            s
            for s in result
            if _parse_session_clock(s) == (hour, minute)
        ]

    return result


def resolve_session(
    sessions: list[dict],
    session_id: str | None = None,
    date_text: str | None = None,
    time_text: str | None = None,
    hall_number: int | None = None,
) -> dict | None:
    if not sessions:
        return None

    if session_id:
        for session in sessions:
            if str(session.get("id")) == str(session_id):
                return session

    target_date, target_time = parse_datetime_filter(date_text, time_text)
    candidates = filter_sessions(sessions, target_date, target_time)

    if hall_number is not None:
        by_hall = [s for s in candidates if s.get("hallNumber") == hall_number]
        if by_hall:
            candidates = by_hall

    if not candidates:
        return None

    if target_time:
        hour, minute = target_time
        time_label = f"{hour:02d}:{minute:02d}"
        exact = next((s for s in candidates if s.get("time") == time_label), None)
        if exact:
            return exact

    return candidates[0]


_ROW_LETTER_TO_LATIN = str.maketrans(
    {
        "А": "A",
        "а": "A",
        "Б": "B",
        "б": "B",
        "В": "B",
        "в": "B",
        "С": "C",
        "с": "C",
        "Д": "D",
        "д": "D",
        "Е": "E",
        "е": "E",
        "Є": "E",
        "є": "E",
    }
)

_UA_ROW_WORDS = {
    "а": "A",
    "ей": "A",
    "б": "B",
    "бе": "B",
    "бі": "B",
    "сі": "C",
    "с": "C",
    "ц": "C",
    "де": "D",
    "д": "D",
    "е": "E",
}

_SEAT_INPUT_SPLIT = re.compile(
    r"[,;]|(?:\s+(?:і|та|and|a|а)\s+)",
    flags=re.IGNORECASE,
)


def _normalize_seat_text(text: str) -> str:
    value = unicodedata.normalize("NFKC", text.strip())
    value = value.translate(_ROW_LETTER_TO_LATIN)

    lower = value.casefold()
    for word, letter in sorted(_UA_ROW_WORDS.items(), key=lambda item: -len(item[0])):
        if lower.startswith(word):
            rest = value[len(word) :].lstrip(" .,-")
            return f"{letter}{rest}"

    return value


def _extract_seat_candidate(raw: str) -> str | None:
    value = _normalize_seat_text(raw)
    if not value:
        return None

    compact = re.sub(r"[^A-Z0-9]", "", value.upper())
    direct_match = re.fullmatch(r"([A-E])(\d{1,2})", compact)
    if direct_match:
        return f"{direct_match.group(1)}{int(direct_match.group(2))}"

    row_num_match = re.match(r"^([A-E])\s+(.+)$", value.upper())
    if row_num_match:
        parsed = _parse_ukrainian_number_words(row_num_match.group(2))
        if parsed is not None and 1 <= parsed <= 10:
            return f"{row_num_match.group(1)}{parsed}"

    loose_match = re.search(r"([A-E])\s*(\d{1,2})", value.upper())
    if loose_match:
        return f"{loose_match.group(1)}{int(loose_match.group(2))}"

    return None


def extract_all_seat_candidates(raw: str) -> list[str]:
    if not raw or not raw.strip():
        return []

    found: list[str] = []
    for part in _SEAT_INPUT_SPLIT.split(raw):
        part = part.strip()
        if not part:
            continue

        candidate = _extract_seat_candidate(part)
        if candidate and candidate not in found:
            found.append(candidate)

    if found:
        return found

    normalized = _normalize_seat_text(raw).upper()
    for match in re.finditer(r"([A-E])\s*(\d{1,2})", normalized):
        candidate = f"{match.group(1)}{int(match.group(2))}"
        if candidate not in found:
            found.append(candidate)

    return found


def split_seat_inputs(raw_inputs: list[str]) -> list[str]:
    expanded: list[str] = []
    for raw in raw_inputs:
        if not raw:
            continue

        candidates = extract_all_seat_candidates(raw)
        if candidates:
            expanded.extend(candidates)
        else:
            expanded.append(raw.strip())

    return expanded


def match_seat_codes(
    raw_inputs: list[str],
    available: list[str],
) -> tuple[list[str], list[str]]:
    raw_inputs = split_seat_inputs(raw_inputs)
    available_map = {seat.upper(): seat for seat in available if seat}
    matched: list[str] = []
    rejected: list[str] = []

    for raw in raw_inputs:
        candidate = _extract_seat_candidate(raw)
        if candidate and candidate in available_map:
            seat = available_map[candidate]
            if seat not in matched:
                matched.append(seat)
            continue

        best_seat = None
        best_score = 0.0
        for seat in available_map.values():
            score = score_movie_match(raw, seat)
            if score > best_score:
                best_score = score
                best_seat = seat

        if best_seat and best_score >= 0.55 and best_seat not in matched:
            matched.append(best_seat)
        else:
            rejected.append(raw)

    return matched, rejected


def group_sessions_by_date(sessions: list[dict]) -> dict[str, list[dict]]:
    grouped: dict[str, list[dict]] = {}
    for session in sessions:
        grouped.setdefault(session.get("date", ""), []).append(session)
    return grouped
