import { describe, it, expect } from 'vitest'
import { addDays, mondayOf, toIsoDate } from '@/utils/dates'

describe('dates', () => {
  it('finds the Monday of any weekday, including Sunday', () => {
    expect(mondayOf('2026-10-05')).toBe('2026-10-05') // Monday
    expect(mondayOf('2026-10-07')).toBe('2026-10-05') // Wednesday
    expect(mondayOf('2026-10-11')).toBe('2026-10-05') // Sunday belongs to the previous Monday
    expect(mondayOf('2026-10-12')).toBe('2026-10-12')
  })

  it('adds days across month and year boundaries', () => {
    expect(addDays('2026-12-30', 3)).toBe('2027-01-02')
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28')
  })

  it('formats local dates without timezone shifts', () => {
    expect(toIsoDate(new Date(2026, 0, 5, 0, 30))).toBe('2026-01-05')
  })
})
