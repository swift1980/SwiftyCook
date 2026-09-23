/**
 * Levenshtein edit distance between two strings (case-sensitive; callers
 * should lower-case inputs first if case-insensitive comparison is wanted).
 */
export function levenshteinDistance(a: string, b: string): number {
  const rows = a.length + 1
  const cols = b.length + 1
  const distances: number[][] = Array.from({ length: rows }, () => new Array<number>(cols).fill(0))

  for (let i = 0; i < rows; i++) distances[i][0] = i
  for (let j = 0; j < cols; j++) distances[0][j] = j

  for (let i = 1; i < rows; i++) {
    for (let j = 1; j < cols; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1
      distances[i][j] = Math.min(
        distances[i - 1][j] + 1, // deletion
        distances[i][j - 1] + 1, // insertion
        distances[i - 1][j - 1] + cost // substitution
      )
    }
  }

  return distances[rows - 1][cols - 1]
}

/**
 * Finds the closest name to `name` among `candidates` (case-insensitive,
 * excluding exact matches), for "Did you mean '‹existing name›'?" suggestions
 * before falling through to ingredient creation (Ticket 7).
 *
 * Very short names (<3 chars) are excluded from matching, since a small edit
 * distance is meaningless noise at that length (e.g. "Oil" vs "Oat").
 */
export function findClosestMatch(name: string, candidates: string[], maxDistance = 2): string | null {
  const trimmed = name.trim()
  const lower = trimmed.toLowerCase()
  if (trimmed.length < 3) return null

  let best: { name: string; distance: number } | null = null
  for (const candidate of candidates) {
    const candidateLower = candidate.toLowerCase()
    if (candidateLower === lower || candidate.length < 3) continue

    const distance = levenshteinDistance(lower, candidateLower)
    if (distance === 0 || distance > maxDistance) continue
    if (!best || distance < best.distance) {
      best = { name: candidate, distance }
    }
  }

  return best?.name ?? null
}
