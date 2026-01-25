import client from '@kubb/plugin-client/clients/axios'
import type { DeleteStackMutationResponse, DeleteStackPathParams } from '../types/DeleteStack'
import type { RequestConfig, ResponseErrorConfig } from '@kubb/plugin-client/clients/axios'

export function getDeleteStackUrl(id: DeleteStackPathParams['id']) {
  return `/api/Stacks/${id}` as const
}

/**
 * {@link /api/Stacks/:id}
 */
export async function deleteStack(id: DeleteStackPathParams['id'], config: Partial<RequestConfig> & { client?: typeof client } = {}) {
  const { client: request = client, ...requestConfig } = config

  const res = await request<DeleteStackMutationResponse, ResponseErrorConfig<Error>, unknown>({
    method: 'DELETE',
    url: getDeleteStackUrl(id).toString(),
    ...requestConfig,
  })
  return res.data
}