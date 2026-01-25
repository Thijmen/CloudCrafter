export type DeleteStackPathParams = {
  /**
   * @type string, uuid
   */
  id: string
}

/**
 * @description OK
 */
export type DeleteStack200 = any

export type DeleteStackMutationResponse = DeleteStack200

export type DeleteStackMutation = {
  Response: DeleteStack200
  PathParams: DeleteStackPathParams
  Errors: any
}