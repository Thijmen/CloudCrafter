import client from '@cloudcrafter/api/client'
import type { DeleteStackMutationResponse, DeleteStackPathParams } from '../types/DeleteStack'
import type { RequestConfig, ResponseErrorConfig } from '@cloudcrafter/api/client'
import type { UseMutationOptions } from '@tanstack/react-query'
import { deleteStack } from '../axios-backend/deleteStack'
import { useMutation } from '@tanstack/react-query'

export const deleteStackMutationKey = () => [{ url: '/api/Stacks/{id}' }] as const

export type DeleteStackMutationKey = ReturnType<typeof deleteStackMutationKey>

/**
 * {@link /api/Stacks/:id}
 */
export function useDeleteStackHook<TContext>(
  options: {
    mutation?: UseMutationOptions<DeleteStackMutationResponse, ResponseErrorConfig<Error>, { id: DeleteStackPathParams['id'] }, TContext>
    client?: Partial<RequestConfig> & { client?: typeof client }
  } = {},
) {
  const { mutation: mutationOptions, client: config = {} } = options ?? {}
  const mutationKey = mutationOptions?.mutationKey ?? deleteStackMutationKey()

  return useMutation<DeleteStackMutationResponse, ResponseErrorConfig<Error>, { id: DeleteStackPathParams['id'] }, TContext>({
    mutationFn: async ({ id }) => {
      return deleteStack(id, config)
    },
    mutationKey,
    ...mutationOptions,
  })
}