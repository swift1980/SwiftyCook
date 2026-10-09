const pad = (n: number) => String(n).padStart(2, '0')

/** Local calendar date as YYYY-MM-DD (never goes through UTC). */
export function toIsoDate(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export function addDays(iso: string, days: number): string {
  const d = parseIsoDate(iso)
  d.setDate(d.getDate() + days)
  return toIsoDate(d)
}

/** The Monday of the week containing the given date (weeks start on Monday). */
export function mondayOf(iso: string): string {
  const d = parseIsoDate(iso)
  const offset = (d.getDay() + 6) % 7
  return addDays(iso, -offset)
}

export function todayIso(): string {
  return toIsoDate(new Date())
}
