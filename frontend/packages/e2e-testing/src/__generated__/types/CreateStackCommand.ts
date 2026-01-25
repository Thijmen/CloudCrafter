import type { CreateStackBuildOption } from './CreateStackBuildOption'

export type CreateStackCommand = {
  /**
   * @minLength 3
   * @type string
   */
  name: string
  /**
   * @minLength 1
   * @type string
   */
  gitRepository: string
  /**
   * @minLength 1
   * @type string
   */
  gitBranch: string
  /**
   * @type string
   */
  pathInGitRepository: string
  buildOption: CreateStackBuildOption
  /**
   * @type string, uuid
   */
  environmentId: string
  /**
   * @type string, uuid
   */
  serverId: string
}