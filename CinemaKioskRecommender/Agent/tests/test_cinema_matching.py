from datetime import date, timedelta

import pytest

from cinema_matching import (
    find_best_movie,
    format_session_for_agent,
    format_time_for_speech,
    build_sessions_response,
    match_seat_codes,
    process_ask_day,
    process_ask_time,
    parse_datetime_filter,
    parse_ukrainian_date,
    parse_ukrainian_time,
    resolve_session,
    score_movie_match,
)


MOVIES = [
    {"id": "1", "title": "Стрибунці"},
    {"id": "2", "title": "Mortal Kombat ІІ"},
    {"id": "3", "title": "Мортал Комбат ІІ"},
    {"id": "4", "title": "Психоз: Зламана Реальність"},
    {"id": "5", "title": "Бунтівний дракон"},
]


def test_score_partial_title_match():
    assert score_movie_match("стрибун", "Стрибунці") >= 0.55
    assert score_movie_match("mortal kombat", "Mortal Kombat ІІ") >= 0.55


def test_find_best_movie_from_voice_input():
    match = find_best_movie("стрибунці", MOVIES)
    assert match is not None
    assert match["title"] == "Стрибунці"


def test_find_best_movie_latin_cyrillic():
    match = find_best_movie("mortal kombat 2", MOVIES, threshold=0.45)
    assert match is not None
    assert "Mortal" in match["title"] or "Комбат" in match["title"]


def test_ambiguous_movie_returns_none():
    result = find_best_movie("комбат", MOVIES, threshold=0.3)
    assert result is None


def test_parse_ukrainian_date_today_tomorrow():
    ref = date(2026, 6, 6)
    assert parse_ukrainian_date("сьогодні", ref) == ref
    assert parse_ukrainian_date("завтра", ref) == ref + timedelta(days=1)


def test_parse_ukrainian_date_numeric():
    ref = date(2026, 6, 6)
    assert parse_ukrainian_date("07.06.2026", ref) == date(2026, 6, 7)
    assert parse_ukrainian_date("7 червня", ref) == date(2026, 6, 7)
    assert parse_ukrainian_date("сьомого червня", ref) == date(2026, 6, 7)


def test_parse_ukrainian_time_formats():
    assert parse_ukrainian_time("17:00") == (17, 0)
    assert parse_ukrainian_time("17.00") == (17, 0)
    assert parse_ukrainian_time("9.37") == (9, 37)
    assert parse_ukrainian_time("937") == (9, 37)
    assert parse_ukrainian_time("1430") == (14, 30)
    assert parse_ukrainian_time("5 вечора") == (17, 0)
    assert parse_ukrainian_time("п'ятнадцять нуль") == (15, 0)
    assert parse_ukrainian_time("о дев'ятій тридцять сім") == (9, 37)
    assert parse_ukrainian_time("20") == (20, 0)


def test_format_time_for_speech_uses_ukrainian_words():
    assert format_time_for_speech("17:00") == "о сімнадцятій"
    assert format_time_for_speech("14:30") == "о чотирнадцятій тридцять"


def test_parse_datetime_filter_combined():
    parsed_date, parsed_time = parse_datetime_filter("завтра", "17:00")
    assert parsed_date == date.today() + timedelta(days=1)
    assert parsed_time == (17, 0)


def test_build_sessions_response_ask_date():
    tomorrow = (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T17:00:00Z",
                "hallNumber": 1,
            }
        )
    ]
    response = build_sessions_response(sessions)
    assert response["step"] == "ask_date"
    assert "завтра" in response["date_hints"][0]
    assert "." not in response["date_hints"][0]


def test_build_sessions_response_wrong_day():
    tomorrow = (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T17:00:00Z",
                "hallNumber": 1,
            }
        )
    ]
    response = build_sessions_response(sessions, date_text="сьогодні")
    assert response["step"] == "suggest_tomorrow"
    assert response["suggested_date"] == tomorrow


def test_process_ask_time_selects_session():
    today = date.today().strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s2",
                "startTime": f"{today.split('.')[2]}-{today.split('.')[1]}-{today.split('.')[0]}T09:37:00",
                "hallNumber": 2,
            }
        )
    ]

    day_result = process_ask_day(sessions, "сьогодні")
    assert day_result["step"] == "ask_time"
    assert day_result["available_times"] == ["09:37"]
    assert day_result["times_for_speech"] == ["о дев'ятій тридцять сім"]

    time_result = process_ask_time(sessions, day_result["date"], "9.37")
    assert time_result["success"] is True
    assert time_result["session"]["time"] == "09:37"
    assert time_result["session"]["time_speech"] == "о дев'ятій тридцять сім"
    assert "09:37" not in time_result["message"]


def test_process_ask_time_supports_kiosk_schedule():
    today = date.today().strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "startTime": f"{today.split('.')[2]}-{today.split('.')[1]}-{today.split('.')[0]}T06:37:00",
                "hallNumber": 1,
            }
        ),
        format_session_for_agent(
            {
                "id": "s2",
                "startTime": f"{today.split('.')[2]}-{today.split('.')[1]}-{today.split('.')[0]}T09:37:00",
                "hallNumber": 2,
            }
        ),
        format_session_for_agent(
            {
                "id": "s3",
                "startTime": f"{today.split('.')[2]}-{today.split('.')[1]}-{today.split('.')[0]}T12:37:00",
                "hallNumber": 1,
            }
        ),
    ]

    day_result = process_ask_day(sessions, "сьогодні")
    assert day_result["available_times"] == ["06:37", "09:37", "12:37"]

    assert process_ask_time(sessions, day_result["date"], "937")["success"] is True
    assert process_ask_time(sessions, day_result["date"], "1237")["success"] is True
    assert process_ask_time(sessions, day_result["date"], "637")["success"] is True


def test_process_ask_time_does_not_select_nearest_session():
    tomorrow = (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T06:37:00Z",
                "hallNumber": 1,
            }
        ),
        format_session_for_agent(
            {
                "id": "s2",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T17:00:00Z",
                "hallNumber": 2,
            }
        ),
    ]

    day_result = process_ask_day(sessions, "завтра")
    time_result = process_ask_time(sessions, day_result["date"], "о дев'ятій тридцять сім")

    assert time_result["success"] is False
    assert time_result["step"] == "time_not_found"


def test_process_ask_time_requires_day_first():
    tomorrow = (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T17:00:00Z",
                "hallNumber": 1,
            }
        )
    ]

    result = process_ask_time(sessions, "", "17:00")
    assert result["success"] is False
    assert result["step"] == "need_day_first"


def test_match_seat_codes_from_spoken_labels():
    available = ["A1", "B4", "B10", "C1", "C4", "D1", "E10"]
    matched, rejected = match_seat_codes(["B10", "C4", "D1"], available)
    assert matched == ["B10", "C4", "D1"]
    assert rejected == []

    matched, rejected = match_seat_codes(["B 10", "Z99"], available)
    assert matched == ["B10"]
    assert rejected == ["Z99"]

    matched, rejected = match_seat_codes(["C1, B4 і E10"], available)
    assert matched == ["C1", "B4", "E10"]
    assert rejected == []

    matched, rejected = match_seat_codes(["С1", "Б4", "Е10"], available)
    assert matched == ["C1", "B4", "E10"]
    assert rejected == []

    matched, rejected = match_seat_codes(["сі1", "бі4", "е10"], available)
    assert matched == ["C1", "B4", "E10"]
    assert rejected == []


def test_split_seat_inputs():
    from cinema_matching import split_seat_inputs

    assert split_seat_inputs(["C1, B4, E10"]) == ["C1", "B4", "E10"]
    assert split_seat_inputs(["C1", "B4"]) == ["C1", "B4"]
    assert split_seat_inputs(["С1; Б4"]) == ["C1", "B4"]


def test_resolve_session_by_date_and_time():
    tomorrow = (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
    sessions = [
        format_session_for_agent(
            {
                "id": "s1",
                "movieTitle": "Стрибунці",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T14:00:00Z",
                "hallNumber": 1,
                "price": 150,
            }
        ),
        format_session_for_agent(
            {
                "id": "s2",
                "movieTitle": "Стрибунці",
                "startTime": f"{tomorrow.split('.')[2]}-{tomorrow.split('.')[1]}-{tomorrow.split('.')[0]}T17:00:00Z",
                "hallNumber": 2,
                "price": 180,
            }
        ),
    ]

    resolved = resolve_session(
        sessions,
        date_text="завтра",
        time_text="17:00",
    )
    assert resolved is not None
    assert resolved["time"] == "17:00"
    assert str(resolved["id"]) == "s2"
