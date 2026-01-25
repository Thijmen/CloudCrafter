import { createStackBuildOptionSchema } from './createStackBuildOptionSchema'
import { z } from 'zod'

export const createStackCommandSchema = z.object({
  name: z.string().min(3),
  gitRepository: z.string().min(1),
  gitBranch: z.string().min(1),
  pathInGitRepository: z.string(),
  buildOption: z.lazy(() => createStackBuildOptionSchema),
  environmentId: z.string().uuid(),
  serverId: z.string().uuid(),
})