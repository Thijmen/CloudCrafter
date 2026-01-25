import { z } from 'zod'

export const createStackBuildOptionSchema = z.enum(['Nixpacks', 'DockerCompose'])