import { parseIsoDate, todayIso } from '@/utils/dates'

/** "Last made: today / yesterday / N days ago" from the newest log date, or null when never made. */
export function lastMadeText(dates: string[], today: string = todayIso()): string | null {
  if (dates.length === 0) return null
  const newest = [...dates].sort()[dates.length - 1]
  const days = Math.round((parseIsoDate(today).getTime() - parseIsoDate(newest).getTime()) / 86_400_000)
  if (days <= 0) return days < 0 ? `Last made: ${newest}` : 'Last made: today'
  if (days === 1) return 'Last made: yesterday'
  return `Last made: ${days} days ago`
}
