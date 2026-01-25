import { z } from 'zod'

export const checkValidGitRepoCommandSchema = z.object({
  repository: z.string(),
  branch: z.string(),
  path: z.string(),
})