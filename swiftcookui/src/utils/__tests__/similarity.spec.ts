import { describe, it, expect } from 'vitest'
import { levenshteinDistance, findClosestMatch } from '@/utils/similarity'

describe('levenshteinDistance', () => {
  it('returns 0 for identical strings', () => {
    expect(levenshteinDistance('tomato', 'tomato')).toBe(0)
  })

  it('counts a single substitution', () => {
    expect(levenshteinDistance('tomato', 'tomate')).toBe(1)
  })

  it('counts a single insertion/deletion', () => {
    expect(levenshteinDistance('tomato', 'tomatoe')).toBe(1)
    expect(levenshteinDistance('tomatoe', 'tomato')).toBe(1)
  })

  it('handles empty strings', () => {
    expect(levenshteinDistance('', 'abc')).toBe(3)
    expect(levenshteinDistance('abc', '')).toBe(3)
  })
})

describe('findClosestMatch', () => {
  it('suggests a close near-duplicate ("Tomatoe" -> "Tomato")', () => {
    expect(findClosestMatch('Tomatoe', ['Tomato', 'Potato', 'Carrot'])).toBe('Tomato')
  })

  it('is case-insensitive', () => {
    expect(findClosestMatch('tomatoe', ['TOMATO'])).toBe('TOMATO')
  })

  it('returns null when the only candidate is an exact match', () => {
    expect(findClosestMatch('Tomato', ['Tomato'])).toBeNull()
  })

  it('returns null when nothing is within the distance threshold', () => {
    expect(findClosestMatch('Tomato', ['Cucumber', 'Pepper'])).toBeNull()
  })

  it('ignores very short names (<3 chars) on either side', () => {
    expect(findClosestMatch('Oi', ['Oil'])).toBeNull()
    expect(findClosestMatch('Oil', ['Oi'])).toBeNull()
  })

  it('picks the closest candidate when multiple are within range', () => {
    expect(findClosestMatch('Tomatoe', ['Tomato', 'Tomatox'])).toBe('Tomato')
  })

  it('respects a custom maxDistance', () => {
    expect(findClosestMatch('Tomatoo', ['Cabbage'], 1)).toBeNull()
    expect(findClosestMatch('Tomatoo', ['Tomato'], 1)).toBe('Tomato')
  })
})
