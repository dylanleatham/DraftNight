/** "1 pack", "3 packs". Only for regular English plurals. */
export function plural(count: number, noun: string): string {
  return `${count} ${count === 1 ? noun : `${noun}s`}`
}
