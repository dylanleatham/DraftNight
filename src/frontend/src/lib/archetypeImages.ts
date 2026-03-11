// Archetype image loading utility using Vite's import.meta.glob

export type Archetype =
  | 'collector'
  | 'cosplay'
  | 'ruleslawyer'
  | 'animefan'
  | 'shark'
  | 'strategist'
  | 'streamer'
  | 'wildcard'

export const ALL_ARCHETYPES: Archetype[] = [
  'collector',
  'cosplay',
  'ruleslawyer',
  'animefan',
  'shark',
  'strategist',
  'streamer',
  'wildcard',
]

// Vite glob import - discovers all images at build time
const modules = import.meta.glob<{ default: string }>(
  '../assets/*/*.{png,jpg,jpeg,webp,gif}',
  { eager: true }
)

// Parse modules into Map<Archetype, string[]>
function buildArchetypeMap(): Map<Archetype, string[]> {
  const map = new Map<Archetype, string[]>()

  for (const archetype of ALL_ARCHETYPES) {
    map.set(archetype, [])
  }

  for (const [path, module] of Object.entries(modules)) {
    // Path looks like: ../assets/shark/portrait.png
    const match = path.match(/\.\.\/assets\/([^/]+)\//)
    if (match) {
      const archetype = match[1] as Archetype
      if (ALL_ARCHETYPES.includes(archetype)) {
        const images = map.get(archetype) || []
        images.push(module.default)
        map.set(archetype, images)
      }
    }
  }

  return map
}

export const archetypeImages: Map<Archetype, string[]> = buildArchetypeMap()

/**
 * Get a random selection of unique archetypes
 */
export function getRandomArchetypes(count: number): Archetype[] {
  const shuffled = [...ALL_ARCHETYPES]
  // Fisher-Yates shuffle
  for (let i = shuffled.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]]
  }
  return shuffled.slice(0, count)
}

/**
 * Get a random image from an archetype's folder, optionally excluding already used images
 * Returns undefined if no images available
 */
export function getRandomImage(
  archetype: Archetype,
  exclude: string[] = []
): string | undefined {
  const images = archetypeImages.get(archetype) || []
  if (images.length === 0) return undefined

  // Filter out excluded images
  const available = images.filter((img) => !exclude.includes(img))

  // If all images have been used, reset and allow repeats
  const pool = available.length > 0 ? available : images

  const randomIndex = Math.floor(Math.random() * pool.length)
  return pool[randomIndex]
}
