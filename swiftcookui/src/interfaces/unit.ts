export interface UnitDto {
  id: number
  name: string
  pluralName?: string
  description: string
  abbreviation: string
}

export interface UnitCreateDto {
  name: string
  pluralName?: string
  description?: string
  abbreviation?: string
}
