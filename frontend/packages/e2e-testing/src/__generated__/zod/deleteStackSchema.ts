import { z } from 'zod'

export const deleteStackPathParamsSchema = z.object({
  id: z.string().uuid(),
})

/**
 * @description OK
 */
export const deleteStack200Schema = z.any()

export const deleteStackMutationResponseSchema = z.lazy(() => deleteStack200Schema)