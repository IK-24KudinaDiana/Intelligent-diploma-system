import os

import aiohttp
import pytest

from cinema_matching import find_best_movie

API_URL = os.getenv("API_URL", "http://localhost:5146")


async def _api_available() -> bool:
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{API_URL}/api/movies", timeout=aiohttp.ClientTimeout(total=3)) as resp:
                return resp.status == 200
    except Exception:
        return False


@pytest.fixture
async def api_ready():
    if not await _api_available():
        pytest.skip(f"Backend недоступний на {API_URL}")
    return True


@pytest.mark.asyncio
async def test_recommendations_endpoint(api_ready):
    async with aiohttp.ClientSession() as session:
        async with session.get(f"{API_URL}/api/movies/recommendations") as resp:
            assert resp.status == 200
            data = await resp.json()
            assert isinstance(data, list)
            assert len(data) > 0


@pytest.mark.asyncio
async def test_movie_search_fuzzy(api_ready):
    async with aiohttp.ClientSession() as session:
        async with session.get(
            f"{API_URL}/api/movies/search",
            params={"query": "стриб"},
        ) as resp:
            assert resp.status == 200
            data = await resp.json()
            assert len(data) >= 1
            assert any("Стриб" in m["title"] for m in data)


@pytest.mark.asyncio
async def test_agent_movie_matching_flow(api_ready):
    async with aiohttp.ClientSession() as session:
        async with session.get(f"{API_URL}/api/movies/recommendations") as resp:
            recommendations = await resp.json()

        voice_input = "стрибун"
        matched = find_best_movie(voice_input, recommendations, threshold=0.45)
        assert matched is not None

        movie_id = matched["id"]
        async with session.get(f"{API_URL}/api/sessions/movie/{movie_id}") as resp:
            assert resp.status == 200
            sessions = await resp.json()
            assert len(sessions) >= 1
            assert all(s["movieId"] == movie_id for s in sessions)


@pytest.mark.asyncio
async def test_ai_command_queue(api_ready):
    import uuid

    session_id = uuid.uuid4()
    async with aiohttp.ClientSession() as session:
        async with session.post(
            f"{API_URL}/api/ai/{session_id}/command",
            json={
                "command": "select_session",
                "payload": {
                    "time": "17:00",
                    "date": (await _tomorrow_label()),
                },
            },
        ) as resp:
            assert resp.status == 200

        async with session.get(f"{API_URL}/api/ai/{session_id}/commands") as resp:
            assert resp.status == 200
            commands = await resp.json()
            assert len(commands) == 1
            assert commands[0]["command"] == "select_session"


async def _tomorrow_label() -> str:
    from datetime import date, timedelta

    return (date.today() + timedelta(days=1)).strftime("%d.%m.%Y")
