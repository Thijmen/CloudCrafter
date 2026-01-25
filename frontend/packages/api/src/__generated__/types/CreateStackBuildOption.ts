export const createStackBuildOptionEnum = {
  Nixpacks: 'Nixpacks',
  DockerCompose: 'DockerCompose',
} as const

export type CreateStackBuildOptionEnum = (typeof createStackBuildOptionEnum)[keyof typeof createStackBuildOptionEnum]

export type CreateStackBuildOption = CreateStackBuildOptionEnum