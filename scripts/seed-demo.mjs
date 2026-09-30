#!/usr/bin/env node
// Seeds a demo draft event through the public REST API so you can explore the app
// with realistic data instead of juggling six browser sessions.
//
//   node scripts/seed-demo.mjs                      # 6-player Swiss event, all rounds played, prizes allocated
//   node scripts/seed-demo.mjs --stage=round2       # stop with round 2 in progress
//   node scripts/seed-demo.mjs --stage=lobby        # players joined, not started
//   node scripts/seed-demo.mjs --players=4          # 2–8 players (≤4 = round-robin, ≥5 = Swiss)
//   node scripts/seed-demo.mjs --url=http://localhost:5173
//
// Requires Node 18+ (global fetch). The API must be running (docker compose up, or dotnet run).

const args = Object.fromEntries(
  process.argv.slice(2).map((a) => {
    const [k, v] = a.replace(/^--/, '').split('=')
    return [k, v ?? 'true']
  })
)

const BASE = (args.url ?? 'http://localhost:8080').replace(/\/$/, '')
const STAGE = args.stage ?? 'complete' // lobby | round1 | round2 | ... | complete
const PLAYER_COUNT = Math.min(8, Math.max(2, Number(args.players ?? 6)))
const NAMES = ['Alex', 'Sam', 'Jordan', 'Priya', 'Marcus', 'Riley', 'Casey', 'Morgan']

async function api(method, path, body, headers = {}) {
  const res = await fetch(`${BASE}/api${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...headers },
    body: body ? JSON.stringify(body) : undefined,
  })
  const text = await res.text()
  const json = text ? JSON.parse(text) : null
  if (!res.ok || json?.success === false) {
    throw new Error(`${method} ${path} -> ${res.status}: ${text}`)
  }
  return json
}

const snapshot = (eventId) => api('GET', `/events/${eventId}`)

async function main() {
  const [hostName, ...guests] = NAMES.slice(0, PLAYER_COUNT)

  const created = await api('POST', '/events', {
    name: 'Friday Night Draft',
    packsInBox: 36,
    hostPin: '1234',
    hostName,
  })
  const { eventId, joinCode, hostToken } = created
  const host = { 'X-Host-Token': hostToken }

  for (const name of guests) {
    await api('POST', '/events/join', { joinCode, playerName: name, playerPin: '0000' })
  }

  if (STAGE !== 'lobby') {
    let snap = await snapshot(eventId)
    await api('POST', `/events/${eventId}/start`, { expectedVersion: snap.version }, host)

    const stopAtRound = STAGE.startsWith('round') ? Number(STAGE.slice(5)) : Infinity
    snap = await snapshot(eventId)

    for (let round = 1; round <= snap.totalRounds; round++) {
      snap = await snapshot(eventId)
      const current = snap.rounds.find((r) => r.roundNumber === round)
      if (!current) {
        // Pairings for later rounds are generated when the host publishes them
        await api('POST', `/events/${eventId}/rounds/${round}/publish`, { expectedVersion: snap.version }, host)
        snap = await snapshot(eventId)
      }

      if (round === stopAtRound) {
        // Leave the round in progress, with one result already reported
        const first = snap.rounds.find((r) => r.roundNumber === round).matches.find((m) => !m.isBye && m.playerBId)
        if (first) await finalize(eventId, first, snap.version, host)
        break
      }

      for (const match of snap.rounds.find((r) => r.roundNumber === round).matches) {
        if (match.winnerId || !match.playerBId) continue // BYEs are auto-resolved
        snap = await snapshot(eventId)
        await finalize(eventId, match, snap.version, host)
      }
    }

    if (STAGE === 'complete') {
      snap = await snapshot(eventId)
      await api('POST', `/events/${eventId}/prizes/allocate`, { expectedVersion: snap.version }, host)
    }
  }

  const session = JSON.stringify({ eventId, hostToken, joinCode })
  console.log(`
Seeded "${STAGE}" event with ${PLAYER_COUNT} players.

  Join code : ${joinCode}   (players can join at ${BASE}/join while in the lobby)
  Event     : ${BASE}/event/${eventId}

To view it as the host (${hostName}), open the app, then run this in the browser devtools console and reload:

  localStorage.setItem('draftapp_host_session', '${session}')
`)
}

// Deterministic but varied results: the lower seed wins most matches, with a few upsets.
async function finalize(eventId, match, version, host) {
  // matchCode is "r{round}-m{index}"; the second table of every round produces an upset
  const upset = match.matchCode.endsWith('-m1')
  const winnerId = upset ? match.playerBId : match.playerAId
  await api('POST', `/events/${eventId}/matches/${match.id}/finalize`, { winnerId, expectedVersion: version }, host)
}

main().catch((err) => {
  console.error(err.message)
  process.exit(1)
})
