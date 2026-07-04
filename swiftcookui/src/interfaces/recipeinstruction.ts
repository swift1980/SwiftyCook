export interface RecipeInstructionDto {
  position: number
  step: string
}

export interface RecipeInstructionCreateDto {
  step: string
  position: number
}