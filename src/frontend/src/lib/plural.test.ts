import { describe, it, expect } from 'vitest'
import { plural } from './plural'

describe('plural', () => {
  it('uses the singular only for exactly one', () => {
    expect(plural(0, 'pack')).toBe('0 packs')
    expect(plural(1, 'pack')).toBe('1 pack')
    expect(plural(18, 'pack')).toBe('18 packs')
  })
})
